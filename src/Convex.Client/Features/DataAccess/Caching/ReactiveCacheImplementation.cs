using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.DataAccess.Caching;

/// <summary>
/// Thread-safe reactive cache implementation that notifies subscribers when values change.
/// This unified cache replaces both the HTTP query cache and subscription cache.
/// </summary>
internal sealed class ReactiveCacheImplementation : IReactiveCache
{
    private readonly ConcurrentDictionary<string, ReactiveEntry> _entries = new();
    private readonly ILogger? _logger;
    private readonly bool _enableDebugLogging;
    private readonly object _disposeLock = new();
    private bool _isDisposed;

    // Static cache for compiled regex patterns to avoid recompilation on repeated RemovePattern calls
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReactiveCacheImplementation"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for debug logging.</param>
    /// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
    public ReactiveCacheImplementation(ILogger? logger = null, bool enableDebugLogging = false)
    {
        _logger = logger;
        _enableDebugLogging = enableDebugLogging;
    }

    /// <inheritdoc/>
    public IObservable<T?> GetObservable<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        var entry = GetOrCreateEntry(key);

        // Return typed observable that casts from object? to T?
        return entry.Observable.Select(value => value is T typedValue ? typedValue : default);
    }

    /// <inheritdoc/>
    public void SetAndNotify<T>(string key, T? value, CacheEntrySource source)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        var entry = GetOrCreateEntry(key);
        entry.SetValue(value, source);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[ReactiveCache] SetAndNotify: Key={Key}, Type={Type}, Source={Source}, CacheSize={CacheSize}",
                key, typeof(T).Name, source, _entries.Count);
        }
    }

    /// <inheritdoc/>
    public T? GetCurrentValue<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_entries.TryGetValue(key, out var entry) && entry.Value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <inheritdoc/>
    public bool TryGetSource(string key, out CacheEntrySource source)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            source = entry.Source;
            return true;
        }

        source = default;
        return false;
    }

    #region IConvexCache Implementation

    /// <inheritdoc/>
    public bool TryGet<T>(string queryName, out T? value)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        if (_entries.TryGetValue(queryName, out var entry) && entry.Value is T typedValue)
        {
            value = typedValue;

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var ageMs = (DateTimeOffset.UtcNow - entry.Timestamp).TotalMilliseconds;
                _logger!.LogDebug("[ReactiveCache] Hit: Key={QueryName}, Type={Type}, Age={AgeMs}ms, Source={Source}",
                    queryName, typeof(T).Name, ageMs, entry.Source);
            }

            return true;
        }

        value = default;

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[ReactiveCache] Miss: Key={QueryName}, Type={Type}",
                queryName, typeof(T).Name);
        }

        return false;
    }

    /// <inheritdoc/>
    public void Set<T>(string queryName, T value)
    {
        // Default to Query source for backwards compatibility with IConvexCache.Set
        SetAndNotify(queryName, value, CacheEntrySource.Query);
    }

    /// <inheritdoc/>
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

        if (!_entries.TryGetValue(queryName, out var entry))
        {
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[ReactiveCache] TryUpdate failed - key not found: Key={QueryName}", queryName);
            }
            return false;
        }

        if (entry.Value is not T typedValue)
        {
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogWarning("[ReactiveCache] TryUpdate failed - type mismatch: Key={QueryName}, Expected={Expected}, Actual={Actual}",
                    queryName, typeof(T).Name, entry.ValueType?.Name ?? "null");
            }
            return false;
        }

        var newValue = updateFn(typedValue);
        entry.SetValue(newValue, entry.Source);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[ReactiveCache] TryUpdate success: Key={QueryName}, Type={Type}",
                queryName, typeof(T).Name);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool Remove(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        if (_entries.TryRemove(queryName, out var entry))
        {
            // Notify subscribers that the value is removed (null)
            entry.Clear();
            entry.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public int RemovePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        // Get or create cached regex pattern to avoid recompilation
        var regex = _regexCache.GetOrAdd(pattern, p =>
        {
            var regexPattern = "^" + Regex.Escape(p).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        });

        var keysToRemove = _entries.Keys.Where(key => regex.IsMatch(key)).ToList();

        var removedCount = 0;
        foreach (var key in keysToRemove)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                entry.Clear();
                entry.Dispose();
                removedCount++;
            }
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[ReactiveCache] RemovePattern: Pattern={Pattern}, Matched={MatchedCount}, Removed={RemovedCount}",
                pattern, keysToRemove.Count, removedCount);
        }

        return removedCount;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        var keys = _entries.Keys.ToList();
        foreach (var key in keys)
        {
            if (_entries.TryRemove(key, out var entry))
            {
                entry.Clear();
                entry.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public int Count => _entries.Count;

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _entries.Keys;

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            // Dispose all entries
            foreach (var entry in _entries.Values)
            {
                entry.Dispose();
            }

            _entries.Clear();
        }
    }

    private ReactiveEntry GetOrCreateEntry(string key)
    {
        return _entries.GetOrAdd(key, _ => new ReactiveEntry());
    }
}
