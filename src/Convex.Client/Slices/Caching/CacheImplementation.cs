using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Convex.Client.Slices.Caching;

/// <summary>
/// Thread-safe in-memory cache implementation for query results.
/// </summary>
internal sealed class CacheImplementation
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    // Static cache for compiled regex patterns to avoid recompilation on repeated RemovePattern calls
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    public bool TryGet<T>(string queryName, out T? value)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        if (_cache.TryGetValue(queryName, out var entry) && entry.Value is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public void Set<T>(string queryName, T value)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        _cache[queryName] = new CacheEntry(value, typeof(T), DateTimeOffset.UtcNow);
    }

    public bool TryUpdate<T>(string queryName, Func<T, T> updateFn)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        if (updateFn == null)
        {
            throw new ArgumentNullException(nameof(updateFn));
        }

        // Check if key exists and value is of correct type
        if (!_cache.TryGetValue(queryName, out var existingEntry))
        {
            return false;
        }

        if (existingEntry.Value is not T typedValue)
        {
            return false;
        }

        // Use AddOrUpdate with a factory function to atomically update the value
        // This avoids the reference equality issue with TryUpdate
        var updated = false;
        _ = _cache.AddOrUpdate(
            queryName,
            new CacheEntry(updateFn(typedValue), typeof(T), DateTimeOffset.UtcNow), // addValueFactory (shouldn't be called)
            (key, oldEntry) =>
            {
                // updateValueFactory - only called if key exists
                if (oldEntry.Value is T oldTypedValue)
                {
                    updated = true;
                    return new CacheEntry(updateFn(oldTypedValue), typeof(T), DateTimeOffset.UtcNow);
                }
                return oldEntry; // Type mismatch, don't update
            });

        return updated;
    }

    public bool Remove(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        return _cache.TryRemove(queryName, out _);
    }

    public int RemovePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        // Get or create cached regex pattern to avoid recompilation
        // Convert glob pattern to regex
        // Supports: "todos:*" -> matches "todos:list", "todos:count", etc.
        var regex = _regexCache.GetOrAdd(pattern, p =>
        {
            var regexPattern = "^" + Regex.Escape(p).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        });

        var keysToRemove = _cache.Keys.Where(key => regex.IsMatch(key)).ToList();

        var removedCount = 0;
        foreach (var key in keysToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    public void Clear() => _cache.Clear();

    public int Count => _cache.Count;

    public IEnumerable<string> Keys => _cache.Keys;

    /// <summary>
    /// Internal cache entry storing value, type, and metadata.
    /// </summary>
    private sealed class CacheEntry(object? value, Type valueType, DateTimeOffset timestamp)
    {
        public object? Value { get; } = value;
        public Type ValueType { get; } = valueType;
        public DateTimeOffset Timestamp { get; } = timestamp;
    }
}
