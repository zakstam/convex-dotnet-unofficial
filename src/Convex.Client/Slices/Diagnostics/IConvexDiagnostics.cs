using System.Text.Json;

namespace Convex.Client.Slices.Diagnostics;

/// <summary>
/// Provides performance tracking and diagnostics for Convex client operations.
/// Thread-safe for concurrent performance measurement and disconnect tracking.
/// </summary>
public interface IConvexDiagnostics
{
    /// <summary>
    /// Gets the performance tracker for detailed timing measurements.
    /// </summary>
    IPerformanceTracker Performance { get; }

    /// <summary>
    /// Gets the disconnect tracker for monitoring connection disruptions.
    /// </summary>
    IDisconnectTracker Disconnects { get; }
}

/// <summary>
/// Tracks performance marks and measures similar to browser Performance API.
/// </summary>
public interface IPerformanceTracker
{
    /// <summary>
    /// Gets all performance entries.
    /// </summary>
    IReadOnlyList<PerformanceEntry> Entries { get; }

    /// <summary>
    /// Creates a named performance mark at the current time.
    /// </summary>
    PerformanceMark Mark(string markName, JsonElement? detail = null);

    /// <summary>
    /// Creates a performance measure between two marks.
    /// </summary>
    PerformanceMeasure Measure(string measureName, string? startMark = null, string? endMark = null);

    /// <summary>
    /// Gets entries by name.
    /// </summary>
    IReadOnlyList<PerformanceEntry> GetEntriesByName(string name);

    /// <summary>
    /// Gets entries by type ("mark" or "measure").
    /// </summary>
    IReadOnlyList<PerformanceEntry> GetEntriesByType(string type);

    /// <summary>
    /// Clears all marks.
    /// </summary>
    void ClearMarks();

    /// <summary>
    /// Clears all measures.
    /// </summary>
    void ClearMeasures();

    /// <summary>
    /// Clears all marks and measures.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a summary of all measures grouped by name.
    /// </summary>
    PerformanceSummary GetSummary();
}

/// <summary>
/// Tracks disconnection events and provides statistics.
/// </summary>
public interface IDisconnectTracker
{
    /// <summary>
    /// Gets whether currently disconnected.
    /// </summary>
    bool IsDisconnected { get; }

    /// <summary>
    /// Gets the current disconnect duration, if disconnected.
    /// </summary>
    TimeSpan? CurrentDisconnectDuration { get; }

    /// <summary>
    /// Gets whether the current disconnection is considered long.
    /// </summary>
    bool IsLongDisconnect { get; }

    /// <summary>
    /// Gets the disconnect history.
    /// </summary>
    IReadOnlyList<DisconnectEvent> DisconnectHistory { get; }

    /// <summary>
    /// Records a disconnection event.
    /// </summary>
    void RecordDisconnect();

    /// <summary>
    /// Records a reconnection event.
    /// </summary>
    void RecordReconnect();

    /// <summary>
    /// Gets statistics about disconnect events.
    /// </summary>
    DisconnectStats GetStats();

    /// <summary>
    /// Clears disconnect history.
    /// </summary>
    void Clear();
}

/// <summary>
/// Base class for performance entries.
/// </summary>
public abstract record PerformanceEntry
{
    public string Name { get; init; } = string.Empty;
    public string EntryType { get; init; } = string.Empty;
    public TimeSpan Timestamp { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Represents a point in time for performance measurement.
/// </summary>
public record PerformanceMark : PerformanceEntry
{
    public JsonElement? Detail { get; init; }

    public PerformanceMark(string name, TimeSpan timestamp, JsonElement? detail = null)
    {
        Name = name;
        EntryType = "mark";
        Timestamp = timestamp;
        Duration = TimeSpan.Zero;
        Detail = detail;
    }

    public override string ToString() => $"Mark '{Name}' at {Timestamp.TotalMilliseconds:F2}ms";
}

/// <summary>
/// Represents a duration between two marks.
/// </summary>
public record PerformanceMeasure : PerformanceEntry
{
    public PerformanceMeasure(string name, TimeSpan startTime, TimeSpan duration)
    {
        Name = name;
        EntryType = "measure";
        Timestamp = startTime;
        Duration = duration;
    }

    public override string ToString() => $"Measure '{Name}': {Duration.TotalMilliseconds:F2}ms (at {Timestamp.TotalMilliseconds:F2}ms)";
}

/// <summary>
/// Statistics for a specific measure name.
/// </summary>
public record PerformanceMeasureStats
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public TimeSpan MinDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }

    public override string ToString() =>
        $"{Name}: {Count} measurements, avg {AverageDuration.TotalMilliseconds:F2}ms, " +
        $"min {MinDuration.TotalMilliseconds:F2}ms, max {MaxDuration.TotalMilliseconds:F2}ms";
}

/// <summary>
/// Summary of all performance measurements.
/// </summary>
public record PerformanceSummary
{
    public int TotalMarks { get; init; }
    public int TotalMeasures { get; init; }
    public List<PerformanceMeasureStats> MeasureStats { get; init; } = [];

    public override string ToString() =>
        $"Performance Summary: {TotalMarks} marks, {TotalMeasures} measures, {MeasureStats.Count} unique operations";
}

/// <summary>
/// Represents a disconnection event.
/// </summary>
public record DisconnectEvent
{
    public DateTimeOffset DisconnectedAt { get; init; }
    public DateTimeOffset ReconnectedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public bool WasLongDisconnect { get; init; }

    public override string ToString() =>
        $"Disconnected for {Duration.TotalSeconds:F1}s {(WasLongDisconnect ? "(LONG)" : "")} " +
        $"from {DisconnectedAt:HH:mm:ss} to {ReconnectedAt:HH:mm:ss}";
}

/// <summary>
/// Statistics about disconnect events.
/// </summary>
public record DisconnectStats
{
    public int TotalDisconnects { get; init; }
    public int LongDisconnects { get; init; }
    public TimeSpan AverageDisconnectDuration { get; init; }
    public TimeSpan LongestDisconnect { get; init; }
    public TimeSpan ShortestDisconnect { get; init; }

    public double LongDisconnectRate => TotalDisconnects == 0 ? 0 : (LongDisconnects / (double)TotalDisconnects) * 100;

    public override string ToString() =>
        $"Disconnects: {TotalDisconnects} total, {LongDisconnects} long ({LongDisconnectRate:F1}%), " +
        $"avg {AverageDisconnectDuration.TotalSeconds:F1}s, longest {LongestDisconnect.TotalSeconds:F1}s";
}
