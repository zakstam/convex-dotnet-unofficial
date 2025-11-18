using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Connection;
using Convex.Client.Shared.Builders;
using Convex.Client.Shared.Quality;
using Convex.Client.Slices.Queries;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for testing Convex client observables and creating test infrastructure.
/// </summary>
public static class ConvexTestingExtensions
{
    #region Mock Client Builder

    /// <summary>
    /// Creates a mock Convex client with controllable observables for testing.
    /// </summary>
    /// <param name="configure">Action to configure the mock client.</param>
    /// <returns>A configured mock Convex client.</returns>
    /// <example>
    /// <code>
    /// var mockClient = CreateMockClient(builder =>
    /// {
    ///     builder.SetupQuery&lt;User&gt;("users:current", testUser);
    ///     builder.SetupConnectionState(ConnectionState.Connected);
    ///     builder.SetupConnectionQuality(ConnectionQuality.Good);
    /// });
    ///
    /// var user = await mockClient.QueryAsync&lt;User&gt;("users:current");
    /// </code>
    /// </example>
    public static IConvexClient CreateMockClient(Action<MockConvexClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MockConvexClientBuilder();
        configure(builder);
        return builder.Build();
    }

    #endregion

    #region Observable Recording

    /// <summary>
    /// Records all emissions from an observable for later assertions in tests.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable to record.</param>
    /// <returns>A RecordedObservable that captures all emissions.</returns>
    /// <example>
    /// <code>
    /// var recorded = client.Observe&lt;Message[]&gt;("messages:list").Record();
    ///
    /// // Trigger some changes...
    ///
    /// // Assert in tests
    /// Assert.Equal(2, recorded.Values.Count);
    /// Assert.Equal(expectedMessages, recorded.Values.Last());
    /// </code>
    /// </example>
    public static RecordedObservable<T> Record<T>(this IObservable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new RecordedObservable<T>(source);
    }

    #endregion

    #region Value Waiting

    /// <summary>
    /// Waits for an observable to emit a value that matches the specified predicate.
    /// Useful for testing asynchronous operations that emit values over time.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="predicate">Predicate that the value must match.</param>
    /// <param name="timeout">Maximum time to wait for the value (default: 5 seconds).</param>
    /// <returns>A task that completes with the matching value.</returns>
    /// <exception cref="TimeoutException">Thrown if no matching value is emitted within the timeout.</exception>
    /// <example>
    /// <code>
    /// var messages = await client.Observe&lt;Message[]&gt;("messages:list")
    ///     .WaitForValue(messages => messages.Length > 0, TimeSpan.FromSeconds(10));
    ///
    /// Assert.True(messages.Length > 0);
    /// </code>
    /// </example>
    public static async Task<T> WaitForValue<T>(
        this IObservable<T> source,
        Func<T, bool> predicate,
        TimeSpan timeout = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var actualTimeout = timeout == default ? TimeSpan.FromSeconds(5) : timeout;

        using var cts = new CancellationTokenSource(actualTimeout);
        var task = source.FirstAsync(predicate).ToTask(cts.Token);

        try
        {
            return await task;
        }
        catch (TaskCanceledException) when (cts.IsCancellationRequested)
        {
            throw new TimeoutException($"No value matching the predicate was emitted within {actualTimeout.TotalSeconds} seconds.");
        }
    }

    /// <summary>
    /// Waits for an observable to emit any value.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="timeout">Maximum time to wait for a value (default: 5 seconds).</param>
    /// <returns>A task that completes with the first emitted value.</returns>
    /// <exception cref="TimeoutException">Thrown if no value is emitted within the timeout.</exception>
    /// <example>
    /// <code>
    /// var firstMessage = await client.Observe&lt;Message&gt;("messages:stream")
    ///     .WaitForValue(TimeSpan.FromSeconds(10));
    /// </code>
    /// </example>
    public static async Task<T> WaitForValue<T>(
        this IObservable<T> source,
        TimeSpan timeout = default) => await source.WaitForValue(_ => true, timeout);

    #endregion

    #region Connection Simulation

