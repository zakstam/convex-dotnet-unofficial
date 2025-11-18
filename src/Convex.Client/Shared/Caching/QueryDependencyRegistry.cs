using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Convex.Client.Shared.Caching;

/// <summary>
/// Thread-safe registry for tracking query dependencies and invalidation rules.
/// Manages which queries should be invalidated when mutations execute.
/// </summary>
public sealed class QueryDependencyRegistry
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _mutationToQueries = new();
    private readonly object _lock = new();

    /// <summary>
    /// Defines that a mutation should invalidate specific queries when it executes.
    /// </summary>
    /// <param name="mutationName">The name of the mutation (e.g., "todos:create").</param>
    /// <param name="invalidates">The queries to invalidate (e.g., "todos:list", "todos:count").</param>
    /// <remarks>
    /// When the specified mutation executes, all listed queries will be invalidated.
    /// This can be used to ensure the UI stays up-to-date after mutations.
    /// </remarks>
    public void DefineQueryDependency(string mutationName, params string[] invalidates)
    {
        if (string.IsNullOrWhiteSpace(mutationName))
        {
            throw new ArgumentNullException(nameof(mutationName));
        }

        if (invalidates == null || invalidates.Length == 0)
        {
            throw new ArgumentException("At least one query to invalidate must be specified.", nameof(invalidates));
        }

        lock (_lock)
        {
            if (!_mutationToQueries.TryGetValue(mutationName, out var queries))
            {
                queries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _mutationToQueries[mutationName] = queries;
            }

            foreach (var query in invalidates)
            {
                if (!string.IsNullOrWhiteSpace(query))
                {
                    _ = queries.Add(query);
                }
            }
        }
    }

    /// <summary>
    /// Gets all queries that should be invalidated when a specific mutation executes.
    /// </summary>
    /// <param name="mutationName">The name of the mutation.</param>
    /// <returns>A collection of query names to invalidate, or empty if no dependencies defined.</returns>
    public IReadOnlyCollection<string> GetQueriesToInvalidate(string mutationName)
    {
        if (string.IsNullOrWhiteSpace(mutationName))
        {
            return [];
        }

        if (_mutationToQueries.TryGetValue(mutationName, out var queries))
        {
            lock (_lock)
            {
                return [.. queries];
            }
        }

        return [];
    }

    /// <summary>
    /// Removes all dependency definitions for a specific mutation.
    /// </summary>
    /// <param name="mutationName">The name of the mutation.</param>
    /// <returns>True if dependencies were removed, false if none existed.</returns>
    public bool RemoveDependencies(string mutationName)
    {
        return string.IsNullOrWhiteSpace(mutationName)
            ? throw new ArgumentNullException(nameof(mutationName))
            : _mutationToQueries.TryRemove(mutationName, out _);
    }

    /// <summary>
    /// Clears all dependency definitions.
    /// </summary>
    public void Clear() => _mutationToQueries.Clear();

    /// <summary>
    /// Gets the total number of mutations with defined dependencies.
    /// </summary>
    public int Count => _mutationToQueries.Count;

    /// <summary>
    /// Expands patterns like "todos:*" into actual query names from the cache.
    /// </summary>
    /// <param name="pattern">The pattern to expand (supports * and ? wildcards).</param>
    /// <param name="cachedQueryNames">Collection of currently cached query names.</param>
    /// <returns>Collection of query names matching the pattern.</returns>
    public static IEnumerable<string> ExpandPattern(string pattern, IEnumerable<string> cachedQueryNames)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            yield break;
        }

        // If no wildcards, return the pattern itself
        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            yield return pattern;
            yield break;
        }

        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        foreach (var queryName in cachedQueryNames)
        {
            if (regex.IsMatch(queryName))
            {
                yield return queryName;
            }
        }
    }
}
