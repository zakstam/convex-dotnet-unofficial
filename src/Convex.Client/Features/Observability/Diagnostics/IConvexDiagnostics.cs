using System.Text.Json;

namespace Convex.Client.Features.Observability.Diagnostics;

/// <summary>
/// Provides performance tracking and diagnostics for Convex client operations.
/// Thread-safe for concurrent performance measurement and disconnect tracking.
/// </summary>
public interface IConvexDiagnostics
{
    /// <summary>
    /// Gets the performance.
    /// </summary>
    IPerformanceTracker Performance { get; }

    /// <summary>
    /// Gets the disconnects.
    /// </summary>
    IDisconnectTracker Disconnects { get; }
}

/// <summary>
/// Tracks performance marks and measures similar to browser Performance API.
/// </summary>
public interface IPerformanceTracker
{
    /// <summary>
    /// Gets the entries.
    /// </summary>
    IReadOnlyList<PerformanceEntry> Entries { get; }

    /// <summary>
    /// Executes the mark operation.
    /// </summary>
    PerformanceMark Mark(string markName, JsonElement? detail = null);

    /// <summary>
    /// Executes the measure operation.
    /// </summary>
    PerformanceMeasure Measure(string measureName, string? startMark = null, string? endMark = null);

    /// <summary>
    /// Gets entries by name.
    /// </summary>
    IReadOnlyList<PerformanceEntry> GetEntriesByName(string name);

    /// <summary>
    /// Gets entries by type.
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
    /// Gets summary.
    /// </summary>
    PerformanceSummary GetSummary();
}

/// <summary>
/// Tracks disconnection events and provides statistics.
/// </summary>
public interface IDisconnectTracker
{
    /// <summary>
    /// Gets or sets a value indicating whether disconnected.
    /// </summary>
    bool IsDisconnected { get; }

    /// <summary>
    /// Gets the current disconnect duration.
    /// </summary>
    TimeSpan? CurrentDisconnectDuration { get; }

    /// <summary>
    /// Gets or sets a value indicating whether long disconnect.
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
    /// Gets stats.
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
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the entry type.
    /// </summary>
    public string EntryType { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public TimeSpan Timestamp { get; init; }
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Represents performance mark.
/// </summary>
public record PerformanceMark : PerformanceEntry
{
    /// <summary>
    /// Gets or sets the detail.
    /// </summary>
    public JsonElement? Detail { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMark"/> class.
    /// </summary>
    public PerformanceMark(string name, TimeSpan timestamp, JsonElement? detail = null)
    {
        Name = name;
        EntryType = "mark";
        Timestamp = timestamp;
        Duration = TimeSpan.Zero;
        Detail = detail;
    }

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() => $"Mark '{Name}' at {Timestamp.TotalMilliseconds:F2}ms";
}

/// <summary>
/// Represents performance measure.
/// </summary>
public record PerformanceMeasure : PerformanceEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMeasure"/> class.
    /// </summary>
    public PerformanceMeasure(string name, TimeSpan startTime, TimeSpan duration)
    {
        Name = name;
        EntryType = "measure";
        Timestamp = startTime;
        Duration = duration;
    }

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() => $"Measure '{Name}': {Duration.TotalMilliseconds:F2}ms (at {Timestamp.TotalMilliseconds:F2}ms)";
}

/// <summary>
/// Statistics for a specific measure name.
/// </summary>
public record PerformanceMeasureStats
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// Gets or sets the count.
    /// </summary>
    public int Count { get; init; }
    /// <summary>
    /// Gets or sets the total duration.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }
    /// <summary>
    /// Gets or sets the average duration.
    /// </summary>
    public TimeSpan AverageDuration { get; init; }
    /// <summary>
    /// Gets or sets the min duration.
    /// </summary>
    public TimeSpan MinDuration { get; init; }
    /// <summary>
    /// Gets or sets the max duration.
    /// </summary>
    public TimeSpan MaxDuration { get; init; }

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() =>
        $"{Name}: {Count} measurements, avg {AverageDuration.TotalMilliseconds:F2}ms, " +
        $"min {MinDuration.TotalMilliseconds:F2}ms, max {MaxDuration.TotalMilliseconds:F2}ms";
}

/// <summary>
/// Summary of all performance measurements.
/// </summary>
public record PerformanceSummary
{
    /// <summary>
    /// Gets or sets the total marks.
    /// </summary>
    public int TotalMarks { get; init; }
    /// <summary>
    /// Gets or sets the total measures.
    /// </summary>
    public int TotalMeasures { get; init; }
    /// <summary>
    /// Gets or sets the measure stats.
    /// </summary>
    public List<PerformanceMeasureStats> MeasureStats { get; init; } = [];

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() =>
        $"Performance Summary: {TotalMarks} marks, {TotalMeasures} measures, {MeasureStats.Count} unique operations";
}

/// <summary>
/// Represents disconnect event.
/// </summary>
public record DisconnectEvent
{
    /// <summary>
    /// Gets or sets the disconnected at.
    /// </summary>
    public DateTimeOffset DisconnectedAt { get; init; }
    /// <summary>
    /// Gets or sets the reconnected at.
    /// </summary>
    public DateTimeOffset ReconnectedAt { get; init; }
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
    /// <summary>
    /// Gets or sets a value indicating whether was long disconnect.
    /// </summary>
    public bool WasLongDisconnect { get; init; }

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() =>
        $"Disconnected for {Duration.TotalSeconds:F1}s {(WasLongDisconnect ? "(LONG)" : "")} " +
        $"from {DisconnectedAt:HH:mm:ss} to {ReconnectedAt:HH:mm:ss}";
}

/// <summary>
/// Statistics about disconnect events.
/// </summary>
public record DisconnectStats
{
    /// <summary>
    /// Gets or sets the total disconnects.
    /// </summary>
    public int TotalDisconnects { get; init; }
    /// <summary>
    /// Gets or sets the long disconnects.
    /// </summary>
    public int LongDisconnects { get; init; }
    /// <summary>
    /// Gets or sets the average disconnect duration.
    /// </summary>
    public TimeSpan AverageDisconnectDuration { get; init; }
    /// <summary>
    /// Gets or sets the longest disconnect.
    /// </summary>
    public TimeSpan LongestDisconnect { get; init; }
    /// <summary>
    /// Gets or sets the shortest disconnect.
    /// </summary>
    public TimeSpan ShortestDisconnect { get; init; }

    /// <summary>
    /// Gets the long disconnect rate.
    /// </summary>
    public double LongDisconnectRate => TotalDisconnects == 0 ? 0 : (LongDisconnects / (double)TotalDisconnects) * 100;

    /// <summary>
    /// Returns a string representation of the current instance.
    /// </summary>
    public override string ToString() =>
        $"Disconnects: {TotalDisconnects} total, {LongDisconnects} long ({LongDisconnectRate:F1}%), " +
        $"avg {AverageDisconnectDuration.TotalSeconds:F1}s, longest {LongestDisconnect.TotalSeconds:F1}s";
}
