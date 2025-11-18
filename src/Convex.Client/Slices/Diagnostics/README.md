# Diagnostics Slice

## Purpose
Provides performance tracking and disconnection monitoring for Convex clients. Implements a browser Performance API-like interface for detailed timing measurements and tracks connection disruptions for diagnostic purposes. Enables observability and debugging of client performance.

## Responsibilities
- Performance mark tracking (timestamp points)
- Performance measure tracking (duration between marks)
- Scoped performance measurements with automatic completion
- Performance statistics aggregation (count, avg, min, max)
- Disconnection event tracking
- Long disconnection detection (>30s default)
- Disconnection statistics (total, long, avg, longest, shortest)
- Thread-safe metric recording

## Public API Surface

### Main Interface
```csharp
public interface IConvexDiagnostics
{
    IPerformanceTracker Performance { get; }
    IDisconnectTracker Disconnects { get; }
}
```

### Performance Tracker
```csharp
public interface IPerformanceTracker
{
    IReadOnlyList<PerformanceEntry> Entries { get; }

    PerformanceMark Mark(string markName, JsonElement? detail = null);
    PerformanceMeasure Measure(string measureName, string? startMark = null, string? endMark = null);

    IReadOnlyList<PerformanceEntry> GetEntriesByName(string name);
    IReadOnlyList<PerformanceEntry> GetEntriesByType(string type);

    void ClearMarks();
    void ClearMeasures();
    void Clear();

    PerformanceSummary GetSummary();
}
```

### Disconnect Tracker
```csharp
public interface IDisconnectTracker
{
    bool IsDisconnected { get; }
    TimeSpan? CurrentDisconnectDuration { get; }
    bool IsLongDisconnect { get; }
    IReadOnlyList<DisconnectEvent> DisconnectHistory { get; }

    void RecordDisconnect();
    void RecordReconnect();
    DisconnectStats GetStats();
    void Clear();
}
```

### Types
```csharp
public abstract record PerformanceEntry;
public record PerformanceMark : PerformanceEntry;
public record PerformanceMeasure : PerformanceEntry;
public record PerformanceMeasureStats;
public record PerformanceSummary;
public record DisconnectEvent;
public record DisconnectStats;
public struct ScopedPerformanceMeasure : IDisposable;
```

## Shared Dependencies
None - this is a pure state management slice with no external dependencies.

## Architecture
- **DiagnosticsSlice**: Public facade implementing IConvexDiagnostics
- **PerformanceTrackerImplementation**: Internal performance tracking with ConcurrentDictionary and Stopwatch
- **DisconnectTrackerImplementation**: Internal disconnection tracking with bounded history
- **ScopedPerformanceMeasure**: Disposable helper for automatic measurement completion

## Usage Examples

### Basic Performance Tracking
```csharp
var perf = client.DiagnosticsSlice.Performance;

// Mark start of operation
perf.Mark("query:start");

var result = await client.QueryAsync<MyData>("myQuery");

// Mark end of operation
perf.Mark("query:end");

// Measure duration
var measure = perf.Measure("query-duration", "query:start", "query:end");
Console.WriteLine($"Query took {measure.Duration.TotalMilliseconds}ms");
```

### Scoped Performance Measurement
```csharp
var perf = client.DiagnosticsSlice.Performance;

// Automatic measurement using IDisposable pattern
using (perf.MeasureScoped("mutation-operation"))
{
    await client.MutationAsync("myMutation", args);
}
// Automatically creates marks and measures on disposal

// Multiple operations
for (int i = 0; i < 10; i++)
{
    using (perf.MeasureScoped($"iteration-{i}"))
    {
        await ProcessItemAsync(i);
    }
}
```

