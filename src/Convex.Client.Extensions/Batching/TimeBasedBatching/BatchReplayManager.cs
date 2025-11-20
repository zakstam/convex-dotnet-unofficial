using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Manages replay of batches with time-relative timestamps for smooth playback on remote clients.
/// </summary>
/// <typeparam name="TEvent">The type of events to replay.</typeparam>
public class BatchReplayManager<TEvent> : IDisposable
{
    private readonly BatchingOptions _options;
    private readonly Subject<TEvent> _replayedEvents = new();
    private readonly Queue<Batch<TEvent>> _batchQueue = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _replayCancellation;
    private Task? _replayTask;
    private double _replaySpeedMultiplier = 1.0;
    private bool _isDisposed;
    private IDisposable? _batchSubscription;

    /// <summary>
    /// Initializes a new instance of the BatchReplayManager class.
    /// </summary>
    /// <param name="options">Batching configuration options (used for timing).</param>
    public BatchReplayManager(BatchingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets an observable stream of replayed events.
    /// Events are emitted at the correct timing based on their timeSinceBatchStart values.
    /// </summary>
    public IObservable<TEvent> ReplayedEvents => _replayedEvents.AsObservable();

    /// <summary>
    /// Gets or sets the replay speed multiplier.
    /// 1.0 = normal speed, 2.0 = 2x speed, 0.5 = half speed, etc.
    /// </summary>
    public double ReplaySpeedMultiplier
    {
        get
        {
            lock (_lock)
            {
                return _replaySpeedMultiplier;
            }
        }
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Replay speed multiplier must be greater than 0");

            lock (_lock)
            {
                _replaySpeedMultiplier = value;
            }
        }
    }

    /// <summary>
    /// Subscribes to batch updates and queues them for replay.
    /// </summary>
    /// <param name="batches">An observable stream of batches to replay.</param>
    public void SubscribeToBatches(IObservable<Batch<TEvent>> batches)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        ArgumentNullException.ThrowIfNull(batches);

        // Dispose existing subscription if any
        _batchSubscription?.Dispose();

        // Subscribe to batches and queue them
        _batchSubscription = batches.Subscribe(
            onNext: batch =>
            {
                lock (_lock)
                {
                    _batchQueue.Enqueue(batch);
                }

                // Start replay task if not already running
                if (_replayTask == null || _replayTask.IsCompleted)
                {
                    StartReplayTask();
                }
            },
            onError: error =>
            {
                _replayedEvents.OnError(error);
            },
            onCompleted: () =>
            {
                _replayedEvents.OnCompleted();
            });
    }

    /// <summary>
    /// Starts the replay task that processes queued batches.
    /// </summary>
    private void StartReplayTask()
    {
        _replayCancellation?.Cancel();
        _replayCancellation = new CancellationTokenSource();
        _replayTask = Task.Run(async () => await ProcessBatchesAsync(_replayCancellation.Token));
    }

    /// <summary>
    /// Processes batches from the queue and replays events at correct timing.
    /// </summary>
    private async Task ProcessBatchesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            Batch<TEvent>? batch = null;

            lock (_lock)
            {
                if (_batchQueue.Count > 0)
                {
                    batch = _batchQueue.Dequeue();
                }
            }

            if (batch == null)
            {
                // No batches to process, wait a bit and check again
                await Task.Delay(10, cancellationToken);
                continue;
            }

            await ReplayBatchAsync(batch, cancellationToken);
        }
    }

    /// <summary>
    /// Replays a single batch, emitting events at the correct timing.
    /// </summary>
    private async Task ReplayBatchAsync(Batch<TEvent> batch, CancellationToken cancellationToken)
    {
        if (batch.Events == null || batch.Events.Count == 0)
            return;

        // Sort events by timeSinceBatchStart to ensure correct order
        var sortedEvents = batch.Events.OrderBy(e => e.TimeSinceBatchStart).ToList();

        double lastTime = 0;

        foreach (var batchableEvent in sortedEvents)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var timeSinceBatchStart = batchableEvent.TimeSinceBatchStart;
            var delay = timeSinceBatchStart - lastTime;

            // Apply replay speed multiplier
            if (delay > 0)
            {
                var adjustedDelay = delay / _replaySpeedMultiplier;
                await Task.Delay((int)adjustedDelay, cancellationToken);
            }

            // Emit the event
            _replayedEvents.OnNext(batchableEvent.EventData);
            lastTime = timeSinceBatchStart;
        }
    }

    /// <summary>
    /// Clears all queued batches.
    /// </summary>
    public void ClearQueue()
    {
        lock (_lock)
        {
            _batchQueue.Clear();
        }
    }

    /// <summary>
    /// Gets the number of batches currently queued for replay.
    /// </summary>
    public int QueuedBatchCount
    {
        get
        {
            lock (_lock)
            {
                return _batchQueue.Count;
            }
        }
    }

    /// <summary>
    /// Disposes the replay manager and stops all replay operations.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _batchSubscription?.Dispose();
        _replayCancellation?.Cancel();

        try
        {
            _replayTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore timeout exceptions during disposal
        }

        _replayCancellation?.Dispose();
        _replayedEvents.Dispose();

        GC.SuppressFinalize(this);
    }
}

