using Convex.Client.Shared.Caching;
using Convex.Client.Shared.Serialization;

namespace Convex.Client.Shared.OptimisticUpdates;

/// <summary>
/// Implementation of IOptimisticLocalStore that uses the query cache.
/// Tracks modifications for rollback purposes.
/// </summary>
internal sealed class OptimisticLocalStore(IConvexCache cache, IConvexSerializer serializer) : IOptimisticLocalStore
{
    private readonly IConvexCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly HashSet<string> _modifiedQueries = [];
    private readonly Dictionary<string, object?> _originalValues = [];

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
        if (_cache.TryGet<TResult>(cacheKey, out var value))
        {
            return value;
        }

        return default;
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

