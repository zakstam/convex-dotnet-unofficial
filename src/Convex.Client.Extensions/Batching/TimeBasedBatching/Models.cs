using System.Collections.Generic;

namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Interface for events that can be batched with time-relative timestamps.
/// </summary>
/// <typeparam name="TEvent">The type of the event data.</typeparam>
public interface IBatchableEvent<TEvent>
{
    /// <summary>
    /// Gets the time since the batch started, in milliseconds.
    /// This is calculated relative to the batch start time for accurate replay.
    /// </summary>
    double TimeSinceBatchStart { get; }

    /// <summary>
    /// Gets the actual event data.
    /// </summary>
    TEvent EventData { get; }
}

/// <summary>
/// Represents a batch of events with time-relative timestamps.
/// </summary>
/// <typeparam name="TEvent">The type of the events in the batch.</typeparam>
public class Batch<TEvent>
{
    /// <summary>
    /// Gets or sets the unique identifier for the batch.
    /// </summary>
    public string? BatchId { get; set; }

    /// <summary>
    /// Gets or sets the list of events with their relative timestamps.
    /// </summary>
    public List<IBatchableEvent<TEvent>> Events { get; set; } = new();

    /// <summary>
    /// Gets or sets the absolute timestamp when the batch started (Unix milliseconds).
    /// </summary>
    public double BatchStartTime { get; set; }

    /// <summary>
    /// Gets or sets optional metadata for the batch (e.g., userId, sessionId, roomId).
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
    /// Gets or sets how often to sample events, in milliseconds.
    /// Events that arrive more frequently than this interval will be skipped.
    /// Default: 10ms
    /// </summary>
    public int SamplingIntervalMs { get; set; } = 10;

    /// <summary>
    /// Gets or sets how often to send batches to the server, in milliseconds.
    /// Default: 500ms
    /// </summary>
    public int BatchIntervalMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum number of events per batch.
    /// When this limit is reached, the batch is flushed immediately.
    /// Default: 200
    /// </summary>
    public int MaxBatchSize { get; set; } = 200;

    /// <summary>
    /// Gets or sets the minimum distance between events for spatial filtering.
    /// Only applies to events that implement spatial distance calculation (X/Y properties).
    /// Set to null to disable spatial filtering.
    /// Default: null (disabled)
    /// </summary>
    public double? MinEventDistance { get; set; }

    /// <summary>
    /// Gets or sets whether to enable sampling.
    /// When enabled, events are sampled at the SamplingIntervalMs rate.
    /// Default: true
    /// </summary>
    public bool EnableSampling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to reset the batch start time after each flush.
    /// When true, each flush starts a new batch with timeSinceBatchStart reset to 0 (standard batching).
    /// When false, maintains the same batch start time across flushes (cumulative timestamps).
    /// Use false when backend appends events to the same batch record.
    /// Default: true (standard batching behavior)
    /// </summary>
    public bool ResetBatchStartTimeOnFlush { get; set; } = true;

    /// <summary>
    /// Creates a new fluent builder for BatchingOptions.
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
    public static BatchingOptionsBuilder Create() => new BatchingOptionsBuilder();

    /// <summary>
    /// Creates preset options optimized for drawing applications.
    /// - Sampling: 10ms (captures smooth strokes)
    /// - Batch interval: 500ms (good balance of latency/bandwidth)
    /// - Max batch size: 200 events
    /// - Min distance: 2.0 pixels (removes redundant points)
    /// - Cumulative timestamps: true (for stroke continuity)
    /// </summary>
    public static BatchingOptions ForDrawing() => new BatchingOptions
    {
        SamplingIntervalMs = 10,
        BatchIntervalMs = 500,
        MaxBatchSize = 200,
        MinEventDistance = 2.0,
        EnableSampling = true,
        ResetBatchStartTimeOnFlush = false
    };

    /// <summary>
    /// Creates preset options optimized for cursor/mouse tracking.
    /// - Sampling: 16ms (~60fps)
    /// - Batch interval: 200ms (low latency for real-time feel)
    /// - Max batch size: 100 events
    /// - Min distance: 5.0 pixels (cursor movements are less precise)
    /// - Cumulative timestamps: false (independent position updates)
    /// </summary>
    public static BatchingOptions ForCursorTracking() => new BatchingOptions
    {
        SamplingIntervalMs = 16,
        BatchIntervalMs = 200,
        MaxBatchSize = 100,
        MinEventDistance = 5.0,
        EnableSampling = true,
        ResetBatchStartTimeOnFlush = true
    };

    /// <summary>
    /// Creates preset options optimized for telemetry/analytics events.
    /// - Sampling: disabled (capture all events)
    /// - Batch interval: 1000ms (low priority, minimize server load)
    /// - Max batch size: 500 events
    /// - Min distance: null (not applicable for non-spatial events)
    /// - Cumulative timestamps: false (independent events)
    /// </summary>
    public static BatchingOptions ForTelemetry() => new BatchingOptions
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
    /// Sets the sampling interval in milliseconds.
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
    /// Sets the batch flush interval in milliseconds.
    /// </summary>
    /// <param name="intervalMs">Interval in milliseconds (default: 500ms).</param>
    public BatchingOptionsBuilder WithBatchInterval(int intervalMs)
    {
        _options.BatchIntervalMs = intervalMs;
        return this;
    }

    /// <summary>
    /// Sets the maximum batch size before auto-flushing.
    /// </summary>
    /// <param name="maxSize">Maximum events per batch (default: 200).</param>
    public BatchingOptionsBuilder WithMaxBatchSize(int maxSize)
    {
        _options.MaxBatchSize = maxSize;
        return this;
    }

    /// <summary>
    /// Sets the minimum distance between spatial events (requires X/Y properties).
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

