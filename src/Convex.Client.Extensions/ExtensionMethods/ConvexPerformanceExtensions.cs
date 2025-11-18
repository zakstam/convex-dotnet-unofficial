using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for performance optimization patterns commonly used with Convex clients.
/// These methods help reduce unnecessary operations, batch updates, and provide performance monitoring.
/// </summary>
public static class ConvexPerformanceExtensions
{
    #region Distinct Value Filtering

    /// <summary>
    /// Filters out consecutive duplicate values based on a custom key selector.
    /// Unlike standard DistinctUntilChanged, this allows comparing objects by specific properties.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <typeparam name="TKey">The type of the key used for comparison.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="keySelector">Function to extract the comparison key from each value.</param>
    /// <returns>An observable that emits only when the key changes.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;User&gt;("users:current")
    ///     .DistinctUntilChangedBy(u => u.LastModified)
    ///     .Subscribe(user => UpdateUserUI(user));
    /// // Only updates UI when the user's last modified timestamp changes
    /// </code>
    /// </example>
    public static IObservable<T> DistinctUntilChangedBy<T, TKey>(
        this IObservable<T> source,
        Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source.DistinctUntilChanged(value => keySelector(value));
    }

    /// <summary>
    /// Filters out consecutive duplicate values based on multiple key selectors.
    /// Useful when you want to consider multiple properties for change detection.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="keySelectors">Functions to extract comparison keys from each value.</param>
    /// <returns>An observable that emits only when any of the keys change.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message&gt;("messages:latest")
    ///     .DistinctUntilChangedBy(
    ///         m => m.Id,
    ///         m => m.Content,
    ///         m => m.AuthorId)
    ///     .Subscribe(message => UpdateMessageUI(message));
    /// // Updates UI only when ID, content, or author actually changes
    /// </code>
    /// </example>
    public static IObservable<T> DistinctUntilChangedBy<T>(
        this IObservable<T> source,
        params Func<T, object>[] keySelectors)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelectors);

        return source.DistinctUntilChanged(value =>
        {
            var keys = new object[keySelectors.Length];
            for (var i = 0; i < keySelectors.Length; i++)
            {
                keys[i] = keySelectors[i](value);
            }
            return new CompositeKey(keys);
        });
    }

    #endregion

    #region Throttling and Rate Limiting

    /// <summary>
    /// Throttles emissions to a maximum frequency, ensuring no more than one emission per time window.
    /// Unlike Throttle, this guarantees the maximum rate rather than minimum delay.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="maxFrequency">Maximum frequency of emissions.</param>
    /// <returns>An observable throttled to the maximum frequency.</returns>
    /// <example>
    /// <code>
    /// var subscription = searchTextChanges
    ///     .ThrottleToMaxFrequency(TimeSpan.FromMilliseconds(200))
    ///     .SelectMany(term => client.Query&lt;Result[]&gt;("search", new { term }))
    ///     .Subscribe(results => UpdateSearchResults(results));
    /// // Ensures search API is called at most every 200ms
    /// </code>
    /// </example>
    public static IObservable<T> ThrottleToMaxFrequency<T>(
        this IObservable<T> source,
        TimeSpan maxFrequency)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxFrequency, TimeSpan.Zero);

        return Observable.Create<T>(observer =>
        {
            var lastEmission = DateTime.MinValue;
            var gate = new object();

            return source.Subscribe(
                onNext: value =>
                {
                    var now = DateTime.UtcNow;
                    lock (gate)
                    {
                        if (now - lastEmission >= maxFrequency)
                        {
                            lastEmission = now;
                            observer.OnNext(value);
                        }
                    }
                },
                onError: observer.OnError,
                onCompleted: observer.OnCompleted);
        });
    }

    /// <summary>
    /// Throttles emissions with a sliding window, allowing bursts but maintaining average rate.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="maxEmissions">Maximum number of emissions allowed in the time window.</param>
    /// <param name="timeWindow">The sliding time window.</param>
    /// <returns>An observable throttled with a sliding window.</returns>
    /// <example>
    /// <code>
    /// var subscription = userActions
    ///     .ThrottleSlidingWindow(maxEmissions: 5, timeWindow: TimeSpan.FromSeconds(10))
    ///     .Subscribe(action => ProcessUserAction(action));
    /// // Allows up to 5 actions per 10-second sliding window
    /// </code>
    /// </example>
    public static IObservable<T> ThrottleSlidingWindow<T>(
        this IObservable<T> source,
        int maxEmissions,
        TimeSpan timeWindow)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxEmissions);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeWindow, TimeSpan.Zero);

        return Observable.Create<T>(observer =>
        {
            var emissions = new Queue<DateTime>();
            var gate = new object();

            return source.Subscribe(
                onNext: value =>
                {
                    var now = DateTime.UtcNow;
                    lock (gate)
                    {
                        // Remove old emissions outside the window
                        while (emissions.Count > 0 && now - emissions.Peek() > timeWindow)
                        {
                            _ = emissions.Dequeue();
                        }

                        // Check if we can emit
                        if (emissions.Count < maxEmissions)
                        {
                            emissions.Enqueue(now);
                            observer.OnNext(value);
                        }
                        // Silently drop emissions that exceed the rate limit
                    }
                },
                onError: observer.OnError,
                onCompleted: observer.OnCompleted);
        });
    }

    #endregion

    #region Batching

    /// <summary>
    /// Batches multiple updates into a single emission based on time windows.
    /// Useful for reducing UI updates or network calls when many changes happen rapidly.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="batchWindow">Time window for collecting items into batches.</param>
    /// <param name="maxBatchSize">Maximum number of items per batch (default: no limit).</param>
    /// <returns>An observable that emits batches of items.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message&gt;("messages:stream")
    ///     .BatchUpdates(TimeSpan.FromMilliseconds(100), maxBatchSize: 10)
    ///     .Subscribe(batch => ProcessMessageBatch(batch));
    /// // Batches rapid message updates for efficient processing
    /// </code>
    /// </example>
    public static IObservable<IList<T>> BatchUpdates<T>(
        this IObservable<T> source,
        TimeSpan batchWindow,
        int? maxBatchSize = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchWindow, TimeSpan.Zero);
        if (maxBatchSize.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBatchSize.Value);
        }

        return Observable.Create<IList<T>>(observer =>
        {
            var batch = new List<T>();
            var timerDisposable = Disposable.Empty;
            var gate = new object();

            void EmitBatch()
            {
                lock (gate)
                {
                    if (batch.Count > 0)
                    {
                        observer.OnNext([.. batch]);
                        batch.Clear();
                    }
                    timerDisposable.Dispose();
                    timerDisposable = Disposable.Empty;
                }
            }

            return source.Subscribe(
                onNext: value =>
                {
                    lock (gate)
                    {
                        batch.Add(value);

                        // Start timer if not already running
                        if (timerDisposable == Disposable.Empty)
                        {
                            timerDisposable = Observable.Timer(batchWindow).Subscribe(_ => EmitBatch());
                        }

                        // Emit immediately if batch size limit reached
                        if (maxBatchSize.HasValue && batch.Count >= maxBatchSize.Value)
                        {
                            EmitBatch();
                        }
                    }
                },
                onError: error =>
                {
                    lock (gate)
                    {
                        timerDisposable.Dispose();
                        if (batch.Count > 0)
                        {
                            observer.OnNext(batch);
                        }
                        observer.OnError(error);
                    }
                },
                onCompleted: () =>
                {
                    lock (gate)
                    {
                        timerDisposable.Dispose();
                        if (batch.Count > 0)
                        {
                            observer.OnNext(batch);
                        }
                        observer.OnCompleted();
                    }
                });
        });
    }

    /// <summary>
    /// Batches updates based on a custom trigger condition.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="batchTrigger">Function that determines when to emit the current batch.</param>
    /// <returns>An observable that emits batches based on the trigger condition.</returns>
    /// <example>
    /// <code>
    /// var subscription = dataUpdates
    ///     .BatchOnTrigger(batch => batch.Count >= 10 || batch.Any(item => item.IsUrgent))
    ///     .Subscribe(batch => ProcessBatch(batch));
    /// // Emits batch when it reaches 10 items or contains an urgent item
    /// </code>
    /// </example>
    public static IObservable<IList<T>> BatchOnTrigger<T>(
        this IObservable<T> source,
        Func<IList<T>, bool> batchTrigger)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(batchTrigger);

        return Observable.Create<IList<T>>(observer =>
        {
            var batch = new List<T>();
            var gate = new object();

            return source.Subscribe(
                onNext: value =>
                {
                    lock (gate)
                    {
                        batch.Add(value);
                        if (batchTrigger(batch))
                        {
                            observer.OnNext([.. batch]);
                            batch.Clear();
                        }
                    }
                },
                onError: error =>
                {
                    lock (gate)
                    {
                        if (batch.Count > 0)
                        {
                            observer.OnNext(batch);
                        }
                        observer.OnError(error);
                    }
                },
                onCompleted: () =>
                {
                    lock (gate)
                    {
                        if (batch.Count > 0)
                        {
                            observer.OnNext(batch);
                        }
                        observer.OnCompleted();
                    }
                });
        });
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Adds performance logging to an observable sequence, measuring emission frequency and latency.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="operationName">Name of the operation for logging purposes.</param>
    /// <param name="logInterval">How often to log performance statistics (default: 30 seconds).</param>
    /// <returns>The same observable with performance monitoring side effects.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Update&gt;("updates:stream")
    ///     .WithPerformanceLogging("UpdateStream", logInterval: TimeSpan.FromMinutes(1))
    ///     .Subscribe(update => ProcessUpdate(update));
    /// // Logs performance metrics every minute
    /// </code>
    /// </example>
    public static IObservable<T> WithPerformanceLogging<T>(
        this IObservable<T> source,
        string operationName,
        TimeSpan? logInterval = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(operationName);

        var interval = logInterval ?? TimeSpan.FromSeconds(30);

        return Observable.Create<T>(observer =>
        {
            var emissionCount = 0;
            var startTime = DateTime.UtcNow;
            var lastLogTime = startTime;
            var gate = new object();

            var loggingSubscription = Observable.Interval(interval).Subscribe(_ =>
            {
                lock (gate)
                {
                    var now = DateTime.UtcNow;
                    var timeSinceLastLog = now - lastLogTime;
                    var totalTime = now - startTime;

                    if (emissionCount > 0)
                    {
                        var emissionsPerSecond = emissionCount / timeSinceLastLog.TotalSeconds;
                        var totalEmissionsPerSecond = emissionCount / totalTime.TotalSeconds;

                        Console.WriteLine($"[{operationName}] Performance: {emissionsPerSecond:F2} emissions/sec (last {timeSinceLastLog.TotalSeconds:F1}s), {totalEmissionsPerSecond:F2} emissions/sec total");

                        emissionCount = 0;
                        lastLogTime = now;
                    }
                }
            });

            return new CompositeDisposable(
                loggingSubscription,
                source.Subscribe(
                    onNext: value =>
                    {
                        lock (gate)
                        {
                            emissionCount++;
                        }
                        observer.OnNext(value);
                    },
                    onError: observer.OnError,
                    onCompleted: observer.OnCompleted));
        });
    }

    /// <summary>
    /// Measures and logs the latency of each emission.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="operationName">Name of the operation for logging purposes.</param>
    /// <param name="logThreshold">Only log latencies above this threshold (default: 100ms).</param>
    /// <returns>The same observable with latency monitoring side effects.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Query&lt;Data&gt;("data:fetch", parameters)
    ///     .WithLatencyLogging("DataFetch", logThreshold: TimeSpan.FromMilliseconds(500))
    ///     .Subscribe(data => ProcessData(data));
    /// // Logs query latencies over 500ms
    /// </code>
    /// </example>
    public static IObservable<T> WithLatencyLogging<T>(
        this IObservable<T> source,
        string operationName,
        TimeSpan? logThreshold = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(operationName);

        var threshold = logThreshold ?? TimeSpan.FromMilliseconds(100);

        return Observable.Create<T>(observer =>
        {
            var stopwatch = new Stopwatch();

            return source.Subscribe(
                onNext: value =>
                {
                    stopwatch.Stop();
                    var latency = stopwatch.Elapsed;

                    if (latency > threshold)
                    {
                        Console.WriteLine($"[{operationName}] High latency: {latency.TotalMilliseconds:F2}ms");
                    }

                    observer.OnNext(value);
                    stopwatch.Restart();
                },
                onError: error =>
                {
                    stopwatch.Stop();
                    observer.OnError(error);
                },
                onCompleted: () =>
                {
                    stopwatch.Stop();
                    observer.OnCompleted();
                });
        });
    }

    #endregion

    #region Helper Classes

    private class CompositeKey(object[] keys) : IEquatable<CompositeKey>
    {
        private readonly object[] _keys = keys;

        public bool Equals(CompositeKey? other)
        {
            if (other is null)
            {
                return false;
            }

            if (_keys.Length != other._keys.Length)
            {
                return false;
            }

            for (var i = 0; i < _keys.Length; i++)
            {
                if (!Equals(_keys[i], other._keys[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as CompositeKey);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var key in _keys)
            {
                hash.Add(key);
            }
            return hash.ToHashCode();
        }
    }

    #endregion
}
