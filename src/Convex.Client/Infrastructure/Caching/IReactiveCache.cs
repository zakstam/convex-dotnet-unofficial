namespace Convex.Client.Infrastructure.Caching;

/// <summary>
/// A reactive cache that extends IConvexCache with observable notifications.
/// When values are set via SetAndNotify, all subscribers receive the update.
/// This enables optimistic updates to notify Observe() subscribers immediately.
/// </summary>
public interface IReactiveCache : IConvexCache, IDisposable
{
    /// <summary>
    /// Gets observable.
    /// New subscribers receive notifications for all future value changes.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key (query name with optional serialized args).</param>
    /// <returns>An observable that emits when the value changes.</returns>
    IObservable<T?> GetObservable<T>(string key);

    /// <summary>
    /// Sets and notify.
    /// This is the primary method for triggering reactive updates.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="source">The source of this value (Query, Subscription, or OptimisticUpdate).</param>
    void SetAndNotify<T>(string key, T? value, CacheEntrySource source);

    /// <summary>
    /// Gets current value.
    /// Unlike TryGet, this returns the value directly without an out parameter.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or default if not found.</returns>
    T? GetCurrentValue<T>(string key);

    /// <summary>
    /// Attempts to get source.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="source">The source of the cached entry.</param>
    /// <returns>True if the entry exists, false otherwise.</returns>
    bool TryGetSource(string key, out CacheEntrySource source);
}