    /// <summary>
    /// Simulates intermittent connection issues by randomly dropping values.
    /// Useful for testing resilience and error handling in observables.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="disconnectProbability">Probability of disconnecting (0.0 to 1.0, default: 0.1).</param>
    /// <param name="reconnectDelay">Delay before reconnecting after a disconnect (default: 1 second).</param>
    /// <returns>An observable that simulates connection issues.</returns>
    /// <example>
    /// <code>
    /// var unreliableStream = client.Observe&lt;Message&gt;("messages:stream")
    ///     .SimulateIntermittentConnection(disconnectProbability: 0.2);
    ///
    /// // Test how your app handles connection drops
    /// var messages = await unreliableStream.Take(10).ToList();
    /// </code>
    /// </example>
    public static IObservable<T> SimulateIntermittentConnection<T>(
        this IObservable<T> source,
        double disconnectProbability = 0.1,
        TimeSpan? reconnectDelay = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (disconnectProbability is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(disconnectProbability), "Must be between 0.0 and 1.0");
        }

        var delay = reconnectDelay ?? TimeSpan.FromSeconds(1);
        var random = new Random();

        return Observable.Create<T>(observer =>
        {
            var isConnected = true;
            var buffer = new Queue<T>();

            return source.Subscribe(
                onNext: value =>
                {
                    if (isConnected)
                    {
                        // Randomly disconnect
                        if (random.NextDouble() < disconnectProbability)
                        {
                            isConnected = false;
                            _ = Observable.Timer(delay).Subscribe(_ =>
                            {
                                isConnected = true;
                                // Replay buffered values
                                while (buffer.Count > 0 && isConnected)
                                {
                                    observer.OnNext(buffer.Dequeue());
                                }
                            });
                        }
                        else
                        {
                            observer.OnNext(value);
                        }
                    }
                    else
                    {
                        // Buffer values while disconnected
                        buffer.Enqueue(value);
                    }
                },
                onError: observer.OnError,
                onCompleted: observer.OnCompleted);
        });
    }

    #endregion
}

/// <summary>
/// Represents a recorded observable that captures all emissions for testing.
/// </summary>
/// <typeparam name="T">The type of values emitted.</typeparam>
public class RecordedObservable<T>
{
    private readonly List<T> _values = [];
    private readonly List<Exception> _errors = [];

    /// <summary>
    /// Gets all values that have been emitted.
    /// </summary>
    public IReadOnlyList<T> Values => _values;

    /// <summary>
    /// Gets all errors that have been emitted.
    /// </summary>
    public IReadOnlyList<Exception> Errors => _errors;

    /// <summary>
    /// Gets whether the observable has completed.
    /// </summary>
    public bool Completed { get; private set; }

    /// <summary>
    /// Gets the last emitted value, or default(T) if no values have been emitted.
    /// </summary>
    public T? LastValue => _values.LastOrDefault();

    internal RecordedObservable(IObservable<T> source)
    {
        _ = source.Subscribe(
            onNext: _values.Add,
            onError: _errors.Add,
            onCompleted: () => Completed = true);
    }
}

/// <summary>
/// Builder for creating mock Convex clients with controllable behavior.
/// </summary>
public class MockConvexClientBuilder
{
    private readonly Dictionary<string, object?> _queryResponses = [];
    private readonly Dictionary<string, object?> _mutationResponses = [];
    private readonly Dictionary<string, object?> _actionResponses = [];
    private readonly Dictionary<string, Exception> _queryExceptions = [];
    private readonly Dictionary<string, Exception> _mutationExceptions = [];
    private readonly Dictionary<string, Exception> _actionExceptions = [];

    private ConnectionState _connectionState = ConnectionState.Connected;
    private ConnectionQuality _connectionQuality = ConnectionQuality.Good;

