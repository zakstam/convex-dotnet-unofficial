using Convex.Client.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.DataAccess.Caching;

/// <summary>
/// Caching slice - provides in-memory caching for query results with optimistic updates.
/// This is a self-contained vertical slice that handles all caching functionality.
/// </summary>
public class CachingSlice : IConvexCache
{
    private readonly CacheImplementation _implementation;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingSlice"/> class.
    /// </summary>
    public CachingSlice(ILogger? logger = null, bool enableDebugLogging = false)
        => _implementation = new CacheImplementation(logger, enableDebugLogging);

    /// <summary>
    /// Attempts to get the operation.
    /// </summary>
    public bool TryGet<T>(string queryName, out T? value)
        => _implementation.TryGet(queryName, out value);

    /// <summary>
    /// Sets the operation.
    /// </summary>
    public void Set<T>(string queryName, T value)
        => _implementation.Set(queryName, value);

    /// <summary>
    /// Attempts to update the operation.
    /// </summary>
    public bool TryUpdate<T>(string queryName, Func<T, T> updateFn)
        => _implementation.TryUpdate(queryName, updateFn);

    /// <summary>
    /// Executes the remove operation.
    /// </summary>
    public bool Remove(string queryName)
        => _implementation.Remove(queryName);

    /// <summary>
    /// Executes the remove pattern operation.
    /// </summary>
    public int RemovePattern(string pattern)
        => _implementation.RemovePattern(pattern);

    /// <summary>
    /// Clears the operation.
    /// </summary>
    public void Clear()
        => _implementation.Clear();

    /// <summary>
    /// Gets the count.
    /// </summary>
    public int Count => _implementation.Count;

    /// <summary>
    /// Gets the keys.
    /// </summary>
    public IEnumerable<string> Keys => _implementation.Keys;
}
