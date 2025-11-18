namespace Convex.Client.Slices.Queries;

/// <summary>
/// Builder for executing multiple queries in a single batch request.
/// All queries execute in parallel on the server for better performance.
/// </summary>
public interface IBatchQueryBuilder
{
    /// <summary>
    /// Adds a query to the batch without arguments.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the query.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <returns>The builder for method chaining.</returns>
    IBatchQueryBuilder Query<T>(string functionName);

    /// <summary>
    /// Adds a query to the batch with arguments.
    /// </summary>
    /// <typeparam name="T">The type of result returned by the query.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the function.</typeparam>
    /// <param name="functionName">The name of the Convex function.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The builder for method chaining.</returns>
    IBatchQueryBuilder Query<T, TArgs>(string functionName, TArgs args) where TArgs : notnull;

    /// <summary>
    /// Executes all queries in the batch and returns the results as a dictionary keyed by function name.
    /// Useful when you have many queries and want to access results by name rather than position.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a dictionary containing all query results keyed by function name.</returns>
    /// <example>
    /// <code>
    /// var results = await client.Batch()
    ///     .Query&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .Query&lt;User&gt;("users:current")
    ///     .Query&lt;Stats&gt;("dashboard:stats")
    ///     .ExecuteAsDictionaryAsync();
    ///
    /// var todos = (List&lt;Todo&gt;)results["todos:list"];
    /// var user = (User)results["users:current"];
    /// var stats = (Stats)results["dashboard:stats"];
    /// </code>
    /// </example>
    Task<Dictionary<string, object>> ExecuteAsDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple.
    /// The results are returned in the same order as the queries were added.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a tuple containing all query results.</returns>
    /// <remarks>
    /// This method has multiple overloads for different numbers of queries (2-8).
    /// Use ExecuteAsync() for dynamic number of queries.
    /// </remarks>
    Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (3 queries).
    /// </summary>
    Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (4 queries).
    /// </summary>
    Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (5 queries).
    /// </summary>
    Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (6 queries).
    /// </summary>
    Task<(T1, T2, T3, T4, T5, T6)> ExecuteAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (7 queries).
    /// </summary>
    Task<(T1, T2, T3, T4, T5, T6, T7)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as a tuple (8 queries).
    /// </summary>
    Task<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7, T8>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all queries in the batch and returns the results as an array.
    /// Use this for a dynamic number of queries.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with an array of results.</returns>
    Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default);
}
