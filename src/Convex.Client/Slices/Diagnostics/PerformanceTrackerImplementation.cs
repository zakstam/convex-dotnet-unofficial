using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Convex.Client.Slices.Diagnostics;

/// <summary>
/// Internal implementation of performance tracking.
/// Provides user-timing-like API similar to browser Performance API.
/// </summary>
internal sealed class PerformanceTrackerImplementation : IPerformanceTracker
{
    private readonly ConcurrentDictionary<string, PerformanceMark> _marks = new();
    private readonly ConcurrentDictionary<string, PerformanceMeasure> _measures = new();
    private readonly List<PerformanceEntry> _entries = [];
#pragma warning disable IDE0330 // Use 'System.Threading.Lock'
    private readonly object _entriesLock = new();
#pragma warning restore IDE0330 // Use 'System.Threading.Lock'
    private readonly Stopwatch _baseStopwatch = Stopwatch.StartNew();

    public IReadOnlyList<PerformanceEntry> Entries
    {
        get
        {
            lock (_entriesLock)
            {
                return [.. _entries];
            }
        }
    }

    public PerformanceMark Mark(string markName, JsonElement? detail = null)
    {
        var timestamp = _baseStopwatch.Elapsed;
        var mark = new PerformanceMark(markName, timestamp, detail);

        _marks[markName] = mark;

        lock (_entriesLock)
        {
            _entries.Add(mark);
        }

        return mark;
    }

    public PerformanceMeasure Measure(string measureName, string? startMark = null, string? endMark = null)
    {
        var endTime = endMark != null && _marks.TryGetValue(endMark, out var endMarkObj)
            ? endMarkObj.Timestamp
            : _baseStopwatch.Elapsed;

        var startTime = startMark != null && _marks.TryGetValue(startMark, out var startMarkObj)
            ? startMarkObj.Timestamp
            : TimeSpan.Zero;

        var duration = endTime - startTime;
        var measure = new PerformanceMeasure(measureName, startTime, duration);

        _measures[measureName] = measure;

        lock (_entriesLock)
        {
            _entries.Add(measure);
        }

        return measure;
    }

    public IReadOnlyList<PerformanceEntry> GetEntriesByName(string name)
    {
        lock (_entriesLock)
        {
            return [.. _entries.Where(e => e.Name == name)];
        }
    }

    public IReadOnlyList<PerformanceEntry> GetEntriesByType(string type)
    {
        lock (_entriesLock)
        {
            return [.. _entries.Where(e => e.EntryType == type)];
        }
    }

    public void ClearMarks()
    {
        _marks.Clear();
        lock (_entriesLock)
        {
            _ = _entries.RemoveAll(e => e.EntryType == "mark");
        }
    }

    public void ClearMeasures()
    {
        _measures.Clear();
        lock (_entriesLock)
        {
            _ = _entries.RemoveAll(e => e.EntryType == "measure");
        }
    }

    public void Clear()
    {
        _marks.Clear();
        _measures.Clear();
        lock (_entriesLock)
        {
            _entries.Clear();
        }
    }

    public PerformanceSummary GetSummary()
    {
        lock (_entriesLock)
        {
            var measures = _entries.OfType<PerformanceMeasure>().ToList();

            var grouped = measures
                .GroupBy(m => m.Name)
                .Select(g => new PerformanceMeasureStats
                {
                    Name = g.Key,
                    Count = g.Count(),
                    TotalDuration = TimeSpan.FromTicks(g.Sum(m => m.Duration.Ticks)),
                    AverageDuration = TimeSpan.FromTicks((long)g.Average(m => m.Duration.Ticks)),
                    MinDuration = TimeSpan.FromTicks(g.Min(m => m.Duration.Ticks)),
                    MaxDuration = TimeSpan.FromTicks(g.Max(m => m.Duration.Ticks))
                })
                .ToList();

            return new PerformanceSummary
            {
                TotalMarks = _entries.Count(e => e.EntryType == "mark"),
                TotalMeasures = measures.Count,
                MeasureStats = grouped
            };
        }
    }
}

/// <summary>
/// Helper for scoped performance measurement.
/// </summary>
public readonly struct ScopedPerformanceMeasure : IDisposable
{
    private readonly IPerformanceTracker _tracker;
    private readonly string _measureName;
    private readonly string _startMarkName;
    private readonly string _endMarkName;

    public ScopedPerformanceMeasure(IPerformanceTracker tracker, string measureName)
    {
        _tracker = tracker;
        _measureName = measureName;
        _startMarkName = $"{measureName}:start";
        _endMarkName = $"{measureName}:end";

        _ = _tracker.Mark(_startMarkName);
    }

    public void Dispose()
    {
        _ = _tracker.Mark(_endMarkName);
        _ = _tracker.Measure(_measureName, _startMarkName, _endMarkName);
    }
}

/// <summary>
/// Extension methods for IPerformanceTracker.
/// </summary>
public static class PerformanceTrackerExtensions
{
    /// <summary>
    /// Creates a scoped performance measure that automatically completes on disposal.
    /// </summary>
    /// <param name="tracker">The performance tracker.</param>
    /// <param name="measureName">The name of the measure.</param>
    /// <returns>A disposable measure that will automatically complete.</returns>
    public static ScopedPerformanceMeasure MeasureScoped(this IPerformanceTracker tracker, string measureName) => new(tracker, measureName);
}
