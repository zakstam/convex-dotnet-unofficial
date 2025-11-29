using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.Connection;
using Convex.Client.Infrastructure.Quality;
using Convex.Client.Features.DataAccess.Queries;
using Convex.Client.Features.Observability.Diagnostics;
using Convex.Client.Features.Observability.Health;
using Convex.Client.Features.Observability.Resilience;
using Convex.Client.Features.Operational.HttpActions;
using Convex.Client.Features.Operational.Scheduling;
using Convex.Client.Features.RealTime.Pagination;
using Convex.Client.Features.Security.Authentication;
using Convex.Client.Features.Storage.Files;
using Convex.Client.Features.Storage.VectorSearch;

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
    /// <value>An observable that emits <see cref="ConnectionQuality"/> values whenever the connection quality changes.</value>
    /// <example>
    /// <code>
    /// // Subscribe to connection quality changes
    /// client.ConnectionQualityChanges
    ///     .Subscribe(quality => {
    ///         Console.WriteLine($"Connection quality: {quality}");
    ///         if (quality == ConnectionQuality.Poor || quality == ConnectionQuality.Terrible)
    ///         {
    ///             ShowConnectionWarning();
    ///         }
    ///     });
    /// </code>
    /// </example>
    /// <seealso cref="ConnectionStateChanges"/>
    IObservable<ConnectionQuality> ConnectionQualityChanges { get; }

    #endregion

    #region Queries (HTTP)

    /// <summary>
    /// Creates a fluent query builder for advanced query configuration.
    /// Queries are read-only operations that fetch data from your Convex backend.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query. This should match the return type of your Convex function.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "todos:list" or "functions/getTodos"). Function names match file paths: `convex/functions/getTodos.ts` becomes `"functions/getTodos"`.</param>
    /// <returns>A query builder for configuring and executing the query. Use fluent methods like <see cref="IQueryBuilder{TResult}.WithArgs{TArgs}(TArgs)"/> and <see cref="IQueryBuilder{TResult}.ExecuteAsync(CancellationToken)"/> to configure and execute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Simple query without arguments
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteAsync();
    ///
    /// // Query with arguments
    /// var user = await client.Query&lt;User&gt;("functions/getUser")
    ///     .WithArgs(new { userId = "user123" })
    ///     .ExecuteAsync();
    ///
    /// // Query with timeout and error handling
    /// var result = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
    ///     .WithArgs(new { query = "laptop" })
    ///     .WithTimeout(TimeSpan.FromSeconds(5))
    ///     .OnError(ex => Console.WriteLine($"Query failed: {ex.Message}"))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="IQueryBuilder{TResult}"/>
    /// <seealso cref="Observe{T}(string)"/>
    IQueryBuilder<TResult> Query<TResult>(string functionName);

    /// <summary>
    /// Creates a batch query builder for executing multiple queries in a single request.
    /// Batch queries are more efficient than executing queries individually as they reduce network round-trips.
    /// All queries in the batch execute concurrently and return results in the same order they were added.
    /// </summary>
    /// <returns>A batch query builder for adding queries and executing them together.</returns>
    /// <example>
    /// <code>
    /// // Execute multiple queries in a single batch
    /// var (todos, user, stats) = await client.Batch()
    ///     .Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Query&lt;User&gt;("functions/getUser", new { userId = "user123" })
    ///     .Query&lt;DashboardStats&gt;("functions/getStats")
    ///     .ExecuteAsync&lt;List&lt;Todo&gt;, User, DashboardStats&gt;();
    ///
    /// // Access results by position
    /// Console.WriteLine($"Found {todos.Count} todos");
    /// Console.WriteLine($"User: {user.Name}");
    /// </code>
    /// </example>
    /// <seealso cref="IBatchQueryBuilder"/>
    /// <seealso cref="Query{TResult}(string)"/>
    IBatchQueryBuilder Batch();

    #endregion

    #region Mutations (HTTP)

    /// <summary>
    /// Creates a fluent mutation builder for advanced mutation configuration.
    /// Mutations are write operations that modify data in your Convex backend.
    /// Mutations are queued and executed sequentially to ensure ordering guarantees.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation. This should match the return type of your Convex function.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "todos:create" or "functions/createTodo"). Function names match file paths: `convex/functions/createTodo.ts` becomes `"functions/createTodo"`.</param>
    /// <returns>A mutation builder for configuring and executing the mutation. Use fluent methods like <see cref="IMutationBuilder{TResult}.WithArgs{TArgs}(TArgs)"/>, <see cref="IMutationBuilder{TResult}.Optimistic(Action{TResult})"/>, and <see cref="IMutationBuilder{TResult}.ExecuteAsync(CancellationToken)"/> to configure and execute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Simple mutation
    /// var newTodo = await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///     .WithArgs(new { text = "Learn Convex .NET", completed = false })
    ///     .ExecuteAsync();
    ///
    /// // Mutation with optimistic update
    /// await client.Mutate&lt;Todo&gt;("functions/updateTodo")
    ///     .WithArgs(new { id = "todo123", completed = true })
    ///     .Optimistic(result => {
    ///         // Update UI immediately before server responds
    ///         _todos.First(t => t.Id == result.Id).IsCompleted = true;
    ///     })
    ///     .ExecuteAsync();
    ///
    /// // Mutation with error handling
    /// await client.Mutate&lt;Todo&gt;("functions/deleteTodo")
    ///     .WithArgs(new { id = "todo123" })
    ///     .OnSuccess(result => Console.WriteLine("Todo deleted"))
    ///     .OnError(ex => Console.WriteLine($"Failed: {ex.Message}"))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="IMutationBuilder{TResult}"/>
    /// <seealso cref="Query{TResult}(string)"/>
    IMutationBuilder<TResult> Mutate<TResult>(string functionName);

    #endregion

    #region Actions (HTTP)

    /// <summary>
    /// Creates a fluent action builder for advanced action configuration.
    /// Actions are server-side operations that can perform side effects like calling external APIs,
    /// sending emails, or other operations that aren't pure database operations.
    /// Unlike queries and mutations, actions can access external resources and have longer execution times.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action. This should match the return type of your Convex action function.</typeparam>
    /// <param name="functionName">The name of the Convex action (e.g., "actions:sendEmail" or "functions/sendEmail"). Function names match file paths: `convex/functions/sendEmail.ts` becomes `"functions/sendEmail"`.</param>
    /// <returns>An action builder for configuring and executing the action. Use fluent methods like <see cref="IActionBuilder{TResult}.WithArgs{TArgs}(TArgs)"/>, <see cref="IActionBuilder{TResult}.WithTimeout(TimeSpan)"/>, and <see cref="IActionBuilder{TResult}.ExecuteAsync(CancellationToken)"/> to configure and execute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Simple action call
    /// var result = await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello", body = "Welcome!" })
    ///     .ExecuteAsync();
    ///
    /// // Action with longer timeout for external API calls
    /// var apiResult = await client.Action&lt;ApiResponse&gt;("functions/callExternalApi")
    ///     .WithArgs(new { endpoint = "https://api.example.com/data" })
    ///     .WithTimeout(TimeSpan.FromSeconds(60))
    ///     .OnSuccess(result => Console.WriteLine($"API call succeeded: {result.Status}"))
    ///     .OnError(ex => Console.WriteLine($"API call failed: {ex.Message}"))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="IActionBuilder{TResult}"/>
    /// <seealso cref="Query{TResult}(string)"/>
    /// <seealso cref="Mutate{TResult}(string)"/>
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
    /// <typeparam name="T">The type of data returned by the subscription. This should match the return type of your Convex query function.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the function. Must be a non-nullable type (class, struct, or record).</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "messages:get" or "functions/getMessages"). Function names match file paths: `convex/functions/getMessages.ts` becomes `"functions/getMessages"`.</param>
    /// <param name="args">The arguments to pass to the function. These are serialized and sent to the Convex backend.</param>
    /// <returns>An observable stream that emits values as they change. Call Subscribe() to start receiving updates. The observable will automatically reconnect if the connection is lost.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty, or when <paramref name="args"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Subscribe to messages for a specific channel
    /// var subscription = client.Observe&lt;List&lt;Message&gt;, object&gt;("functions/getMessages", new { channelId = "channel123" })
    ///     .Subscribe(messages => {
    ///         Console.WriteLine($"Received {messages.Count} messages");
    ///         UpdateUI(messages);
    ///     });
    ///
    /// // Subscribe with error handling
    /// client.Observe&lt;User, object&gt;("functions/getUser", new { userId = "user123" })
    ///     .Subscribe(
    ///         user => Console.WriteLine($"User: {user.Name}"),
    ///         error => Console.WriteLine($"Subscription error: {error.Message}")
    ///     );
    ///
    /// // Remember to dispose subscriptions when done
    /// subscription.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="Observe{T}(string)"/>
    /// <seealso cref="GetCachedValue{T}(string)"/>
    IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

    #endregion

    #region Cached Values

    /// <summary>
    /// Gets a cached value from an active subscription, if available.
    /// Returns default(T?) if no subscription exists for this function or if the subscription hasn't received any data yet.
    /// Cached values are automatically updated when the subscription receives new data.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve from the cache.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "functions/listTodos"). Must match the function name used in <see cref="Observe{T}(string)"/>.</param>
    /// <returns>The cached value from the most recent subscription update, or default(T?) if no subscription exists or no data has been received yet.</returns>
    /// <remarks>
    /// This method is useful for getting the current value without subscribing to updates.
    /// For subscriptions with arguments, use the same function name format as used in Observe.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Subscribe to updates
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Subscribe(todos => Console.WriteLine($"Got {todos.Count} todos"));
    ///
    /// // Later, get the cached value without subscribing
    /// var cachedTodos = client.GetCachedValue&lt;List&lt;Todo&gt;&gt;("functions/listTodos");
    /// if (cachedTodos != null)
    /// {
    ///     Console.WriteLine($"Currently have {cachedTodos.Count} todos cached");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="TryGetCachedValue{T}"/>
    /// <seealso cref="Observe{T}(string)"/>
    T? GetCachedValue<T>(string functionName);

    /// <summary>
    /// Tries to get a cached value from an active subscription.
    /// This is the preferred method over <see cref="GetCachedValue{T}"/> as it avoids potential null reference issues.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve from the cache.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "functions/listTodos"). Must match the function name used in <see cref="Observe{T}(string)"/>.</param>
    /// <param name="value">When this method returns, contains the cached value if found, or default(T?) if not found.</param>
    /// <returns>True if a cached value was found and assigned to <paramref name="value"/>; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// // Subscribe to updates
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Subscribe(todos => UpdateUI(todos));
    ///
    /// // Later, safely get the cached value
    /// if (client.TryGetCachedValue&lt;List&lt;Todo&gt;&gt;("functions/listTodos", out var todos))
    /// {
    ///     Console.WriteLine($"Found {todos.Count} todos in cache");
    /// }
    /// else
    /// {
    ///     Console.WriteLine("No cached value available yet");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetCachedValue{T}(string)"/>
    /// <seealso cref="Observe{T}(string)"/>
    bool TryGetCachedValue<T>(string functionName, out T? value);

    #endregion

    #region Pagination

    /// <summary>
    /// Gets the pagination slice for cursor-based pagination of Convex queries.
    /// Provides cursor-based pagination for loading large datasets in manageable pages.
    /// Use this when you need to load data incrementally rather than all at once.
    /// </summary>
    /// <value>The pagination slice that provides methods for creating paginated queries and loading pages.</value>
    /// <example>
    /// <code>
    /// // Create a paginated query
    /// var paginator = client.PaginationSlice
    ///     .Query&lt;Todo&gt;("functions/listTodos")
    ///     .WithPageSize(20)
    ///     .Build();
    ///
    /// // Load first page
    /// var firstPage = await paginator.LoadNextAsync();
    /// Console.WriteLine($"Loaded {firstPage.Count} items");
    ///
    /// // Load more pages
    /// while (paginator.HasMore)
    /// {
    ///     var nextPage = await paginator.LoadNextAsync();
    ///     Console.WriteLine($"Loaded {nextPage.Count} more items");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IConvexPagination"/>
    IConvexPagination Pagination { get; }

    #endregion Pagination

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
    /// After invalidation, the next call to the query will fetch fresh data from the server.
    /// This is useful when you know data has changed outside of the normal mutation flow.
    /// </summary>
    /// <param name="queryName">The name of the query to invalidate (e.g., "functions/listTodos"). Must match the function name used in queries.</param>
    /// <returns>A task that completes when invalidation is done.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="queryName"/> is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Invalidate a specific query after external data change
    /// await client.InvalidateQueryAsync("functions/listTodos");
    ///
    /// // Next query will fetch fresh data
    /// var freshTodos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="InvalidateQueriesAsync(string)"/>
    /// <seealso cref="DefineQueryDependency(string, string[])"/>
    Task InvalidateQueryAsync(string queryName);

    /// <summary>
    /// Manually invalidates all queries matching a pattern from the cache.
    /// Supports wildcards: "todos:*" matches "todos:list", "todos:count", etc.
    /// Use this when you need to invalidate multiple related queries at once.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports * and ? wildcards). For example, "todos:*" matches all queries starting with "todos:".</param>
    /// <returns>A task that completes when invalidation is done.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern"/> is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Invalidate all todo-related queries
    /// await client.InvalidateQueriesAsync("functions/todos:*");
    ///
    /// // Invalidate all queries starting with "functions/get"
    /// await client.InvalidateQueriesAsync("functions/get*");
    ///
    /// // Next queries will fetch fresh data
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/todos:list").ExecuteAsync();
    /// var count = await client.Query&lt;int&gt;("functions/todos:count").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="InvalidateQueryAsync(string)"/>
    /// <seealso cref="DefineQueryDependency(string, string[])"/>
    Task InvalidateQueriesAsync(string pattern);

    #endregion Cache & Dependency Tracking

    #region Feature Services

    /// <summary>
    /// File storage operations including upload, download, and metadata management.
    /// </summary>
    /// <example>
    /// <code>
    /// // Upload a file
    /// var storageId = await client.Files.UploadFileAsync(stream, "image/png", "photo.png");
    ///
    /// // Download a file
    /// var fileStream = await client.Files.DownloadFileAsync(storageId);
    /// </code>
    /// </example>
    IConvexFileStorage Files { get; }

    /// <summary>
    /// Vector similarity search operations for semantic search and embeddings.
    /// </summary>
    /// <example>
    /// <code>
    /// // Search by text (auto-generates embedding)
    /// var results = await client.VectorSearch.SearchByTextAsync&lt;Document&gt;("indexName", "search query");
    /// </code>
    /// </example>
    IConvexVectorSearch VectorSearch { get; }

    /// <summary>
    /// HTTP action operations for calling Convex HTTP endpoints directly.
    /// </summary>
    /// <example>
    /// <code>
    /// var response = await client.Http.PostAsync&lt;MyResponse, MyRequest&gt;("/api/endpoint", request);
    /// </code>
    /// </example>
    IConvexHttpActions Http { get; }

    /// <summary>
    /// Function scheduling for delayed, recurring, and interval-based execution.
    /// </summary>
    /// <example>
    /// <code>
    /// // Schedule a function to run in 5 minutes
    /// var jobId = await client.Scheduler.ScheduleAsync("functions/sendReminder", TimeSpan.FromMinutes(5));
    ///
    /// // Schedule a recurring function with cron expression
    /// var recurringJobId = await client.Scheduler.ScheduleRecurringAsync("functions/dailyReport", "0 9 * * *");
    /// </code>
    /// </example>
    IConvexScheduler Scheduler { get; }

    /// <summary>
    /// Authentication and token management for secure API access.
    /// </summary>
    /// <example>
    /// <code>
    /// // Set authentication token
    /// await client.Auth.SetAuthTokenAsync(jwtToken);
    ///
    /// // Clear authentication
    /// await client.Auth.ClearAuthAsync();
    /// </code>
    /// </example>
    IConvexAuthentication Auth { get; }

    /// <summary>
    /// Query result caching for improved performance.
    /// </summary>
    /// <example>
    /// <code>
    /// // Check if a value is cached
    /// if (client.Cache.TryGet&lt;User&gt;("functions/getUser", out var user))
    /// {
    ///     Console.WriteLine($"Cached user: {user.Name}");
    /// }
    /// </code>
    /// </example>
    IConvexCache Cache { get; }

    /// <summary>
    /// Health monitoring and connection metrics.
    /// </summary>
    /// <example>
    /// <code>
    /// var healthCheck = client.Health.CreateHealthCheck(client.ConnectionState, activeSubscriptions);
    /// Console.WriteLine($"Average latency: {client.Health.GetAverageLatency()}ms");
    /// </code>
    /// </example>
    IConvexHealth Health { get; }

    /// <summary>
    /// Performance diagnostics and tracking for debugging and optimization.
    /// </summary>
    /// <example>
    /// <code>
    /// var mark = client.Diagnostics.Performance.Mark("operation-start");
    /// // ... perform operation ...
    /// var measure = client.Diagnostics.Performance.Measure("operation", "operation-start");
    /// </code>
    /// </example>
    IConvexDiagnostics Diagnostics { get; }

    /// <summary>
    /// Resilience patterns including retry policies and circuit breakers.
    /// </summary>
    /// <example>
    /// <code>
    /// client.Resilience.RetryPolicy = RetryPolicy.Aggressive();
    /// var result = await client.Resilience.ExecuteAsync(() => SomeOperationAsync());
    /// </code>
    /// </example>
    IConvexResilience Resilience { get; }

    #endregion Feature Services
}
