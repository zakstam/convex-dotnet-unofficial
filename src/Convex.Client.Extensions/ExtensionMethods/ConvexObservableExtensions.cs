using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Reactive;
using Convex.Client.Shared.Quality;
using Convex.Client.Shared.Connection;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for IObservable&lt;T&gt; that provide common reactive patterns
/// specifically designed for Convex client usage.
/// </summary>
public static class ConvexObservableExtensions
{
    #region Retry and Error Handling

    /// <summary>
    /// Automatically retries the observable on errors with exponential backoff.
    /// Useful for handling transient network failures in Convex subscriptions.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="initialDelay">Initial delay before first retry (default: 1 second).</param>
    /// <param name="maxDelay">Maximum delay between retries (default: 30 seconds).</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0).</param>
    /// <returns>An observable that retries on errors with exponential backoff.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .RetryWithBackoff(maxRetries: 5)
    ///     .Subscribe(messages => UpdateUI(messages));
    /// </code>
    /// </example>
    public static IObservable<T> RetryWithBackoff<T>(
        this IObservable<T> source,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries, nameof(maxRetries));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(backoffMultiplier, 1.0, nameof(backoffMultiplier));

        var delay = initialDelay ?? TimeSpan.FromSeconds(1);
        var max = maxDelay ?? TimeSpan.FromSeconds(30);

