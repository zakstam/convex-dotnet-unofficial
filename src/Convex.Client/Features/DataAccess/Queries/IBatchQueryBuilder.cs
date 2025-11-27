using Convex.Client.Infrastructure.ErrorHandling;

namespace Convex.Client.Features.DataAccess.Queries;

/// <summary>
/// Builder for executing multiple queries in a single batch request.
/// All queries execute in parallel on the server for better performance.
/// Batch queries reduce network round-trips and improve efficiency when you need data from multiple queries.
/// </summary>
/// <remarks>
/// <para>
/// Batch queries are more efficient than executing queries individually because:
/// <list type="bullet">
/// <item>They reduce network round-trips (single HTTP request instead of multiple)</item>
/// <item>Queries execute in parallel on the server</item>
/// <item>Results are returned together, reducing latency</item>
/// </list>
/// </para>
/// <para>
/// Results are returned in the same order queries were added. Use tuple overloads for type-safe access,
/// or dictionary overload for accessing results by function name.
/// </para>
/// </remarks>
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
/// <seealso cref="Convex.Client.IConvexClient.Batch"/>
/// <seealso cref="Convex.Client.Infrastructure.Builders.IQueryBuilder{TResult}"/>
public interface IBatchQueryBuilder
{
    /// <summary>
    /// Adds a query to the batch without arguments.
    /// The query will be executed as part of the batch when <see cref="ExecuteAsync(CancellationToken)"/> is called.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the query. This should match the return type of your Convex function.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "functions/listTodos" or "todos:list"). Function names match file paths: `convex/functions/listTodos.ts` becomes `"functions/listTodos"`.</param>
    /// <returns>The builder for method chaining, allowing you to add more queries or execute the batch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Add queries to batch
    /// var batch = client.Batch()
    ///     .Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Query&lt;List&lt;User&gt;&gt;("functions/listUsers")
    ///     .Query&lt;DashboardStats&gt;("functions/getStats");
    /// </code>
    /// </example>
    IBatchQueryBuilder Query<T>(string functionName);

    /// <summary>
    /// Adds a query to the batch with arguments.
    /// The query will be executed as part of the batch when <see cref="ExecuteAsync(CancellationToken)"/> is called.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the query. This should match the return type of your Convex function.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the function. Can be an anonymous type, class, record, or struct.</typeparam>
    /// <param name="functionName">The name of the Convex function (e.g., "functions/getUser" or "users:get"). Function names match file paths: `convex/functions/getUser.ts` becomes `"functions/getUser"`.</param>
    /// <param name="args">The arguments to pass to the function. Must not be null.</param>
    /// <returns>The builder for method chaining, allowing you to add more queries or execute the batch.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="functionName"/> is null or empty, or when <paramref name="args"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Add queries with arguments to batch
    /// var batch = client.Batch()
    ///     .Query&lt;User&gt;("functions/getUser", new { userId = "user123" })
    ///     .Query&lt;List&lt;Message&gt;&gt;("functions/getMessages", new { channelId = "channel456" })
    ///     .Query&lt;Stats&gt;("functions/getStats", new { startDate = DateTime.UtcNow.AddDays(-7) });
    /// </code>
    /// </example>
    IBatchQueryBuilder Query<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

