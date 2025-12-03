using System.Collections.Concurrent;
using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.Serialization;

namespace Convex.Client.Infrastructure.OptimisticUpdates;

/// <summary>
/// Implementation of IOptimisticLocalStore that uses both the query cache and subscription cache.
/// Tracks modifications for rollback purposes.
/// </summary>
/// <remarks>
/// The local store checks both caches when retrieving query data:
/// 1. First checks the HTTP query cache (IConvexCache)
/// 2. Falls back to the WebSocket subscription cache (_cachedValues from Observe())
/// This ensures optimistic updates work with both Query() and Observe() data.
/// </remarks>
internal sealed class OptimisticLocalStore : IOptimisticLocalStore
{
    private readonly IConvexCache _cache;
    private readonly IConvexSerializer _serializer;
    private readonly ConcurrentDictionary<string, object?>? _subscriptionCache;
    private readonly HashSet<string> _modifiedQueries = [];
    private readonly Dictionary<string, object?> _originalValues = [];

    /// <summary>
    /// Creates a new OptimisticLocalStore with only the query cache.
    /// </summary>
    /// <param name="cache">The HTTP query cache.</param>
    /// <param name="serializer">The serializer for cache key generation.</param>
    public OptimisticLocalStore(IConvexCache cache, IConvexSerializer serializer)
        : this(cache, serializer, null)
    {
    }

    /// <summary>
    /// Creates a new OptimisticLocalStore with both query cache and subscription cache.
    /// </summary>
    /// <param name="cache">The HTTP query cache.</param>
    /// <param name="serializer">The serializer for cache key generation.</param>
    /// <param name="subscriptionCache">The WebSocket subscription cache from Observe(). Can be null.</param>
    public OptimisticLocalStore(IConvexCache cache, IConvexSerializer serializer, ConcurrentDictionary<string, object?>? subscriptionCache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _subscriptionCache = subscriptionCache;
    }

    /// <summary>
    /// Gets the set of query keys that were modified during the optimistic update.
    /// </summary>
    public IReadOnlyCollection<string> ModifiedQueries => _modifiedQueries;

    /// <summary>
    /// Gets the original values of queries before they were modified.
    /// Used for rollback purposes.
    /// </summary>
    public IReadOnlyDictionary<string, object?> OriginalValues => _originalValues;

    /// <inheritdoc/>
    public TResult? GetQuery<TResult>(string queryName, object? args = null)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentException("Query name cannot be null or whitespace.", nameof(queryName));
        }

        var cacheKey = GenerateCacheKey(queryName, args);

        // First, check the HTTP query cache
        if (_cache.TryGet<TResult>(cacheKey, out var value))
        {
            return value;
        }

        // Fall back to the WebSocket subscription cache (from Observe())
        return _subscriptionCache != null
            && _subscriptionCache.TryGetValue(cacheKey, out var subscriptionValue)
            && subscriptionValue is TResult typedValue
            ? typedValue
            : default;
    }

    /// <inheritdoc/>
    public IEnumerable<QueryResult<TResult, TArgs>> GetAllQueries<TResult, TArgs>(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentException("Query name cannot be null or whitespace.", nameof(queryName));
        }

        var results = new List<QueryResult<TResult, TArgs>>();
        var prefix = $"{queryName}:";

        // Iterate through all cache keys that start with the query name prefix
        foreach (var cacheKey in _cache.Keys)
        {
            if (cacheKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                // Extract args from cache key (format: "queryName:serializedArgs")
                var argsString = cacheKey.Substring(prefix.Length);

                // Try to deserialize args
                TArgs? args = default;
                if (!string.IsNullOrEmpty(argsString))
                {
                    try
                    {
                        args = _serializer.Deserialize<TArgs>(argsString);
                    }
                    catch
                    {
                        // If deserialization fails, skip this entry
                        continue;
                    }
                }

                // Get the query result
                TResult? value = default;
                if (_cache.TryGet<TResult>(cacheKey, out var cachedValue))
                {
                    value = cachedValue;
                }

                results.Add(new QueryResult<TResult, TArgs>(args, value));
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public void SetQuery<TResult>(string queryName, TResult? value, object? args = null)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentException("Query name cannot be null or whitespace.", nameof(queryName));
        }

        var cacheKey = GenerateCacheKey(queryName, args);

        // Capture original value BEFORE modifying (if it exists)
        if (!_modifiedQueries.Contains(cacheKey))
        {
            // This is the first time modifying this query in this optimistic update
            // Capture the original value for rollback
            if (_cache.TryGet<object>(cacheKey, out var originalValue))
            {
                _originalValues[cacheKey] = originalValue;
            }
            else
            {
                // Query didn't exist before - mark as null for removal on rollback
                _originalValues[cacheKey] = null;
            }
        }

        _ = _modifiedQueries.Add(cacheKey);

        if (value == null)
        {
            // Remove the query from cache
            _ = _cache.Remove(cacheKey);
        }
        else
        {
            // Set the optimistic value
            _cache.Set(cacheKey, value);
        }
    }

    /// <summary>
    /// Generates a cache key for a query name and arguments.
    /// Matches the format used by ConvexClient: "functionName:serializedArgs"
    /// </summary>
    private string GenerateCacheKey(string queryName, object? args)
    {
        if (args == null)
        {
            return queryName;
        }

        var serializedArgs = _serializer.Serialize(args);
        return $"{queryName}:{serializedArgs}";
    }
}

