using Convex.Client;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Connection;
using Convex.Client.Authentication.Contracts;
using Convex.Client.FileStorage.Contracts;
using Convex.Client.VectorSearch.Contracts;
using Convex.Client.HttpActions.Contracts;
using Convex.Client.Scheduling.Contracts;
using Convex.Client.Infrastructure.ConsistentQueries;
using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Results;
using Convex.Client.Features.RealTime.Pagination;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Convex.Client.Extensions.Testing;

/// <summary>
/// Mock implementation of IConvexClient for testing purposes.
/// Allows setting up predefined responses for queries, mutations, and actions.
/// Note: This is a simplified mock for basic testing. Advanced features like
/// file storage, vector search, etc. return null/default implementations.
/// </summary>
public class MockConvexClient : IConvexClient
{
    private readonly Dictionary<string, object?> _queryResponses = new();
    private readonly Dictionary<string, object?> _mutationResponses = new();
    private readonly Dictionary<string, object?> _actionResponses = new();
    private readonly Dictionary<string, Exception> _queryExceptions = new();
    private readonly Dictionary<string, Exception> _mutationExceptions = new();
    private readonly Dictionary<string, Exception> _actionExceptions = new();
    private readonly List<CallRecord> _callHistory = new();

    private string? _authToken;
    private string _deploymentUrl = "https://mock.convex.cloud";

    /// <summary>
    /// Gets the call history for all operations.
    /// </summary>
    public IReadOnlyList<CallRecord> CallHistory => _callHistory;

    // IConvexClient implementation

    public string DeploymentUrl => _deploymentUrl;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public AuthenticationState AuthenticationState => string.IsNullOrEmpty(_authToken)
        ? AuthenticationState.Unauthenticated
        : AuthenticationState.Authenticated;

    public string? CurrentAuthToken => _authToken;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    public TimestampManager TimestampManager { get; } = null!; // Mock doesn't implement this

    public IConvexFileStorage FileStorage { get; } = null!; // Mock doesn't implement this

    public IConvexVectorSearch VectorSearch { get; } = null!; // Mock doesn't implement this

    public IConvexHttpActions HttpActions { get; } = null!; // Mock doesn't implement this

    public IConvexScheduler Scheduler { get; } = null!; // Mock doesn't implement this

    public ConnectionState ConnectionState => ConnectionState.Disconnected;

    public IObservable<ConnectionState> ConnectionStateChanges => Observable.Empty<ConnectionState>();

    public IObservable<Convex.Client.Infrastructure.Quality.ConnectionQuality> ConnectionQualityChanges => Observable.Empty<Convex.Client.Infrastructure.Quality.ConnectionQuality>();

    private readonly Dictionary<string, object?> _cachedValues = new();

