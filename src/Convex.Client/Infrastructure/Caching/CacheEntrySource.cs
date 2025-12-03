namespace Convex.Client.Infrastructure.Caching;

/// <summary>
/// Indicates the source of a cache entry.
/// </summary>
public enum CacheEntrySource
{
    /// <summary>
    /// Value came from an HTTP Query() execution.
    /// </summary>
    Query = 0,

    /// <summary>
    /// Value came from a WebSocket Observe() subscription.
    /// </summary>
    Subscription = 1,

    /// <summary>
    /// Value was set via SetQuery() in an optimistic update.
    /// </summary>
    OptimisticUpdate = 2
}
