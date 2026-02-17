
namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Interface for events that can be batched with time-relative timestamps.
/// </summary>
/// <typeparam name="TEvent">The type of the event data.</typeparam>
public interface IBatchableEvent<TEvent>
{
    /// <summary>
    /// Gets the time since batch start.
    /// This is calculated relative to the batch start time for accurate replay.
    /// </summary>
    double TimeSinceBatchStart { get; }

    /// <summary>
    /// Gets the event data.
    /// </summary>
    TEvent EventData { get; }
}

/// <summary>
/// Represents batch t event.
/// </summary>
/// <typeparam name="TEvent">The type of the events in the batch.</typeparam>
public class Batch<TEvent>
{
    /// <summary>
    /// Gets or sets the batch ID.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the events.
    /// </summary>
    public List<IBatchableEvent<TEvent>> Events { get; set; } = [];

    /// <summary>
    /// Gets or sets the batch start time.
    /// </summary>
    public double BatchStartTime { get; set; }

    /// <summary>
    /// Gets or sets the string.
    /// This is used to identify and update existing batches.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Configuration options for time-based batching behavior.
/// Use the fluent builder API via <see cref="Create"/> or preset configurations
/// via <see cref="ForDrawing"/>, <see cref="ForCursorTracking"/>, etc.
/// </summary>
public class BatchingOptions
{
    /// <summary>
    /// Gets or sets the sampling interval ms.
    /// Events that arrive more frequently than this interval will be skipped.
    /// Default: 10ms
    /// </summary>
    public int SamplingIntervalMs { get; set; } = 10;

    /// <summary>
    /// Gets or sets the batch interval ms.
    /// Default: 500ms
    /// </summary>
    public int BatchIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the max batch size.
    /// When this limit is reached, the batch is flushed immediately.
    /// Default: 200
    /// </summary>
    public int MaxBatchSize { get; set; } = 200;

    /// <summary>
    /// Gets or sets the min event distance.
    /// Only applies to events that implement spatial distance calculation (X/Y properties).
    /// Set to null to disable spatial filtering.
    /// Default: null (disabled)
    /// </summary>
    public double? MinEventDistance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enable sampling.
    /// When enabled, events are sampled at the SamplingIntervalMs rate.
    /// Default: true
    /// </summary>
    public bool EnableSampling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether reset batch start time on flush.
    /// When true, each flush starts a new batch with timeSinceBatchStart reset to 0 (standard batching).
    /// When false, maintains the same batch start time across flushes (cumulative timestamps).
    /// Use false when backend appends events to the same batch record.
    /// Default: true (standard batching behavior)
    /// </summary>
    public bool ResetBatchStartTimeOnFlush { get; set; } = true;

    /// <summary>
    /// Gets the create.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    /// <example>
    /// <code>
    /// var options = BatchingOptions.Create()
    ///     .WithSampling(intervalMs: 10)
    ///     .WithBatchInterval(500)
    ///     .WithMaxBatchSize(200)
    ///     .WithMinDistance(2.0)
    ///     .WithCumulativeTimestamps()
    ///     .Build();
    /// </code>
    /// </example>
    public static BatchingOptionsBuilder Create() => new();

    /// <summary>
    /// Gets the for drawing.
    /// - Sampling: 10ms (captures smooth strokes)
    /// - Batch interval: 500ms (good balance of latency/bandwidth)
    /// - Max batch size: 200 events
    /// - Min distance: 2.0 pixels (removes redundant points)
    /// - Cumulative timestamps: true (for stroke continuity)
    /// </summary>
    public static BatchingOptions ForDrawing() => new()
    {
        SamplingIntervalMs = 10,
        BatchIntervalMs = 500,
        MaxBatchSize = 200,
        MinEventDistance = 2.0,
        EnableSampling = true,
        ResetBatchStartTimeOnFlush = false
    };

    /// <summary>
    /// Gets the for cursor tracking.
    /// - Sampling: 16ms (~60fps)
    /// - Batch interval: 200ms (low latency for real-time feel)
    /// - Max batch size: 100 events
    /// - Min distance: 5.0 pixels (cursor movements are less precise)
    /// - Cumulative timestamps: false (independent position updates)
    /// </summary>
    public static BatchingOptions ForCursorTracking() => new()
    {
        SamplingIntervalMs = 16,
        BatchIntervalMs = 200,
        MaxBatchSize = 100,
        MinEventDistance = 5.0,
        EnableSampling = true,
        ResetBatchStartTimeOnFlush = true
    };

