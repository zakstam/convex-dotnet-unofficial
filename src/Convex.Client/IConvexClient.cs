using Convex.Client.Shared.Builders;
using Convex.Client.Shared.Connection;
using Convex.Client.Slices.Pagination;
using Convex.Client.Slices.Queries;

namespace Convex.Client;

/// <summary>
/// Unified Convex client interface providing both HTTP operations (queries/mutations)
/// and WebSocket operations (real-time subscriptions).
/// </summary>
public interface IConvexClient : IDisposable
{
    #region Connection Management

    /// <summary>
    /// Gets the Convex deployment URL.
    /// </summary>
    string DeploymentUrl { get; }

    /// <summary>
    /// Gets or sets the default timeout for HTTP operations.
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets the current WebSocket connection state.
    /// Connection happens automatically when subscriptions are created.
    /// </summary>
    ConnectionState ConnectionState { get; }

    /// <summary>
    /// Observable stream of connection state changes.
    /// Emits new values whenever the WebSocket connection state changes.
    /// Connection is fully automatic - connects on first subscription, reconnects with exponential backoff on failures.
    /// Use ObserveOn(SynchronizationContext.Current) to marshal updates to the UI thread.
    /// </summary>
    IObservable<ConnectionState> ConnectionStateChanges { get; }

    /// <summary>
    /// Observable stream of connection quality changes.
    /// Quality is assessed periodically based on latency, errors, reconnections, and stability.
    /// Provides insights into network performance for adaptive UI and diagnostics.
    /// </summary>
    IObservable<Convex.Client.Shared.Quality.ConnectionQuality> ConnectionQualityChanges { get; }

    #endregion

    #region Queries (HTTP)

    /// <summary>
    /// Creates a fluent query builder for advanced query configuration.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "todos:list").</param>
    /// <returns>A query builder for configuring and executing the query.</returns>
    IQueryBuilder<TResult> Query<TResult>(string functionName);

    /// <summary>
    /// Creates a batch query builder for executing multiple queries in a single request.
    /// </summary>
    /// <returns>A batch query builder.</returns>
    IBatchQueryBuilder Batch();

    #endregion

    #region Mutations (HTTP)

    /// <summary>
    /// Creates a fluent mutation builder for advanced mutation configuration.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "todos:create").</param>
    /// <returns>A mutation builder for configuring and executing the mutation.</returns>
    IMutationBuilder<TResult> Mutate<TResult>(string functionName);

    #endregion

    #region Actions (HTTP)

    /// <summary>
    /// Creates a fluent action builder for advanced action configuration.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="functionName">The name of the Convex action (e.g., "actions:myAction").</param>
    /// <returns>An action builder for configuring and executing the action.</returns>
    IActionBuilder<TResult> Action<TResult>(string functionName);

    #endregion

    #region Subscriptions (WebSocket)

    /// <summary>
    /// Creates a real-time observable stream of a Convex query.
    /// The subscription starts automatically when you call Subscribe() on the observable.
    /// Auto-connects to the WebSocket server if needed and emits new values as the data changes.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the subscription.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "todos:list").</param>
    /// <returns>An observable stream that emits values as they change. Call Subscribe() to start receiving updates.</returns>
    /// <example>
    /// <code>
    /// client.Observe&lt;Message[]&gt;("messages:list")
    ///     .Subscribe(messages => UpdateUI(messages));
    /// </code>
    /// </example>
    IObservable<T> Observe<T>(string functionName);

    /// <summary>
    /// Creates a real-time observable stream of a Convex query with arguments.
    /// The subscription starts automatically when you call Subscribe() on the observable.
    /// Auto-connects to the WebSocket server if needed and emits new values as the data changes.
    /// </summary>
    /// <typeparam name="T">The type of data returned by the subscription.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the function.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "messages:get").</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>An observable stream that emits values as they change. Call Subscribe() to start receiving updates.</returns>
    IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

    #endregion

    #region Cached Values

    /// <summary>
    /// Gets a cached value from an active subscription, if available.
    /// Returns default(T?) if no subscription exists for this function.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <returns>The cached value, or default(T?) if not found.</returns>
    T? GetCachedValue<T>(string functionName);

    /// <summary>
    /// Tries to get a cached value from an active subscription.
    /// </summary>
    /// <typeparam name="T">The type of data.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if a cached value was found, false otherwise.</returns>
    bool TryGetCachedValue<T>(string functionName, out T? value);

    #endregion

    #region Pagination

    /// <summary>
    /// Gets the pagination slice for cursor-based pagination of Convex queries.
    /// Provides cursor-based pagination for loading large datasets in manageable pages.
    /// </summary>
    IConvexPagination PaginationSlice { get; }

    #endregion

    #region Cache & Dependency Tracking

    /// <summary>
    /// Defines query dependencies for automatic cache invalidation.
    /// When the specified mutation executes, the listed queries will be invalidated from the cache.
    /// </summary>
    /// <param name="mutationName">The name of the mutation (e.g., "todos:create").</param>
    /// <param name="invalidates">The queries to invalidate (e.g., "todos:list", "todos:count").</param>
    /// <example>
    /// <code>
    /// client.DefineQueryDependency("todos:create", "todos:list", "todos:count", "projects:getStats");
    /// </code>
    /// </example>
    void DefineQueryDependency(string mutationName, params string[] invalidates);

    /// <summary>
    /// Manually invalidates a specific query from the cache.
    /// </summary>
    /// <param name="queryName">The name of the query to invalidate.</param>
    /// <returns>A task that completes when invalidation is done.</returns>
    /// <exception cref="ArgumentNullException">Thrown when queryName is null or whitespace.</exception>
    Task InvalidateQueryAsync(string queryName);

    /// <summary>
    /// Manually invalidates all queries matching a pattern from the cache.
    /// Supports wildcards: "todos:*" matches "todos:list", "todos:count", etc.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports * and ? wildcards).</param>
    /// <returns>A task that completes when invalidation is done.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null or whitespace.</exception>
    Task InvalidateQueriesAsync(string pattern);

    #endregion
}