    /// <summary>
    /// Sets up a query to return a specific response.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="response">The response to return.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupQuery<TResult>(string functionName, TResult response)
    {
        _queryResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up a query to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupQueryException(string functionName, Exception exception)
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
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupMutation<TResult>(string functionName, TResult response)
    {
        _mutationResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up a mutation to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the mutation function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupMutationException(string functionName, Exception exception)
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
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupAction<TResult>(string functionName, TResult response)
    {
        _actionResponses[functionName] = response;
        return this;
    }

    /// <summary>
    /// Sets up an action to throw a specific exception.
    /// </summary>
    /// <param name="functionName">The name of the action function.</param>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupActionException(string functionName, Exception exception)
    {
        _actionExceptions[functionName] = exception;
        return this;
    }

    /// <summary>
    /// Sets the mock connection state.
    /// </summary>
    /// <param name="state">The connection state to simulate.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupConnectionState(ConnectionState state)
    {
        _connectionState = state;
        return this;
    }

    /// <summary>
    /// Sets the mock connection quality.
    /// </summary>
    /// <param name="quality">The connection quality to simulate.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MockConvexClientBuilder SetupConnectionQuality(ConnectionQuality quality)
    {
        _connectionQuality = quality;
        return this;
    }


    /// <summary>
    /// Builds the mock Convex client with the configured behavior.
    /// </summary>
    /// <returns>A mock IConvexClient implementation.</returns>
    public IConvexClient Build()
    {
        return new MockConvexClient(
            _queryResponses,
            _mutationResponses,
            _actionResponses,
            _queryExceptions,
            _mutationExceptions,
            _actionExceptions,
            _connectionState,
            _connectionQuality);
    }
}

/// <summary>
/// Mock implementation of IConvexClient for testing.
/// </summary>
internal class MockConvexClient(
    Dictionary<string, object?> queryResponses,
    Dictionary<string, object?> mutationResponses,
    Dictionary<string, object?> actionResponses,
    Dictionary<string, Exception> queryExceptions,
    Dictionary<string, Exception> mutationExceptions,
    Dictionary<string, Exception> actionExceptions,
    ConnectionState connectionState,
    ConnectionQuality connectionQuality) : IConvexClient
{
    private readonly Dictionary<string, object?> _queryResponses = queryResponses;
    private readonly Dictionary<string, object?> _mutationResponses = mutationResponses;
    private readonly Dictionary<string, object?> _actionResponses = actionResponses;
    private readonly Dictionary<string, Exception> _queryExceptions = queryExceptions;
    private readonly Dictionary<string, Exception> _mutationExceptions = mutationExceptions;
    private readonly Dictionary<string, Exception> _actionExceptions = actionExceptions;
    private readonly ConnectionQuality _connectionQuality = connectionQuality;

    // IConvexClient implementation (simplified for testing)
    public string DeploymentUrl => "https://mock.convex.cloud";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public ConnectionState ConnectionState { get; } = connectionState;
    public IObservable<ConnectionState> ConnectionStateChanges => Observable.Return(ConnectionState);
    public IObservable<ConnectionQuality> ConnectionQualityChanges => Observable.Return(_connectionQuality);

    // Fluent API implementations for testing
    public IQueryBuilder<TResult> Query<TResult>(string functionName) => new MockQueryBuilder<TResult>(this, functionName, _queryResponses, _queryExceptions);

    public IBatchQueryBuilder Batch() => throw new NotImplementedException();

    public IMutationBuilder<TResult> Mutate<TResult>(string functionName) => new MockMutationBuilder<TResult>(this, functionName, _mutationResponses, _mutationExceptions);

    public IActionBuilder<TResult> Action<TResult>(string functionName) => new MockActionBuilder<TResult>(this, functionName, _actionResponses, _actionExceptions);
    public IObservable<T> Observe<T>(string functionName) => throw new NotImplementedException();
    public IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull => throw new NotImplementedException();
    public T? GetCachedValue<T>(string functionName) => default;
    public bool TryGetCachedValue<T>(string functionName, out T? value) { value = default; return false; }
    public void DefineQueryDependency(string mutationName, params string[] invalidates) { }
    public Task InvalidateQueryAsync(string queryName) => Task.CompletedTask;
    public Task InvalidateQueriesAsync(string pattern) => Task.CompletedTask;
    public Convex.Client.Slices.Pagination.IConvexPagination PaginationSlice => throw new NotImplementedException("Pagination is not implemented in MockConvexClient");
    public void Dispose() { }

    // Features not implemented in mock
    // TODO: Restore when vertical slice migration is complete
    // public Convex.Client.FileStorage.Contracts.IConvexFileStorage FileStorage => throw new NotImplementedException();
    // public Convex.Client.VectorSearch.Contracts.IConvexVectorSearch VectorSearch => throw new NotImplementedException();
    // public Convex.Client.HttpActions.Contracts.IConvexHttpActions HttpActions => throw new NotImplementedException();
    // public Convex.Client.Scheduling.Contracts.IConvexScheduler Scheduler => throw new NotImplementedException();
    // public Convex.Client.CoreOperations.ConsistentQueries.TimestampManager TimestampManager => throw new NotImplementedException();
}

/// <summary>
/// Mock query builder for testing.
/// </summary>
internal class MockQueryBuilder<TResult>(MockConvexClient client, string functionName, Dictionary<string, object?> responses, Dictionary<string, Exception> exceptions) : IQueryBuilder<TResult>
{
    private readonly MockConvexClient _client = client;
    private readonly string _functionName = functionName;
    private readonly Dictionary<string, object?> _responses = responses;
    private readonly Dictionary<string, Exception> _exceptions = exceptions;
    private object? _args;

    public IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configureArgs) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configureArgs(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IQueryBuilder<TResult> WithTimeout(TimeSpan timeout) => this;

    public IQueryBuilder<TResult> IncludeMetadata() => this;

    public IQueryBuilder<TResult> UseConsistency(long timestamp) => this;

    public IQueryBuilder<TResult> Cached(TimeSpan duration) => this;

    public IQueryBuilder<TResult> OnError(Action<Exception> errorHandler) => this;

    public IQueryBuilder<TResult> WithRetry(Action<Shared.Resilience.RetryPolicyBuilder> _) => this;

    public IQueryBuilder<TResult> WithRetry(Shared.Resilience.RetryPolicy _) => this;

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

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_exceptions.TryGetValue(_functionName, out var exception))
        {
            return Task.FromException<TResult>(exception);
        }
        if (_responses.TryGetValue(_functionName, out var response))
        {
            return Task.FromResult((TResult)response!);
        }
        return Task.FromException<TResult>(new ConvexException($"No mock setup for query: {_functionName}"));
    }
}