    /// <summary>
    /// Gets the for telemetry.
    /// - Sampling: disabled (capture all events)
    /// - Batch interval: 1000ms (low priority, minimize server load)
    /// - Max batch size: 500 events
    /// - Min distance: null (not applicable for non-spatial events)
    /// - Cumulative timestamps: false (independent events)
    /// </summary>
    public static BatchingOptions ForTelemetry() => new()
    {
        SamplingIntervalMs = 50,
        BatchIntervalMs = 1000,
        MaxBatchSize = 500,
        MinEventDistance = null,
        EnableSampling = false,
        ResetBatchStartTimeOnFlush = true
    };
}

/// <summary>
/// Fluent builder for BatchingOptions.
/// </summary>
public class BatchingOptionsBuilder
{
    private readonly BatchingOptions _options = new();

    /// <summary>
    /// Configures sampling.
    /// </summary>
    /// <param name="intervalMs">Interval in milliseconds (default: 10ms).</param>
    public BatchingOptionsBuilder WithSampling(int intervalMs = 10)
    {
        _options.SamplingIntervalMs = intervalMs;
        _options.EnableSampling = true;
        return this;
    }

    /// <summary>
    /// Disables event sampling (all events are captured).
    /// </summary>
    public BatchingOptionsBuilder WithoutSampling()
    {
        _options.EnableSampling = false;
        return this;
    }

    /// <summary>
    /// Configures batch interval.
    /// </summary>
    /// <param name="intervalMs">Interval in milliseconds (default: 500ms).</param>
    public BatchingOptionsBuilder WithBatchInterval(int intervalMs)
    {
        _options.BatchIntervalMs = intervalMs;
        return this;
    }

    /// <summary>
    /// Configures max batch size.
    /// </summary>
    /// <param name="maxSize">Maximum events per batch (default: 200).</param>
    public BatchingOptionsBuilder WithMaxBatchSize(int maxSize)
    {
        _options.MaxBatchSize = maxSize;
        return this;
    }

    /// <summary>
    /// Configures min distance.
    /// </summary>
    /// <param name="minDistance">Minimum distance in pixels/units.</param>
    public BatchingOptionsBuilder WithMinDistance(double minDistance)
    {
        _options.MinEventDistance = minDistance;
        return this;
    }

    /// <summary>
    /// Disables spatial distance filtering.
    /// </summary>
    public BatchingOptionsBuilder WithoutDistanceFiltering()
    {
        _options.MinEventDistance = null;
        return this;
    }

    /// <summary>
    /// Enables cumulative timestamps across flushes (for backends that append to same batch).
    /// Use this when your backend PATCH operation appends events to the same batch record.
    /// </summary>
    public BatchingOptionsBuilder WithCumulativeTimestamps()
    {
        _options.ResetBatchStartTimeOnFlush = false;
        return this;
    }

    /// <summary>
    /// Enables independent batches with reset timestamps (standard behavior).
    /// Use this when each flush creates a new batch record.
    /// </summary>
    public BatchingOptionsBuilder WithIndependentBatches()
    {
        _options.ResetBatchStartTimeOnFlush = true;
        return this;
    }

    /// <summary>
    /// Builds the final BatchingOptions instance.
    /// </summary>
    public BatchingOptions Build() => _options;
}

/// <summary>
/// Exception thrown when batch validation fails, including detailed property and type information.
/// </summary>
/// <param name="typeName">The name of the type being validated.</param>
/// <param name="propertyName">The name of the property that failed validation.</param>
/// <param name="expectedType">The expected type for the property.</param>
/// <param name="actualType">The actual type provided, or null if the value was null.</param>
/// <param name="details">Additional details about the validation failure.</param>
public sealed class BatchValidationException(
    string typeName,
    string propertyName,
    Type expectedType,
    Type? actualType,
    string details)
    : Exception(FormatMessage(typeName, propertyName, expectedType, actualType, details))
{
    /// <summary>
    /// Gets the type name.
    /// </summary>
    public string TypeName { get; } = typeName;

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the expected type.
    /// </summary>
    public Type ExpectedType { get; } = expectedType;

    /// <summary>
    /// Gets the actual type.
    /// </summary>
    public Type? ActualType { get; } = actualType;

    private static string FormatMessage(
        string typeName,
        string propertyName,
        Type expectedType,
        Type? actualType,
        string details)
    {
        var actualTypeName = actualType?.Name ?? "null";
        return $"Batch validation failed for {typeName}.{propertyName}: " +
               $"Expected {expectedType.Name}, got {actualTypeName}. {details}";
    }
}

