using Convex.Client.Shared.Builders;
using Convex.Client.Shared.Caching;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.OptimisticUpdates;
using Convex.Client.Shared.Resilience;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Internal.Threading;

namespace Convex.Client.Slices.Mutations;

/// <summary>
/// Fluent builder for creating and executing Convex mutations with optimistic updates.
/// This implementation uses Shared infrastructure instead of CoreOperations.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
internal sealed class MutationBuilder<TResult>(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    string functionName,
    IConvexCache? queryCache = null,
    Func<string, Task>? invalidateDependencies = null,
    Func<string, string, object?, TimeSpan?, CancellationToken, Task<TResult>>? middlewareExecutor = null,
    SyncContextCapture? syncContext = null,
    Func<Func<CancellationToken, Task<TResult>>, CancellationToken, Task<TResult>>? enqueueMutation = null) : IMutationBuilder<TResult>
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly string _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    private readonly IConvexCache? _queryCache = queryCache;
    private readonly Func<string, Task>? _invalidateDependencies = invalidateDependencies;
    private readonly Func<string, string, object?, TimeSpan?, CancellationToken, Task<TResult>>? _middlewareExecutor = middlewareExecutor;
    private readonly SyncContextCapture? _syncContext = syncContext;

    private object? _args;
    private TimeSpan? _timeout;
    private Action<TResult>? _optimisticUpdate;
    private TResult? _optimisticValue;
    private Action<TResult>? _applyOptimistic;
    private Action? _rollbackOptimistic;
    private Action<TResult>? _onSuccess;
    private Action<Exception>? _onError;
    private Type? _rollbackExceptionType;
    private RetryPolicy? _retryPolicy;
    private readonly List<CacheUpdate> _cacheUpdates = [];
    private bool _skipQueue = false;
    private readonly Func<Func<CancellationToken, Task<TResult>>, CancellationToken, Task<TResult>>? _enqueueMutation = enqueueMutation;

    // Auto-rollback optimistic update support
    private object? _autoRollbackState;
    private bool _hasAutoRollbackState;

    // Query-focused optimistic update support
    private Delegate? _queryFocusedOptimisticUpdate;
    private Dictionary<string, object?>? _querySnapshots;

    // Cleanup and tracking support
    private ISet<string>? _pendingTracker;
    private string? _pendingKey;
    private Action? _cleanupAction;

    /// <summary>
    /// Bypasses the mutation queue, executing this mutation immediately.
    /// By default, mutations are queued and executed sequentially to ensure ordering guarantees.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public IMutationBuilder<TResult> SkipQueue()
    {
        _skipQueue = true;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate)
    {
        _optimisticUpdate = optimisticUpdate;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply)
    {
        _optimisticValue = optimisticValue;
        _applyOptimistic = apply;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(
        Func<TState> getter,
        Action<TState> setter,
        Func<TState, TState> update)
    {
        if (getter == null)
        {
            throw new ArgumentNullException(nameof(getter));
        }

        if (setter == null)
        {
            throw new ArgumentNullException(nameof(setter));
        }

        if (update == null)
        {
            throw new ArgumentNullException(nameof(update));
        }

        // Wrap the getter/setter/update functions to automatically handle rollback
        _applyOptimistic = _ =>
        {
            // Capture the current state before applying the update
            _autoRollbackState = getter();
            _hasAutoRollbackState = true;

            // Apply the optimistic update, marshalling to UI thread if needed
            var currentState = getter();
            var updatedState = update(currentState);

            // Marshal setter to UI thread if sync context is available
            ThreadMarshallingHelper.InvokeCallback(setter, updatedState, _syncContext);
        };

        // Set up automatic rollback using the captured state
        _rollbackOptimistic = () =>
        {
            if (_hasAutoRollbackState && _autoRollbackState is TState capturedState)
            {
                // Marshal rollback to UI thread if sync context is available
                ThreadMarshallingHelper.InvokeCallback(setter, capturedState, _syncContext);
                _hasAutoRollbackState = false;
                _autoRollbackState = null;
            }
        };

        // Use a dummy optimistic value since the apply function handles everything
        _optimisticValue = default;

        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithRollback(Action rollback)
    {
        _rollbackOptimistic = rollback;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess)
    {
        _onSuccess = onSuccess;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> OnError(Action<Exception> onError)
    {
        _onError = onError;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception
    {
        _rollbackExceptionType = typeof(TException);
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new RetryPolicyBuilder();
        configure(builder);
        _retryPolicy = builder.Build();
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithRetry(RetryPolicy policy)
    {
        _retryPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<IOptimisticLocalStore, TArgs> updateFn) where TArgs : notnull
    {
        if (updateFn == null) throw new ArgumentNullException(nameof(updateFn));

        // Store the update function as a delegate
        _queryFocusedOptimisticUpdate = updateFn;
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        if (updateFn == null)
        {
            throw new ArgumentNullException(nameof(updateFn));
        }

        _cacheUpdates.Add(new CacheUpdate(queryName, typeof(TCache), value => updateFn((TCache)value!)));
        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> TrackPending(ISet<string> tracker, string key)
    {
        if (tracker == null) throw new ArgumentNullException(nameof(tracker));
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        _pendingTracker = tracker;
        _pendingKey = key;

        // Add to tracker immediately
        _ = tracker.Add(key);

        return this;
    }

    /// <inheritdoc/>
    public IMutationBuilder<TResult> WithCleanup(Action cleanup)
    {
        if (cleanup == null) throw new ArgumentNullException(nameof(cleanup));

        // Chain cleanup actions if multiple are registered
        if (_cleanupAction != null)
        {
            var previousCleanup = _cleanupAction;
            _cleanupAction = () =>
            {
                previousCleanup();
                cleanup();
            };
        }
        else
        {
            _cleanupAction = cleanup;
        }

        return this;
    }

    /// <inheritdoc/>
    public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var appliedOptimistic = false;
        var appliedCacheUpdates = new List<CacheSnapshot>();
        OptimisticLocalStore? optimisticStore = null;

        try
        {
            // Apply query-focused optimistic update if configured and cache is available
            if (_queryCache != null && _queryFocusedOptimisticUpdate != null && _args != null)
            {
                optimisticStore = new OptimisticLocalStore(_queryCache, _serializer);

                // Invoke the optimistic update function
                // We need to dynamically invoke the delegate with the correct type
                var updateMethod = _queryFocusedOptimisticUpdate.GetType().GetMethod("Invoke")!;

                // The delegate signature is Action<IOptimisticLocalStore, TArgs>
                // We need to call it with optimisticStore and _args
                _ = updateMethod.Invoke(_queryFocusedOptimisticUpdate, [optimisticStore, _args]);

                // Capture snapshots of modified queries for rollback
                // OptimisticLocalStore already captured original values in SetQuery
                _querySnapshots = new Dictionary<string, object?>(optimisticStore.OriginalValues);
            }

            // Apply cache updates optimistically if configured and cache is available
            if (_queryCache != null && _cacheUpdates.Count > 0)
            {
                foreach (var update in _cacheUpdates)
                {
                    // Save current value for rollback
                    var snapshot = new CacheSnapshot(update.QueryName, update.ValueType);
                    if (_queryCache.TryGet<object>(update.QueryName, out var currentValue))
                    {
                        snapshot.OriginalValue = currentValue;
                        snapshot.WasCached = true;

                        // Apply optimistic update
                        var updatedValue = update.UpdateFunction(currentValue);
                        _queryCache.Set(update.QueryName, updatedValue);
                        appliedCacheUpdates.Add(snapshot);
                    }
                }
            }

            // Apply optimistic update immediately if configured
            // Note: This is done ONCE, NOT during retries
            // OptimisticWithAutoRollback sets _applyOptimistic but _optimisticValue may be null
            if (_applyOptimistic != null)
            {
                // For OptimisticWithAutoRollback, _optimisticValue is set to default/null
                // but _applyOptimistic handles everything internally
                if (_optimisticValue != null)
                {
                    _applyOptimistic(_optimisticValue);
                }
                else
                {
                    // For OptimisticWithAutoRollback, invoke with a dummy value
                    // The _applyOptimistic lambda ignores the parameter and uses getter/setter/update instead
                    _applyOptimistic(default!);
                }
                appliedOptimistic = true;
            }

            // Execute mutation (with queueing, retry if configured)
            async Task<TResult> ExecuteMutationAsync(CancellationToken ct)
            {
                if (_retryPolicy != null)
                {
                    // Execute with retry logic
                    return await RetryExecutor.ExecuteAsync(
                        () => ExecuteMutationOnceAsync(ct),
                        _retryPolicy,
                        ct);
                }
                else
                {
                    // Execute without retry
                    return await ExecuteMutationOnceAsync(ct);
                }
            }

            TResult result;
            // Queue mutation if queueing is enabled and not skipped
            if (!_skipQueue && _enqueueMutation != null)
            {
                result = await _enqueueMutation(ExecuteMutationAsync, cancellationToken);
            }
            else
            {
                // Execute immediately without queueing
                result = await ExecuteMutationAsync(cancellationToken);
            }

            // Apply optimistic update with actual result if configured
            if (!appliedOptimistic && _optimisticUpdate != null)
            {
                ThreadMarshallingHelper.InvokeCallback(_optimisticUpdate, result, _syncContext);
            }

            // Invalidate dependent queries based on defined dependencies
            if (_invalidateDependencies != null)
            {
                await _invalidateDependencies(_functionName);
            }

            // Invoke success callback
            if (_onSuccess != null)
            {
                ThreadMarshallingHelper.InvokeCallback(_onSuccess, result, _syncContext);
            }

            // Execute cleanup action (on success)
            _cleanupAction?.Invoke();

            // Remove from pending tracker if tracking
            if (_pendingTracker != null && _pendingKey != null)
            {
                _ = _pendingTracker.Remove(_pendingKey);
            }

            return result;
        }
        catch (Exception ex)
        {
            // Check if we should rollback optimistic update
            var shouldRollback = _rollbackExceptionType == null ||
                                ex.GetType() == _rollbackExceptionType ||
                                ex.GetType().IsSubclassOf(_rollbackExceptionType);

            if (shouldRollback)
            {
                // Rollback query-focused optimistic updates
                if (_queryCache != null && _querySnapshots != null)
                {
                    foreach (var (queryKey, originalValue) in _querySnapshots)
                    {
                        if (originalValue == null)
                        {
                            // Query was not cached before, remove it
                            _ = _queryCache.Remove(queryKey);
                        }
                        else
                        {
                            // Restore original value
                            _queryCache.Set(queryKey, originalValue);
                        }
                    }
                }

                // Rollback cache updates
                if (_queryCache != null && appliedCacheUpdates.Count > 0)
                {
                    foreach (var snapshot in appliedCacheUpdates)
                    {
                        if (snapshot.WasCached)
                        {
                            _queryCache.Set(snapshot.QueryName, snapshot.OriginalValue!);
                        }
                        else
                        {
                            _ = _queryCache.Remove(snapshot.QueryName);
                        }
                    }
                }

                // Rollback optimistic update
                if (appliedOptimistic)
                {
                    _rollbackOptimistic?.Invoke();
                }
            }

            // Invoke error callback
            if (_onError != null)
            {
                ThreadMarshallingHelper.InvokeCallback(_onError, ex, _syncContext);
            }

            // Execute cleanup action (on error)
            _cleanupAction?.Invoke();

            // Remove from pending tracker if tracking
            if (_pendingTracker != null && _pendingKey != null)
            {
                _ = _pendingTracker.Remove(_pendingKey);
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default) => await ExecutionExtensions.ExecuteWithResultAsync(() => ExecuteAsync(cancellationToken));

    /// <summary>
    /// Executes the mutation once (without retry logic or optimistic updates).
    /// </summary>
    private async Task<TResult> ExecuteMutationOnceAsync(CancellationToken cancellationToken)
    {
        using var timeoutWrapper = TimeoutHelper.CreateTimeoutToken(_timeout, cancellationToken);

        try
        {
            // Execute mutation through middleware if available, otherwise directly
            if (_middlewareExecutor != null)
            {
                return await _middlewareExecutor("mutation", _functionName, _args, _timeout, timeoutWrapper.Token);
            }

            // Execute mutation directly using Shared infrastructure
            return await ExecuteDirectAsync(timeoutWrapper.Token);
        }
        catch (OperationCanceledException) when (timeoutWrapper.WasTimeout)
        {
            // Timeout occurred - wrap in more specific exception
            throw new TimeoutException($"Mutation '{_functionName}' timed out after {_timeout}");
        }
    }

    /// <summary>
    /// Executes the mutation directly using the HTTP provider.
    /// </summary>
    private async Task<TResult> ExecuteDirectAsync(CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildMutationRequest(
            _httpProvider.DeploymentUrl,
            _functionName,
            _args,
            _serializer);

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            _functionName,
            "mutation",
            _serializer,
            cancellationToken);
    }

    /// <summary>
    /// Represents a cache update operation to apply optimistically.
    /// </summary>
    private sealed class CacheUpdate(string queryName, Type valueType, Func<object?, object?> updateFunction)
    {
        public string QueryName { get; } = queryName;
        public Type ValueType { get; } = valueType;
        public Func<object?, object?> UpdateFunction { get; } = updateFunction;
    }

    /// <summary>
    /// Snapshot of cache state for rollback purposes.
    /// </summary>
    private sealed class CacheSnapshot(string queryName, Type valueType)
    {
        public string QueryName { get; } = queryName;
        public Type ValueType { get; } = valueType;
        public object? OriginalValue { get; set; }
        public bool WasCached { get; set; }
    }
}
