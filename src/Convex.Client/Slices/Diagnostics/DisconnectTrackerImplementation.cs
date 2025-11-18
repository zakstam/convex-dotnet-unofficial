namespace Convex.Client.Slices.Diagnostics;

/// <summary>
/// Internal implementation of disconnection tracking.
/// Tracks long disconnections and provides diagnostic statistics.
/// </summary>
internal sealed class DisconnectTrackerImplementation(TimeSpan? longDisconnectThreshold = null) : IDisconnectTracker
{
    private DateTimeOffset? _disconnectedAt;
    private readonly TimeSpan _longDisconnectThreshold = longDisconnectThreshold ?? TimeSpan.FromSeconds(30);
    private readonly List<DisconnectEvent> _disconnectHistory = [];
    private readonly object _lock = new();
    private const int MaxHistorySize = 50;

    public bool IsDisconnected
    {
        get
        {
            lock (_lock)
            {
                return _disconnectedAt.HasValue;
            }
        }
    }

    public TimeSpan? CurrentDisconnectDuration
    {
        get
        {
            lock (_lock)
            {
                if (_disconnectedAt.HasValue)
                {
                    return DateTimeOffset.UtcNow - _disconnectedAt.Value;
                }
                return null;
            }
        }
    }

    public bool IsLongDisconnect
    {
        get
        {
            var duration = CurrentDisconnectDuration;
            return duration.HasValue && duration.Value > _longDisconnectThreshold;
        }
    }

    public IReadOnlyList<DisconnectEvent> DisconnectHistory
    {
        get
        {
            lock (_lock)
            {
                return [.. _disconnectHistory];
            }
        }
    }

    public void RecordDisconnect()
    {
        lock (_lock)
        {
            _disconnectedAt = DateTimeOffset.UtcNow;
        }
    }

    public void RecordReconnect()
    {
        lock (_lock)
        {
            if (_disconnectedAt.HasValue)
            {
                var duration = DateTimeOffset.UtcNow - _disconnectedAt.Value;
                var wasLong = duration > _longDisconnectThreshold;

                var disconnectEvent = new DisconnectEvent
                {
                    DisconnectedAt = _disconnectedAt.Value,
                    ReconnectedAt = DateTimeOffset.UtcNow,
                    Duration = duration,
                    WasLongDisconnect = wasLong
                };

                _disconnectHistory.Add(disconnectEvent);

                // Keep history size bounded
                while (_disconnectHistory.Count > MaxHistorySize)
                {
                    _disconnectHistory.RemoveAt(0);
                }

                _disconnectedAt = null;
            }
        }
    }

    public DisconnectStats GetStats()
    {
        lock (_lock)
        {
            if (_disconnectHistory.Count == 0)
            {
                return new DisconnectStats
                {
                    TotalDisconnects = 0,
                    LongDisconnects = 0,
                    AverageDisconnectDuration = TimeSpan.Zero,
                    LongestDisconnect = TimeSpan.Zero
                };
            }

            return new DisconnectStats
            {
                TotalDisconnects = _disconnectHistory.Count,
                LongDisconnects = _disconnectHistory.Count(e => e.WasLongDisconnect),
                AverageDisconnectDuration = TimeSpan.FromTicks((long)_disconnectHistory.Average(e => e.Duration.Ticks)),
                LongestDisconnect = TimeSpan.FromTicks(_disconnectHistory.Max(e => e.Duration.Ticks)),
                ShortestDisconnect = TimeSpan.FromTicks(_disconnectHistory.Min(e => e.Duration.Ticks))
            };
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _disconnectHistory.Clear();
            _disconnectedAt = null;
        }
    }
}
