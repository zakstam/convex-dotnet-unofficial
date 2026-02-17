namespace Convex.Client.Infrastructure.Caching;

/// <summary>
/// Defines operations for i convex cache.
/// Thread-safe for concurrent reads and writes.
/// </summary>
public interface IConvexCache
{
    /// <summary>
    /// Tries to get a cached query result.
    /// </summary>
    bool TryGet<T>(string queryName, out T? value);

    /// <summary>
    /// Sets the operation.
    /// </summary>
    void Set<T>(string queryName, T value);

    /// <summary>
    /// Attempts to update the operation.
    /// If the value doesn't exist in cache, the update function is not called.
    /// </summary>
    bool TryUpdate<T>(string queryName, Func<T, T> updateFn);

    /// <summary>
    /// Removes a query result from the cache.
    /// </summary>
    bool Remove(string queryName);

    /// <summary>
    /// Removes all cached query results matching a pattern (e.g., "todos:*").
    /// </summary>
    int RemovePattern(string pattern);

    /// <summary>
    /// Clears all cached query results.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the count.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the keys.
    /// </summary>
    IEnumerable<string> Keys { get; }
}

/// <summary>
/// Exception thrown when cache operations fail.
/// </summary>
public class ConvexCacheException(string message, string? queryName = null, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the query name.
    /// </summary>
    public string? QueryName { get; } = queryName;
}