/// <summary>
/// Mock mutation builder for testing.
/// </summary>
internal class MockMutationBuilder<TResult>(MockConvexClient client, string functionName, Dictionary<string, object?> responses, Dictionary<string, Exception> exceptions) : IMutationBuilder<TResult>
{
    private readonly MockConvexClient _client = client;
    private readonly string _functionName = functionName;
    private readonly Dictionary<string, object?> _responses = responses;
    private readonly Dictionary<string, Exception> _exceptions = exceptions;
    private object? _args;

    public IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configureArgs) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configureArgs(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IMutationBuilder<TResult> WithTimeout(TimeSpan timeout) => this;
    public IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate) => this;
    public IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply) => this;
    public IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(Func<TState> getter, Action<TState> setter, Func<TState, TState> update) => this;
    public IMutationBuilder<TResult> WithRollback(Action rollback) => this;
    public IMutationBuilder<TResult> OnSuccess(Action<TResult> successHandler) => this;
    public IMutationBuilder<TResult> OnError(Action<Exception> errorHandler) => this;
    public IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception => this;
    public IMutationBuilder<TResult> SkipQueue() => this;
    public IMutationBuilder<TResult> WithRetry(Action<Shared.Resilience.RetryPolicyBuilder> _) => this;
    public IMutationBuilder<TResult> WithRetry(Shared.Resilience.RetryPolicy _) => this;
    public IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<Shared.OptimisticUpdates.IOptimisticLocalStore, TArgs> updateAction) where TArgs : notnull => this;
    public IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn) => this;
    public IMutationBuilder<TResult> TrackPending(ISet<string> pendingMutations, string mutationId) => this;
    public IMutationBuilder<TResult> WithCleanup(Action cleanupAction) => this;

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

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_exceptions.TryGetValue(_functionName, out var exception))
        {
            return Task.FromException<TResult>(exception);
        }
        if (_responses.TryGetValue(_functionName, out var response))
        {
            return Task.FromResult((TResult)response!);
        }
        return Task.FromException<TResult>(new ConvexException($"No mock setup for mutation: {_functionName}"));
    }
}

/// <summary>
/// Mock action builder for testing.
/// </summary>
internal class MockActionBuilder<TResult>(MockConvexClient client, string functionName, Dictionary<string, object?> responses, Dictionary<string, Exception> exceptions) : IActionBuilder<TResult>
{
    private readonly MockConvexClient _client = client;
    private readonly string _functionName = functionName;
    private readonly Dictionary<string, object?> _responses = responses;
    private readonly Dictionary<string, Exception> _exceptions = exceptions;
    private object? _args;

    public IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configureArgs) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configureArgs(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IActionBuilder<TResult> WithTimeout(TimeSpan timeout) => this;
    public IActionBuilder<TResult> OnSuccess(Action<TResult> successHandler) => this;
    public IActionBuilder<TResult> OnError(Action<Exception> errorHandler) => this;
    public IActionBuilder<TResult> WithRetry(Action<Shared.Resilience.RetryPolicyBuilder> _) => this;
    public IActionBuilder<TResult> WithRetry(Shared.Resilience.RetryPolicy _) => this;

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

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_exceptions.TryGetValue(_functionName, out var exception))
        {
            return Task.FromException<TResult>(exception);
        }
        if (_responses.TryGetValue(_functionName, out var response))
        {
            return Task.FromResult((TResult)response!);
        }
        return Task.FromException<TResult>(new ConvexException($"No mock setup for action: {_functionName}"));
    }
}