        return source.Catch<T, Exception>(ex =>
        {
            return Observable.Defer(() =>
            {
                var currentDelay = TimeSpan.FromTicks(Math.Min(delay.Ticks * (long)Math.Pow(backoffMultiplier, maxRetries), max.Ticks));
                return Observable.Timer(currentDelay).SelectMany(_ => source.RetryWithBackoff(maxRetries - 1, delay, max, backoffMultiplier));
            });
        });
    }

    /// <summary>
    /// Provides a fallback value when the observable errors, allowing the stream to continue.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="fallbackValue">The value to emit when an error occurs.</param>
    /// <returns>An observable that emits the fallback value on errors and continues.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .WithFallback(Array.Empty&lt;Message&gt;())
    ///     .Subscribe(messages => UpdateUI(messages));
    /// </code>
    /// </example>
    public static IObservable<T> WithFallback<T>(this IObservable<T> source, T fallbackValue)
    {
        return source == null
            ? throw new ArgumentNullException(nameof(source))
            : source.Catch<T, Exception>(_ => Observable.Return(fallbackValue));
    }

    #endregion

    #region Timing and Debouncing

    /// <summary>
    /// Debounces updates but never misses the first or last value in a sequence.
    /// Unlike standard Debounce, this ensures both immediate feedback and final state.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="window">The time window for debouncing.</param>
    /// <returns>An observable that debounces but preserves first and last values.</returns>
    /// <example>
    /// <code>
    /// // For search input - immediate response, then debounced updates
    /// var searchResults = searchText
    ///     .SmartDebounce(TimeSpan.FromMilliseconds(300))
    ///     .SelectMany(term => client.Query&lt;Result[]&gt;("search", new { term }))
    ///     .Subscribe(results => UpdateSearchResults(results));
    /// </code>
    /// </example>
    public static IObservable<T> SmartDebounce<T>(this IObservable<T> source, TimeSpan window)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(window, TimeSpan.Zero, nameof(window));

        return Observable.Create<T>(observer =>
        {
            var gate = new object();
            var hasPending = false;
            var lastValue = default(T);
            var timerDisposable = Disposable.Empty;
            var isCompleted = false;

            return source.Subscribe(
                onNext: value =>
                {
                    lock (gate)
                    {
                        lastValue = value;
                        hasPending = true;
                        timerDisposable.Dispose();
                        timerDisposable = Observable.Timer(window).Subscribe(_ =>
                        {
                            lock (gate)
                            {
                                if (hasPending && !isCompleted)
                                {
                                    observer.OnNext(lastValue!);
                                    hasPending = false;
                                }
                            }
                        });
                    }
                },
                onError: observer.OnError,
                onCompleted: () =>
                {
                    lock (gate)
                    {
                        isCompleted = true;
                        timerDisposable.Dispose();
                        if (hasPending)
                        {
                            observer.OnNext(lastValue!);
                        }
                        observer.OnCompleted();
                    }
                });
        });
    }

    #endregion

    #region Sharing and Caching

    /// <summary>
    /// Shares the subscription with reference counting and replays the latest value to new subscribers.
    /// Useful for expensive subscriptions that should be shared across multiple consumers.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <returns>A shared observable that replays the latest value.</returns>
    /// <example>
    /// <code>
    /// // Share expensive subscription across multiple UI components
    /// var sharedUserData = client.Observe&lt;User&gt;("users:current")
    ///     .ShareReplayLatest();
    ///
    /// // Multiple subscribers get the same data
    /// sharedUserData.Subscribe(user => header.UpdateUser(user));
    /// sharedUserData.Subscribe(user => profile.UpdateUser(user));
    /// </code>
    /// </example>
    public static IObservable<T> ShareReplayLatest<T>(this IObservable<T> source) => source == null ? throw new ArgumentNullException(nameof(source)) : source.Replay(1).RefCount();

    #endregion

    #region Cancellation and Lifecycle

    /// <summary>
    /// Automatically disposes the subscription when a cancellation token is triggered.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An observable that completes when the token is cancelled.</returns>
    /// <example>
    /// <code>
    /// using var cts = new CancellationTokenSource();
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .TakeUntilCanceled(cts.Token)
    ///     .Subscribe(messages => UpdateUI(messages));
    ///
    /// // Later: cts.Cancel(); // Automatically disposes subscription
    /// </code>
    /// </example>
    public static IObservable<T> TakeUntilCanceled<T>(this IObservable<T> source, CancellationToken cancellationToken)
    {
        return source == null
            ? throw new ArgumentNullException(nameof(source))
            : source.TakeUntil(Observable.Create<Unit>(observer =>
        {
            var registration = cancellationToken.Register(() => observer.OnNext(Unit.Default));
            return registration;
        }));
    }

    #endregion

    #region Connection-Aware Operations

    /// <summary>
    /// Only emits values when the Convex client is connected.
    /// Buffers values received during disconnection and emits them when reconnected.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="client">The Convex client to monitor connection state.</param>
    /// <returns>An observable that only emits when connected.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .WhenConnected(client)
    ///     .Subscribe(messages => UpdateUI(messages));
    /// </code>
    /// </example>
    public static IObservable<T> WhenConnected<T>(this IObservable<T> source, IConvexClient client)
    {
        return source == null
            ? throw new ArgumentNullException(nameof(source))
            : client == null
            ? throw new ArgumentNullException(nameof(client))
            : source.CombineLatest(client.ConnectionStateChanges, (value, state) => (value, state))
            .Where(tuple => tuple.state == ConnectionState.Connected)
            .Select(tuple => tuple.value);
    }

    /// <summary>
    /// Buffers updates during poor connection quality and emits them when connection improves.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="client">The Convex client to monitor connection quality.</param>
    /// <param name="bufferSize">Maximum number of items to buffer (default: 10).</param>
    /// <returns>An observable that buffers during poor connection.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message&gt;("messages:stream")
    ///     .BufferDuringPoorConnection(client, bufferSize: 50)
    ///     .Subscribe(messages => ProcessBatch(messages));
    /// </code>
    /// </example>
    public static IObservable<IList<T>> BufferDuringPoorConnection<T>(
        this IObservable<T> source,
        IConvexClient client,
        int bufferSize = 10)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize, nameof(bufferSize));

        return Observable.Create<IList<T>>(observer =>
        {
            var buffer = new List<T>();
            var isBuffering = false;

            var qualitySubscription = client.ConnectionQualityChanges.Subscribe(quality =>
            {
                var shouldBuffer = quality == ConnectionQuality.Poor;
                if (shouldBuffer != isBuffering)
                {
                    isBuffering = shouldBuffer;
                    if (!isBuffering && buffer.Count > 0)
                    {
                        observer.OnNext([.. buffer]);
                        buffer.Clear();
                    }
                }
            });

            var sourceSubscription = source.Subscribe(
                onNext: value =>
                {
                    if (isBuffering)
                    {
                        buffer.Add(value);
                        if (buffer.Count >= bufferSize)
                        {
                            observer.OnNext([.. buffer]);
                            buffer.Clear();
                        }
                    }
                    else
                    {
                        observer.OnNext([value]);
                    }
                },
                onError: observer.OnError,
                onCompleted: () =>
                {
                    if (buffer.Count > 0)
                    {
                        observer.OnNext(buffer);
                    }
                    observer.OnCompleted();
                });

            return new CompositeDisposable(qualitySubscription, sourceSubscription);
        });
    }

    #endregion

    #region Debugging and Logging

    /// <summary>
    /// Logs all values, errors, and completions to the console for debugging.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="name">Optional name for the observable in logs.</param>
    /// <returns>The same observable with logging side effects.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Message[]&gt;("messages:list")
    ///     .LogToConsole("Messages")
    ///     .Subscribe(messages => UpdateUI(messages));
    /// // Output: [Messages] OnNext: 5 items
    /// // Output: [Messages] OnCompleted
    /// </code>
    /// </example>
    public static IObservable<T> LogToConsole<T>(this IObservable<T> source, string? name = null)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var prefix = string.IsNullOrEmpty(name) ? "" : $"[{name}] ";

        return Observable.Create<T>(observer => source.Subscribe(
            onNext: value =>
            {
                Console.WriteLine($"{prefix}OnNext: {value}");
                observer.OnNext(value);
            },
            onError: error =>
            {
                Console.WriteLine($"{prefix}OnError: {error.Message}");
                observer.OnError(error);
            },
            onCompleted: () =>
            {
                Console.WriteLine($"{prefix}OnCompleted");
                observer.OnCompleted();
            }));
    }

    #endregion
}