    /// <summary>
    /// Sets up a query to return a specific response.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupQuery<TResult>(string functionName, TResult response)
    {
        _queryResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up a query to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupQueryException(string functionName, Exception exception)
    {
        _queryExceptions[functionName] = exception;
        return this;
    }

    /// <summary>
    /// Sets up a mutation to return a specific response.
    /// </summary>
    /// <typeparam name="TResult">The type of the mutation result.</typeparam>
    /// <param name="functionName">The name of the mutation function.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupMutation<TResult>(string functionName, TResult response)
    {
        _mutationResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up a mutation to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the mutation function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupMutationException(string functionName, Exception exception)
    {
        _mutationExceptions[functionName] = exception;
        return this;
    }

    /// <summary>
    /// Sets up an action to return a specific response.
    /// </summary>
    /// <typeparam name="TResult">The type of the action result.</typeparam>
    /// <param name="functionName">The name of the action function.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupAction<TResult>(string functionName, TResult response)
    {
        _actionResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up an action to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the action function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This mock instance for method chaining.</returns>
    public MockConvexClient SetupActionException(string functionName, Exception exception)
    {
        _actionExceptions[functionName] = exception;
        return this;
    }

    /// <summary>
    /// Sets the mock deployment URL.
    /// </summary>
    public MockConvexClient SetDeploymentUrl(string url)
    {
        _deploymentUrl = url;
        return this;
    }

    /// <summary>
    /// Clears all setups and call history.
    /// </summary>
    public void Reset()
    {
        _queryResponses.Clear();
        _mutationResponses.Clear();
        _actionResponses.Clear();
        _queryExceptions.Clear();
        _mutationExceptions.Clear();
        _actionExceptions.Clear();
        _callHistory.Clear();
        _authToken = null;
    }

    /// <summary>
    /// Verifies that a query was called with specific arguments.
    /// </summary>
    /// <param name="functionName">The query function name.</param>
    /// <param name="times">Expected number of calls (null = at least once).</param>
    /// <returns>True if verification passes.</returns>
    public bool VerifyQueryCalled(string functionName, int? times = null)
    {
        var calls = _callHistory.Count(c => c.Type == OperationType.Query && c.FunctionName == functionName);
        return times.HasValue ? calls == times.Value : calls > 0;
    }

    /// <summary>
    /// Verifies that a mutation was called.
    /// </summary>
    /// <param name="functionName">The mutation function name.</param>
    /// <param name="times">Expected number of calls (null = at least once).</param>
    /// <returns>True if verification passes.</returns>
    public bool VerifyMutationCalled(string functionName, int? times = null)
    {
        var calls = _callHistory.Count(c => c.Type == OperationType.Mutation && c.FunctionName == functionName);
        return times.HasValue ? calls == times.Value : calls > 0;
    }

    /// <summary>
    /// Verifies that an action was called.
    /// </summary>
    /// <param name="functionName">The action function name.</param>
    /// <param name="times">Expected number of calls (null = at least once).</param>
    /// <returns>True if verification passes.</returns>
    public bool VerifyActionCalled(string functionName, int? times = null)
    {
        var calls = _callHistory.Count(c => c.Type == OperationType.Action && c.FunctionName == functionName);
        return times.HasValue ? calls == times.Value : calls > 0;
    }

    // Fluent API implementation
    public IQueryBuilder<TResult> Query<TResult>(string functionName)
    {
        return new MockQueryBuilder<TResult>(this, functionName);
    }

    public IBatchQueryBuilder Batch()
    {
        throw new NotImplementedException("Mock client does not support batch queries.");
    }

    public IMutationBuilder<TResult> Mutate<TResult>(string functionName)
    {
        return new MockMutationBuilder<TResult>(this, functionName);
    }

    public IActionBuilder<TResult> Action<TResult>(string functionName)
    {
        return new MockActionBuilder<TResult>(this, functionName);
    }

    public IObservable<T> Observe<T>(string functionName)
    {
        // Return a simple observable that emits the cached value if available
        if (_cachedValues.TryGetValue(functionName, out var cached) && cached is T typedValue)
        {
            return Observable.Return(typedValue);
        }
        return Observable.Empty<T>();
    }

    public IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        // Return a simple observable that emits the cached value if available
        var cacheKey = $"{functionName}:{args}";
        if (_cachedValues.TryGetValue(cacheKey, out var cached) && cached is T typedValue)
        {
            return Observable.Return(typedValue);
        }
        return Observable.Empty<T>();
    }

    public T? GetCachedValue<T>(string functionName)
    {
        return _cachedValues.TryGetValue(functionName, out var cached) && cached is T typedValue
            ? typedValue
            : default;
    }

    public bool TryGetCachedValue<T>(string functionName, out T? value)
    {
        if (_cachedValues.TryGetValue(functionName, out var cached) && cached is T typedValue)
        {
            value = typedValue;
            return true;
        }
        value = default;
        return false;
    }

    public void DefineQueryDependency(string mutationName, params string[] invalidates)
    {
        // Mock implementation - no-op
    }

    public Task InvalidateQueryAsync(string queryName)
    {
        _cachedValues.Remove(queryName);
        return Task.CompletedTask;
    }

    public Task InvalidateQueriesAsync(string pattern)
    {
        // Simple pattern matching - supports * wildcard
        var keysToRemove = _cachedValues.Keys
            .Where(key => MatchesPattern(key, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cachedValues.Remove(key);
        }

        return Task.CompletedTask;
    }

    public IConvexPagination PaginationSlice => throw new NotImplementedException("Pagination is not implemented in MockConvexClient");

    private static bool MatchesPattern(string key, string pattern)
    {
        if (!pattern.Contains('*'))
        {
            return key == pattern;
        }

        var parts = pattern.Split('*');
        if (parts.Length == 0)
        {
            return true;
        }

        if (!key.StartsWith(parts[0]))
        {
            return false;
        }

        var remaining = key.Substring(parts[0].Length);
        for (int i = 1; i < parts.Length; i++)
        {
            var index = remaining.IndexOf(parts[i]);
            if (index == -1)
            {
                return false;
            }
            remaining = remaining.Substring(index + parts[i].Length);
        }

        return true;
    }

    // Auth methods
    public void SetAuthToken(string token)
    {
        _authToken = token;
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(AuthenticationState));
    }

    public void SetAdminAuth(string adminKey)
    {
        _authToken = adminKey;
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(AuthenticationState));
    }

    public void SetAuthTokenProvider(IAuthTokenProvider provider)
    {
        // Mock implementation - just store it but don't use
    }

    public void ClearAuth()
    {
        _authToken = null;
        AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(AuthenticationState));
    }

