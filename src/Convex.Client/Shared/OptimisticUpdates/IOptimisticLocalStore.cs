namespace Convex.Client.Shared.OptimisticUpdates;

/// <summary>
/// A view of the query results currently in the Convex client for use within optimistic updates.
/// This interface mirrors convex-js's OptimisticLocalStore API for query-focused optimistic updates.
/// </summary>
/// <remarks>
/// Query results should be treated as immutable!
/// Always make new copies of structures within query results to avoid corrupting data within the client.
/// </remarks>
public interface IOptimisticLocalStore
{
    /// <summary>
    /// Retrieve the result of a query from the client.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <param name="queryName">The name of the query function.</param>
    /// <param name="args">The arguments object for this query. Can be null if the query takes no arguments.</param>
    /// <returns>The query result or null if the query is not currently in the client.</returns>
    TResult? GetQuery<TResult>(string queryName, object? args = null);

    /// <summary>
    /// Retrieve the results and arguments of all queries with a given name.
    /// This is useful for complex optimistic updates that need to inspect and update many query results
    /// (for example updating a paginated list).
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <typeparam name="TArgs">The type of the query arguments.</typeparam>
    /// <param name="queryName">The name of the query function.</param>
    /// <returns>
    /// An array of objects, one for each query of the given name.
    /// Each object includes:
    /// - Args - The arguments object for the query.
    /// - Value - The query result or null if the query is loading.
    /// </returns>
    IEnumerable<QueryResult<TResult, TArgs>> GetAllQueries<TResult, TArgs>(string queryName);

    /// <summary>
    /// Optimistically update the result of a query.
    /// This can either be a new value (perhaps derived from the old value from GetQuery)
    /// or null to remove the query. Removing a query is useful to create loading states
    /// while Convex recomputes the query results.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <param name="queryName">The name of the query function.</param>
    /// <param name="args">The arguments object for this query. Can be null if the query takes no arguments.</param>
    /// <param name="value">The new value to set the query to or null to remove it from the client.</param>
    void SetQuery<TResult>(string queryName, TResult? value, object? args = null);
}

/// <summary>
/// Represents a query result with its arguments.
/// </summary>
/// <typeparam name="TResult">The type of the query result.</typeparam>
/// <typeparam name="TArgs">The type of the query arguments.</typeparam>
public sealed record QueryResult<TResult, TArgs>(TArgs? Args, TResult? Value);