### Performance Summary
```csharp
var perf = client.DiagnosticsSlice.Performance;

// Record multiple operations
for (int i = 0; i < 100; i++)
{
    using (perf.MeasureScoped("query"))
    {
        await client.QueryAsync<MyData>("myQuery");
    }
}

// Get aggregated statistics
var summary = perf.GetSummary();
Console.WriteLine(summary.ToString());
// Output: Performance Summary: 200 marks, 100 measures, 1 unique operations

foreach (var stats in summary.MeasureStats)
{
    Console.WriteLine(stats.ToString());
    // Output: query: 100 measurements, avg 45.23ms, min 12.50ms, max 123.45ms

    Console.WriteLine($"  Total: {stats.TotalDuration.TotalMilliseconds}ms");
    Console.WriteLine($"  Average: {stats.AverageDuration.TotalMilliseconds}ms");
    Console.WriteLine($"  Min: {stats.MinDuration.TotalMilliseconds}ms");
    Console.WriteLine($"  Max: {stats.MaxDuration.TotalMilliseconds}ms");
    Console.WriteLine($"  Count: {stats.Count}");
}
```

### Querying Performance Entries
```csharp
var perf = client.DiagnosticsSlice.Performance;

// Get all entries
var allEntries = perf.Entries;
Console.WriteLine($"Total entries: {allEntries.Count}");

// Get entries by name
var queryEntries = perf.GetEntriesByName("query");
Console.WriteLine($"Query entries: {queryEntries.Count}");

// Get entries by type
var marks = perf.GetEntriesByType("mark");
var measures = perf.GetEntriesByType("measure");
Console.WriteLine($"Marks: {marks.Count}, Measures: {measures.Count}");

// Display entry details
foreach (var entry in allEntries)
{
    Console.WriteLine($"{entry.EntryType}: {entry.Name}");
    Console.WriteLine($"  Timestamp: {entry.Timestamp.TotalMilliseconds}ms");
    Console.WriteLine($"  Duration: {entry.Duration.TotalMilliseconds}ms");

    if (entry is PerformanceMark mark && mark.Detail.HasValue)
    {
        Console.WriteLine($"  Detail: {mark.Detail.Value}");
    }
}
```

### Disconnection Tracking
```csharp
var disconnects = client.DiagnosticsSlice.Disconnects;

// Track disconnection events
client.ConnectionStateChanged += (sender, state) =>
{
    if (state == ConnectionState.Disconnected)
    {
        disconnects.RecordDisconnect();
    }
    else if (state == ConnectionState.Connected)
    {
        disconnects.RecordReconnect();
    }
};

// Check current disconnection status
if (disconnects.IsDisconnected)
{
    var duration = disconnects.CurrentDisconnectDuration;
    Console.WriteLine($"Currently disconnected for {duration?.TotalSeconds:F1}s");

    if (disconnects.IsLongDisconnect)
    {
        Console.WriteLine("WARNING: Long disconnection detected!");
    }
}
```

### Disconnection Statistics
```csharp
var disconnects = client.DiagnosticsSlice.Disconnects;

// Get disconnection stats
var stats = disconnects.GetStats();
Console.WriteLine(stats.ToString());
// Output: Disconnects: 5 total, 2 long (40.0%), avg 25.3s, longest 120.5s

Console.WriteLine($"Total Disconnects: {stats.TotalDisconnects}");
Console.WriteLine($"Long Disconnects: {stats.LongDisconnects} ({stats.LongDisconnectRate:F1}%)");
Console.WriteLine($"Average Duration: {stats.AverageDisconnectDuration.TotalSeconds:F1}s");
Console.WriteLine($"Longest: {stats.LongestDisconnect.TotalSeconds:F1}s");
Console.WriteLine($"Shortest: {stats.ShortestDisconnect.TotalSeconds:F1}s");

// View disconnection history
var history = disconnects.DisconnectHistory;
Console.WriteLine($"\nDisconnection History ({history.Count} events):");
foreach (var evt in history)
{
    Console.WriteLine(evt.ToString());
    // Output: Disconnected for 45.2s (LONG) from 14:32:15 to 14:33:00

    Console.WriteLine($"  Duration: {evt.Duration.TotalSeconds:F1}s");
    Console.WriteLine($"  Was Long: {evt.WasLongDisconnect}");
    Console.WriteLine($"  Disconnected: {evt.DisconnectedAt}");
    Console.WriteLine($"  Reconnected: {evt.ReconnectedAt}");
}
```