    public void Dispose()
    {
        // Mock implementation - nothing to dispose
        GC.SuppressFinalize(this);
    }

    // Internal helper methods for mock builders
    internal Task<TResult> ExecuteQuery<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        _callHistory.Add(new CallRecord(OperationType.Query, functionName, args));

        if (_queryExceptions.TryGetValue(functionName, out var exception))
        {
            throw exception;
        }

        if (_queryResponses.TryGetValue(functionName, out var response))
        {
            if (response is TResult typedResult)
            {
                return Task.FromResult(typedResult);
            }

            throw new InvalidOperationException(
                $"Mock response for query '{functionName}' is of type {response?.GetType().Name ?? "null"} " +
                $"but expected type {typeof(TResult).Name}. " +
                $"Ensure the mock is configured with the correct return type.");
        }

        throw new ConvexException($"No mock setup found for query: {functionName}");
    }

    internal Task<TResult> ExecuteMutation<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        _callHistory.Add(new CallRecord(OperationType.Mutation, functionName, args));

        if (_mutationExceptions.TryGetValue(functionName, out var exception))
        {
            throw exception;
        }

        if (_mutationResponses.TryGetValue(functionName, out var response))
        {
            if (response is TResult typedResult)
            {
                return Task.FromResult(typedResult);
            }

            throw new InvalidOperationException(
                $"Mock response for mutation '{functionName}' is of type {response?.GetType().Name ?? "null"} " +
                $"but expected type {typeof(TResult).Name}. " +
                $"Ensure the mock is configured with the correct return type.");
        }

        throw new ConvexException($"No mock setup found for mutation: {functionName}");
    }

    internal Task<TResult> ExecuteAction<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        _callHistory.Add(new CallRecord(OperationType.Action, functionName, args));

        if (_actionExceptions.TryGetValue(functionName, out var exception))
        {
            throw exception;
        }

        if (_actionResponses.TryGetValue(functionName, out var response))
        {
            if (response is TResult typedResult)
            {
                return Task.FromResult(typedResult);
            }

            throw new InvalidOperationException(
                $"Mock response for action '{functionName}' is of type {response?.GetType().Name ?? "null"} " +
                $"but expected type {typeof(TResult).Name}. " +
                $"Ensure the mock is configured with the correct return type.");
        }

        throw new ConvexException($"No mock setup found for action: {functionName}");
    }
}

/// <summary>
/// Represents a recorded call to the mock client.
/// </summary>
public class CallRecord
{
    public OperationType Type { get; }
    public string FunctionName { get; }
    public object? Args { get; }
    public DateTime Timestamp { get; }

