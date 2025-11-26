namespace Convex.Client.Infrastructure.Results;

/// <summary>
/// Wrapper for query results that includes server metadata and diagnostics.
/// Provides access to server log lines, execution timing, and other observability data.
/// </summary>
/// <typeparam name="T">The type of the query result value.</typeparam>
/// <remarks>
/// Initializes a new instance of the QueryResult record.
/// </remarks>
public record QueryResult<T>(
    T Value,
    IReadOnlyList<string> LogLines,
    DateTimeOffset RequestTimestamp,
    DateTimeOffset ResponseTimestamp,
    string FunctionName,
    string RequestId,
    bool IsCached = false)
{
    /// <summary>
    /// Gets the total execution time including network round-trip.
    /// </summary>
    public TimeSpan ExecutionTime => ResponseTimestamp - RequestTimestamp;

    /// <summary>
    /// Implicit conversion to the underlying value type for convenience.
    /// </summary>
    public static implicit operator T(QueryResult<T> result) => result.Value;

    /// <summary>
    /// Returns a string representation of the query result with metadata.
    /// </summary>
    public override string ToString() => $"QueryResult: {FunctionName} ({ExecutionTime.TotalMilliseconds:F2}ms, {LogLines.Count} log lines)";
}

/// <summary>
/// Options for controlling query result metadata collection.
/// </summary>
public class QueryResultOptions
{
    /// <summary>
    /// Gets or sets whether to include server log lines in the result.
    /// Default is false for performance.
    /// </summary>
    public bool IncludeLogLines { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to track execution timing.
    /// Default is true.
    /// </summary>
    public bool TrackExecutionTime { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum execution time threshold for logging slow queries.
    /// Queries slower than this threshold will be logged as warnings.
    /// Default is null (no threshold).
    /// </summary>
    public TimeSpan? SlowQueryThreshold { get; set; }

    /// <summary>
    /// Default options with minimal overhead.
    /// </summary>
    public static QueryResultOptions Default => new();

    /// <summary>
    /// Options optimized for development and debugging.
    /// Includes log lines and tracks all metrics.
    /// </summary>
    public static QueryResultOptions Debug => new()
    {
        IncludeLogLines = true,
        TrackExecutionTime = true,
        SlowQueryThreshold = TimeSpan.FromSeconds(1)
    };

    /// <summary>
    /// Options optimized for production with minimal overhead.
    /// Only tracks execution time, no log lines.
    /// </summary>
    public static QueryResultOptions Production => new()
    {
        IncludeLogLines = false,
        TrackExecutionTime = true,
        SlowQueryThreshold = TimeSpan.FromSeconds(5)
    };
}