    /// <summary>
    /// Executes all queries in the batch and returns the results as a dictionary keyed by function name.
    /// Useful when you have many queries and want to access results by name rather than position.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a dictionary containing all query results keyed by function name. Values are of type <see cref="object"/> and need to be cast to the expected type.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when any query in the batch fails.</exception>
    /// <remarks>
    /// <para>
    /// This method is useful when you have a dynamic number of queries or when you prefer accessing
    /// results by function name. For type-safe access with a fixed number of queries, use the tuple overloads.
    /// </para>
    /// <para>
    /// Results are returned as <see cref="object"/> and need to be cast to the expected type.
    /// Make sure to cast to the correct type that matches your query's return type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute batch and access results by name
    /// var results = await client.Batch()
    ///     .Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Query&lt;User&gt;("functions/getUser", new { userId = "user123" })
    ///     .Query&lt;DashboardStats&gt;("functions/getStats")
    ///     .ExecuteAsDictionaryAsync();
    ///
    /// var todos = (List&lt;Todo&gt;)results["functions/listTodos"];
    /// var user = (User)results["functions/getUser"];
    /// var stats = (DashboardStats)results["functions/getStats"];
    ///
    /// Console.WriteLine($"Found {todos.Count} todos for {user.Name}");
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsync(CancellationToken)"/>
    Task<Dictionary<string, object>> ExecuteAsDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (2 queries).
    /// The results are returned in the same order as the queries were added.
    /// This overload provides type-safe access to results without casting.
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when any query in the batch fails.</exception>
    /// <remarks>
    /// This method provides type-safe access to results. Make sure the number of type parameters matches
    /// the number of queries added to the batch, and that the types match the query return types.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute 2 queries with type-safe tuple result
    /// var (todos, user) = await client.Batch()
    ///     .Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Query&lt;User&gt;("functions/getUser", new { userId = "user123" })
    ///     .ExecuteAsync&lt;List&lt;Todo&gt;, User&gt;();
    ///
    /// Console.WriteLine($"User {user.Name} has {todos.Count} todos");
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsync{T1, T2, T3}(CancellationToken)"/>
    /// <seealso cref="ExecuteAsDictionaryAsync(CancellationToken)"/>
    Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (3 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (4 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <typeparam name="T4">The type of result from the fourth query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (5 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <typeparam name="T4">The type of result from the fourth query.</typeparam>
    /// <typeparam name="T5">The type of result from the fifth query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (6 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <typeparam name="T4">The type of result from the fourth query.</typeparam>
    /// <typeparam name="T5">The type of result from the fifth query.</typeparam>
    /// <typeparam name="T6">The type of result from the sixth query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3, T4, T5, T6)> ExecuteAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (7 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <typeparam name="T4">The type of result from the fourth query.</typeparam>
    /// <typeparam name="T5">The type of result from the fifth query.</typeparam>
    /// <typeparam name="T6">The type of result from the sixth query.</typeparam>
    /// <typeparam name="T7">The type of result from the seventh query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3, T4, T5, T6, T7)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (8 queries).
    /// </summary>
    /// <typeparam name="T1">The type of result from the first query.</typeparam>
    /// <typeparam name="T2">The type of result from the second query.</typeparam>
    /// <typeparam name="T3">The type of result from the third query.</typeparam>
    /// <typeparam name="T4">The type of result from the fourth query.</typeparam>
    /// <typeparam name="T5">The type of result from the fifth query.</typeparam>
    /// <typeparam name="T6">The type of result from the sixth query.</typeparam>
    /// <typeparam name="T7">The type of result from the seventh query.</typeparam>
    /// <typeparam name="T8">The type of result from the eighth query.</typeparam>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with a tuple containing the query results in order.</returns>
    Task<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7, T8>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as an array.
    /// Use this for a dynamic number of queries when you don't know the count at compile time.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the batch operation.</param>
    /// <returns>A task that completes with an array of results. Results are returned in the same order as queries were added. Values are of type <see cref="object"/> and need to be cast to the expected type.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when any query in the batch fails.</exception>
    /// <remarks>
    /// <para>
    /// This method is useful when you have a dynamic number of queries that you're building at runtime.
    /// For a fixed number of queries, use the tuple overloads for type-safe access.
    /// </para>
    /// <para>
    /// Results are returned as <see cref="object"/> and need to be cast to the expected type.
    /// Make sure to cast to the correct type that matches your query's return type.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Dynamic batch execution
    /// var batch = client.Batch();
    /// foreach (var queryName in queryNames)
    /// {
    ///     batch = batch.Query&lt;object&gt;(queryName);
    /// }
    ///
    /// var results = await batch.ExecuteAsync();
    /// for (int i = 0; i &lt; results.Length; i++)
    /// {
    ///     var result = results[i];
    ///     // Process result...
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteAsDictionaryAsync(CancellationToken)"/>
    /// <seealso cref="ExecuteAsync{T1, T2}(CancellationToken)"/>
    Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default);
}
