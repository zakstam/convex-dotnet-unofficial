using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.Serialization;

namespace Convex.Client.Infrastructure.OptimisticUpdates;

/// <summary>
/// Implementation of IOptimisticLocalStore that uses the unified reactive cache.
/// Tracks modifications for rollback purposes.
/// </summary>
/// <remarks>
/// The local store uses the reactive cache which holds all cached values from:
/// - HTTP queries (Query())
/// - WebSocket subscriptions (Observe())
/// - Optimistic updates (SetQuery())
/// When SetQuery() is called, subscribers are automatically notified via the reactive cache.
/// </remarks>
internal sealed class OptimisticLocalStore : IOptimisticLocalStore
{
    private readonly IReactiveCache _reactiveCache;
    private readonly IConvexSerializer _serializer;
    private readonly HashSet<string> _modifiedQueries = [];
    private readonly Dictionary<string, object?> _originalValues = [];

    /// <summary>
    /// Creates a new OptimisticLocalStore with a reactive cache.
    /// </summary>
    /// <param name="reactiveCache">The reactive cache for optimistic updates with subscriber notifications.</param>
    /// <param name="serializer">The serializer for cache key generation.</param>
    public OptimisticLocalStore(IReactiveCache reactiveCache, IConvexSerializer serializer)
    {
        _reactiveCache = reactiveCache ?? throw new ArgumentNullException(nameof(reactiveCache));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
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

        // Use the unified reactive cache which contains values from both Query() and Observe()
        return _reactiveCache.GetCurrentValue<TResult>(cacheKey);
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
        foreach (var cacheKey in _reactiveCache.Keys)
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
                var value = _reactiveCache.GetCurrentValue<TResult>(cacheKey);
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
            _originalValues[cacheKey] = _reactiveCache.GetCurrentValue<object>(cacheKey);
        }

        _ = _modifiedQueries.Add(cacheKey);

        if (value is null)
        {
            // Remove the query from cache
            _ = _reactiveCache.Remove(cacheKey);
        }
        else
        {
            // Set the optimistic value and notify all subscribers
            // This is the key change - SetAndNotify triggers Observe() callbacks immediately
            _reactiveCache.SetAndNotify(cacheKey, value, CacheEntrySource.OptimisticUpdate);
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

