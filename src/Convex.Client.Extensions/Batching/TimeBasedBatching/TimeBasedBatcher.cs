using System.Globalization;
using System.Reflection;

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
    private readonly List<IBatchableEvent<TEvent>> _currentBatch = [];

    private DateTime _batchStartTime;
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
    /// <exception cref="ObjectDisposedException">Thrown when the batcher has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown when createBatchableEvent is null.</exception>
    /// <exception cref="BatchValidationException">Thrown when the created batchable event fails validation.</exception>
    public void AddEvent(TEvent eventData, Func<TEvent, double, IBatchableEvent<TEvent>> createBatchableEvent)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ArgumentNullException.ThrowIfNull(createBatchableEvent);

        var shouldFlush = false;
        var shouldSkip = false;

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
            if (!shouldSkip &&
                _options.MinEventDistance.HasValue &&
                !EqualityComparer<TEvent>.Default.Equals(_lastEvent, default) &&
                TryCalculateDistance(_lastEvent!, eventData, out var distance) &&
                distance < _options.MinEventDistance.Value)
            {
                shouldSkip = true;
            }

            if (shouldSkip)
            {
                return;
            }

            // Calculate time since batch start
            var timeSinceBatchStart = (now - _batchStartTime).TotalMilliseconds;

            // Create batchable event with relative timestamp
            var batchableEvent = createBatchableEvent(eventData, timeSinceBatchStart);
            ValidateBatchableEvent(batchableEvent);
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
            {
                return;
            }

            batch = [.. _currentBatch];
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
                _ = await _client.Mutate<object>(_storeMutation)
                    .WithArgs(batchData)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

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
            Console.WriteLine($"Batch flush error: {ex.Message}");
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
    /// <param name="event1">The first event.</param>
    /// <param name="event2">The second event.</param>
    /// <param name="distance">The calculated distance between events.</param>
    /// <returns>True if distance calculation succeeded; otherwise, false.</returns>
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
                var x1 = ConvertToDouble(xProp, event1);
                var y1 = ConvertToDouble(yProp, event1);
                var x2 = ConvertToDouble(xProp, event2);
                var y2 = ConvertToDouble(yProp, event2);

                var dx = x2 - x1;
                var dy = y2 - y1;
                distance = Math.Sqrt((dx * dx) + (dy * dy));
                return true;
            }
            catch (BatchValidationException)
            {
                // Validation error - property exists but has incompatible type
                // Let it propagate for better error reporting in debug scenarios
                return false;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a property value to double with detailed error reporting.
    /// </summary>
    /// <param name="property">The property to read from.</param>
    /// <param name="eventData">The event data containing the property value.</param>
    /// <returns>The property value as a double.</returns>
    /// <exception cref="BatchValidationException">Thrown when the property value cannot be converted to double.</exception>
    private static double ConvertToDouble(PropertyInfo property, TEvent eventData)
    {
        var value = property.GetValue(eventData) ?? throw new BatchValidationException(
            typeof(TEvent).Name,
            property.Name,
            property.PropertyType,
            null,
            "Property value is null but double was expected");

        try
        {
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new BatchValidationException(
                typeof(TEvent).Name,
                property.Name,
                property.PropertyType,
                value.GetType(),
                $"Cannot convert value '{value}' to double: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a batchable event before adding to the batch.
    /// </summary>
    /// <param name="batchableEvent">The batchable event to validate.</param>
    /// <exception cref="BatchValidationException">Thrown when validation fails.</exception>
    private static void ValidateBatchableEvent(IBatchableEvent<TEvent> batchableEvent)
    {
        if (batchableEvent == null)
        {
            throw new BatchValidationException(
                typeof(IBatchableEvent<TEvent>).Name,
                "result",
                typeof(IBatchableEvent<TEvent>),
                null,
                "The createBatchableEvent factory returned null");
        }

        if (batchableEvent.EventData == null)
        {
            throw new BatchValidationException(
                typeof(IBatchableEvent<TEvent>).Name,
                nameof(IBatchableEvent<TEvent>.EventData),
                typeof(TEvent),
                null,
                "EventData property is null");
        }

        if (double.IsNaN(batchableEvent.TimeSinceBatchStart) || double.IsInfinity(batchableEvent.TimeSinceBatchStart))
        {
            throw new BatchValidationException(
                typeof(IBatchableEvent<TEvent>).Name,
                nameof(IBatchableEvent<TEvent>.TimeSinceBatchStart),
                typeof(double),
                batchableEvent.TimeSinceBatchStart.GetType(),
                $"TimeSinceBatchStart has invalid value: {batchableEvent.TimeSinceBatchStart}");
        }

        if (batchableEvent.TimeSinceBatchStart < 0)
        {
            throw new BatchValidationException(
                typeof(IBatchableEvent<TEvent>).Name,
                nameof(IBatchableEvent<TEvent>.TimeSinceBatchStart),
                typeof(double),
                batchableEvent.TimeSinceBatchStart.GetType(),
                $"TimeSinceBatchStart cannot be negative: {batchableEvent.TimeSinceBatchStart}");
        }
    }

    /// <summary>
    /// Disposes the batcher and flushes any remaining events.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _flushTimer?.Dispose();

        // Flush remaining events synchronously on dispose
        FlushAsync().GetAwaiter().GetResult();

        GC.SuppressFinalize(this);
    }
}