    public CallRecord(OperationType type, string functionName, object? args)
    {
        Type = type;
        FunctionName = functionName;
        Args = args;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Type of Convex operation.
/// </summary>
public enum OperationType
{
    Query,
    Mutation,
    Action
}

/// <summary>
/// Mock implementation of IQueryBuilder for testing.
/// </summary>
internal class MockQueryBuilder<TResult> : IQueryBuilder<TResult>
{
    private readonly MockConvexClient _client;
    private readonly string _functionName;
    private object? _args;

    public MockQueryBuilder(MockConvexClient client, string functionName)
    {
        _client = client;
        _functionName = functionName;
    }

    public IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IQueryBuilder<TResult> WithTimeout(TimeSpan timeout) => this;
    public IQueryBuilder<TResult> IncludeMetadata() => this;
    public IQueryBuilder<TResult> UseConsistency(long timestamp) => this;
    public IQueryBuilder<TResult> Cached(TimeSpan cacheDuration) => this;
    public IQueryBuilder<TResult> OnError(Action<Exception> onError) => this;
    public IQueryBuilder<TResult> WithRetry(Action<Convex.Client.Infrastructure.Resilience.RetryPolicyBuilder> configure) => this;
    public IQueryBuilder<TResult> WithRetry(Convex.Client.Infrastructure.Resilience.RetryPolicy policy) => this;

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _client.ExecuteQuery<TResult>(_functionName, _args, cancellationToken);
    }

    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteAsync(cancellationToken);
            return ConvexResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return ConvexResult<TResult>.Failure(ex);
        }
    }
}

/// <summary>
/// Mock implementation of IMutationBuilder for testing.
/// </summary>
internal class MockMutationBuilder<TResult> : IMutationBuilder<TResult>
{
    private readonly MockConvexClient _client;
    private readonly string _functionName;
    private object? _args;

    public MockMutationBuilder(MockConvexClient client, string functionName)
    {
        _client = client;
        _functionName = functionName;
    }

    public IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IMutationBuilder<TResult> WithTimeout(TimeSpan timeout) => this;
    public IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate) => this;
    public IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply) => this;
    public IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(Func<TState> getter, Action<TState> setter, Func<TState, TState> update) => this;
    public IMutationBuilder<TResult> WithRollback(Action rollback) => this;
    public IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess) => this;
    public IMutationBuilder<TResult> OnError(Action<Exception> onError) => this;
    public IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception => this;
    public IMutationBuilder<TResult> SkipQueue() => this;
    public IMutationBuilder<TResult> WithRetry(Action<Convex.Client.Infrastructure.Resilience.RetryPolicyBuilder> configure) => this;
    public IMutationBuilder<TResult> WithRetry(Convex.Client.Infrastructure.Resilience.RetryPolicy policy) => this;
    public IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<Convex.Client.Infrastructure.OptimisticUpdates.IOptimisticLocalStore, TArgs> updateFn) where TArgs : notnull => this;
    public IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn) => this;
    public IMutationBuilder<TResult> TrackPending(ISet<string> tracker, string key) => this;
    public IMutationBuilder<TResult> WithCleanup(Action cleanup) => this;

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _client.ExecuteMutation<TResult>(_functionName, _args, cancellationToken);
    }

    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteAsync(cancellationToken);
            return ConvexResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return ConvexResult<TResult>.Failure(ex);
        }
    }
}

/// <summary>
/// Mock implementation of IActionBuilder for testing.
/// </summary>
internal class MockActionBuilder<TResult> : IActionBuilder<TResult>
{
    private readonly MockConvexClient _client;
    private readonly string _functionName;
    private object? _args;

    public MockActionBuilder(MockConvexClient client, string functionName)
    {
        _client = client;
        _functionName = functionName;
    }

    public IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IActionBuilder<TResult> WithTimeout(TimeSpan timeout) => this;
    public IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess) => this;
    public IActionBuilder<TResult> OnError(Action<Exception> onError) => this;
    public IActionBuilder<TResult> WithRetry(Action<Convex.Client.Infrastructure.Resilience.RetryPolicyBuilder> configure) => this;
    public IActionBuilder<TResult> WithRetry(Convex.Client.Infrastructure.Resilience.RetryPolicy policy) => this;

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _client.ExecuteAction<TResult>(_functionName, _args, cancellationToken);
    }

    public async Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteAsync(cancellationToken);
            return ConvexResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return ConvexResult<TResult>.Failure(ex);
        }
    }
}
