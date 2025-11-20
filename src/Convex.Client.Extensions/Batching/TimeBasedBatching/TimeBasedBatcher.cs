using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client;

namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Generic batcher for high-frequency events with time-relative timestamps.
/// Implements sampling, spatial filtering, and periodic batching for efficient event processing.
/// </summary>
/// <typeparam name="TEvent">The type of events to batch.</typeparam>
public class TimeBasedBatcher<TEvent> : IDisposable
{
    private readonly IConvexClient _client;
    private readonly BatchingOptions _options;
    private readonly string _storeMutation;
    private readonly Timer _flushTimer;
    private readonly object _lock = new();
    
    private DateTime _batchStartTime;
    private List<IBatchableEvent<TEvent>> _currentBatch = new();
    private Dictionary<string, object>? _metadata;
    private DateTime _lastSampledTime;
    private TEvent? _lastEvent;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the TimeBasedBatcher class.
    /// </summary>
    /// <param name="client">The Convex client for sending batches.</param>
    /// <param name="options">Batching configuration options.</param>
    /// <param name="storeMutation">The name of the Convex mutation function to store batches.</param>
    /// <param name="metadata">Optional metadata for identifying batches (e.g., userId, sessionId).</param>
    public TimeBasedBatcher(
        IConvexClient client,
        BatchingOptions options,
        string storeMutation,
        Dictionary<string, object>? metadata = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _storeMutation = storeMutation ?? throw new ArgumentNullException(nameof(storeMutation));
        _metadata = metadata;
        
        _batchStartTime = DateTime.UtcNow;
        _lastSampledTime = DateTime.UtcNow;
        
        // Start periodic flush timer
        _flushTimer = new Timer(
            async _ => await FlushAsync(),
            null,
            _options.BatchIntervalMs,
            _options.BatchIntervalMs);
    }

    /// <summary>
    /// Adds an event to the current batch with time-relative timestamp calculation.
    /// </summary>
    /// <param name="eventData">The event data to add.</param>
    /// <param name="createBatchableEvent">Function to create a batchable event from the event data and timeSinceBatchStart.</param>
    public void AddEvent(TEvent eventData, Func<TEvent, double, IBatchableEvent<TEvent>> createBatchableEvent)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ArgumentNullException.ThrowIfNull(createBatchableEvent);

        bool shouldFlush = false;
        bool shouldSkip = false;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            
            // Sampling: skip events that arrive too frequently
            if (_options.EnableSampling)
            {
                var timeSinceLastSample = (now - _lastSampledTime).TotalMilliseconds;
                if (timeSinceLastSample < _options.SamplingIntervalMs)
                {
                    shouldSkip = true;
                }
                else
                {
                    _lastSampledTime = now;
                }
            }

            // Spatial filtering: skip events that are too close to the last event
            if (!shouldSkip && _options.MinEventDistance.HasValue && _lastEvent != null)
            {
                if (TryCalculateDistance(_lastEvent, eventData, out var distance))
                {
                    if (distance < _options.MinEventDistance.Value)
                    {
                        shouldSkip = true;
                    }
                }
            }

            if (shouldSkip)
            {
                return;
            }

            // Calculate time since batch start
            var timeSinceBatchStart = (now - _batchStartTime).TotalMilliseconds;
            
            // Create batchable event with relative timestamp
            var batchableEvent = createBatchableEvent(eventData, timeSinceBatchStart);
            _currentBatch.Add(batchableEvent);
            _lastEvent = eventData;

            // Adaptive batching: flush immediately if batch size limit reached
            if (_currentBatch.Count >= _options.MaxBatchSize)
            {
                shouldFlush = true;
            }
        }

        // Flush outside the lock to prevent deadlock
        if (shouldFlush)
        {
            _ = FlushAsync();
        }
    }

    /// <summary>
    /// Sets or updates the metadata for batches.
    /// This metadata is used to identify and update existing batches.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    public void SetMetadata(Dictionary<string, object> metadata)
    {
        lock (_lock)
        {
            _metadata = metadata;
        }
    }

    /// <summary>
    /// Manually flushes the current batch to the server.
    /// </summary>
    public async Task FlushAsync()
    {
        List<IBatchableEvent<TEvent>> batch;
        Dictionary<string, object>? metadata;
        double batchStartTime;

        lock (_lock)
        {
            if (_currentBatch.Count == 0)
                return;

            batch = _currentBatch.ToList();
            _currentBatch.Clear();
            metadata = _metadata;
            batchStartTime = (_batchStartTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        try
        {
            if (batch.Count > 0)
            {
                var batchData = new Batch<TEvent>
                {
                    Events = batch,
                    BatchStartTime = batchStartTime,
                    Metadata = metadata
                };

                // Send batch via mutation
                await _client.Mutate<object>(_storeMutation)
                    .WithArgs(batchData)
                    .ExecuteAsync();

                // Reset batch start time for next batch (if configured to do so)
                lock (_lock)
                {
                    if (_options.ResetBatchStartTimeOnFlush)
                    {
                        _batchStartTime = DateTime.UtcNow;
                    }
                    _lastEvent = default;
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - allow batching to continue
            // In a production scenario, you might want to raise an event or use a logger
            System.Diagnostics.Debug.WriteLine($"Batch flush error: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets the current batch and starts a new one.
    /// </summary>
    public void ResetBatch()
    {
        lock (_lock)
        {
            _currentBatch.Clear();
            _batchStartTime = DateTime.UtcNow;
            _lastSampledTime = DateTime.UtcNow;
            _lastEvent = default;
        }
    }

    /// <summary>
    /// Gets the current number of events in the batch.
    /// </summary>
    public int CurrentBatchSize
    {
        get
        {
            lock (_lock)
            {
                return _currentBatch.Count;
            }
        }
    }

    /// <summary>
    /// Attempts to calculate the distance between two events for spatial filtering.
    /// Returns false if the event type doesn't support distance calculation.
    /// </summary>
    private static bool TryCalculateDistance(TEvent event1, TEvent event2, out double distance)
    {
        distance = 0;

        // Try to use reflection to find X and Y properties (common for spatial events)
        var type = typeof(TEvent);
        var xProp = type.GetProperty("X");
        var yProp = type.GetProperty("Y");

        if (xProp != null && yProp != null)
        {
            try
            {
                var x1 = Convert.ToDouble(xProp.GetValue(event1));
                var y1 = Convert.ToDouble(yProp.GetValue(event1));
                var x2 = Convert.ToDouble(xProp.GetValue(event2));
                var y2 = Convert.ToDouble(yProp.GetValue(event2));

                var dx = x2 - x1;
                var dy = y2 - y1;
                distance = Math.Sqrt(dx * dx + dy * dy);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Disposes the batcher and flushes any remaining events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _flushTimer?.Dispose();
        
        // Flush remaining events synchronously on dispose
        FlushAsync().GetAwaiter().GetResult();
        
        GC.SuppressFinalize(this);
    }
}