### Comprehensive Diagnostics Report
```csharp
// Generate comprehensive diagnostics report
var perf = client.DiagnosticsSlice.Performance;
var disconnects = client.DiagnosticsSlice.Disconnects;

Console.WriteLine("=== Performance Report ===");
var perfSummary = perf.GetSummary();
Console.WriteLine($"Marks: {perfSummary.TotalMarks}");
Console.WriteLine($"Measures: {perfSummary.TotalMeasures}");
Console.WriteLine("\nTop Operations:");
foreach (var stats in perfSummary.MeasureStats.OrderByDescending(s => s.AverageDuration).Take(5))
{
    Console.WriteLine($"  {stats.Name}: avg {stats.AverageDuration.TotalMilliseconds:F2}ms ({stats.Count} calls)");
}

Console.WriteLine("\n=== Disconnection Report ===");
var disconnectStats = disconnects.GetStats();
Console.WriteLine($"Total: {disconnectStats.TotalDisconnects}");
Console.WriteLine($"Long: {disconnectStats.LongDisconnects} ({disconnectStats.LongDisconnectRate:F1}%)");
Console.WriteLine($"Average: {disconnectStats.AverageDisconnectDuration.TotalSeconds:F1}s");

if (disconnects.IsDisconnected)
{
    Console.WriteLine($"\nCurrently Disconnected: {disconnects.CurrentDisconnectDuration?.TotalSeconds:F1}s");
}
```

### Clearing Diagnostics Data
```csharp
var perf = client.DiagnosticsSlice.Performance;
var disconnects = client.DiagnosticsSlice.Disconnects;

// Clear specific performance data
perf.ClearMarks();        // Clears all marks
perf.ClearMeasures();     // Clears all measures
perf.Clear();             // Clears everything

// Clear disconnection data
disconnects.Clear();      // Clears history and resets state

// Verify cleared
Console.WriteLine($"Marks: {perf.GetEntriesByType("mark").Count}");           // 0
Console.WriteLine($"Measures: {perf.GetEntriesByType("measure").Count}");     // 0
Console.WriteLine($"Disconnects: {disconnects.DisconnectHistory.Count}");     // 0
```

## Implementation Details
- Uses Stopwatch for high-precision timing (tick resolution)
- Uses ConcurrentDictionary for thread-safe mark/measure storage
- Uses List with lock for ordered entry storage
- Disconnection history bounded to 50 events (FIFO)
- Long disconnection threshold: 30 seconds (configurable in implementation)
- No HTTP calls - pure state management and timing

## Performance Entry Types

### PerformanceMark
- Represents a point in time
- EntryType: "mark"
- Duration: Always TimeSpan.Zero
- Optional Detail: JsonElement for additional data

### PerformanceMeasure
- Represents duration between two marks
- EntryType: "measure"
- Timestamp: Start time
- Duration: Time between start and end marks

## Thread Safety
All diagnostic operations are thread-safe:
- Performance marks/measures use ConcurrentDictionary (lock-free writes)
- Entry list uses lock for reads/writes
- Disconnection tracking uses lock for all state changes
- Summary generation creates snapshot with lock
- Multiple concurrent measurements are safely serialized

## Limitations
- No metric persistence (resets on application restart)
- Fixed disconnection history size (50 events)
- No configurable long disconnection threshold via API
- No automatic cleanup of old performance entries
- No performance entry size limits (unbounded growth)
- No integration with System.Diagnostics.Activity for distributed tracing

## Owner
TBD
