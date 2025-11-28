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

    public CachingSlice(ILogger? logger = null, bool enableDebugLogging = false)
        => _implementation = new CacheImplementation(logger, enableDebugLogging);

    public bool TryGet<T>(string queryName, out T? value)
        => _implementation.TryGet(queryName, out value);

    public void Set<T>(string queryName, T value)
        => _implementation.Set(queryName, value);

    public bool TryUpdate<T>(string queryName, Func<T, T> updateFn)
        => _implementation.TryUpdate(queryName, updateFn);

    public bool Remove(string queryName)
        => _implementation.Remove(queryName);

    public int RemovePattern(string pattern)
        => _implementation.RemovePattern(pattern);

    public void Clear()
        => _implementation.Clear();

    public int Count => _implementation.Count;

    public IEnumerable<string> Keys => _implementation.Keys;
}
