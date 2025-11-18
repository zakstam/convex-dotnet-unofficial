using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Convex.Client.Shared.Extensions;

/// <summary>
/// Extension methods for IObservable that provide convenient subscription patterns.
/// </summary>
public static class ObservableExtensions
{
    /// <summary>
    /// Subscribes to the observable and returns a Task that completes with the first value.
    /// Automatically disposes the subscription when the task completes.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with the first value emitted by the observable.</returns>
    /// <example>
    /// <code>
    /// var firstValue = await client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .SubscribeAsync();
    /// </code>
    /// </example>
    public static Task<T> SubscribeAsync<T>(
        this IObservable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source
            .Take(1)
            .ToTask(cancellationToken);
    }

    /// <summary>
    /// Converts an observable to an IAsyncEnumerable for use with async/await patterns.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to convert.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>An async enumerable that yields values from the observable.</returns>
    /// <example>
    /// <code>
    /// await foreach (var todos in client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list").ToAsyncEnumerable())
    /// {
    ///     UpdateUI(todos);
    /// }
    /// </code>
    /// </example>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IObservable<T> source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
        var subscription = source.Subscribe(
            onNext: value => channel.Writer.TryWrite(value),
            onError: error => channel.Writer.TryComplete(error),
            onCompleted: () => channel.Writer.TryComplete());

        using var _ = subscription;

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Subscribes to the observable with automatic cleanup when the provided scope is disposed.
    /// Useful for managing subscription lifecycle with dependency injection containers.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="onNext">Action to execute when a value is emitted.</param>
    /// <param name="scope">The scope that controls the subscription lifetime.</param>
    /// <returns>A disposable that can be used to unsubscribe manually if needed.</returns>
    /// <example>
    /// <code>
    /// // In a service with IServiceScope
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .SubscribeWithCleanup(
    ///         todos => UpdateUI(todos),
    ///         serviceScope);
    /// // Subscription is automatically disposed when serviceScope is disposed
    /// </code>
    /// </example>
    public static IDisposable SubscribeWithCleanup<T>(
        this IObservable<T> source,
        Action<T> onNext,
        IDisposable scope)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (onNext == null)
        {
            throw new ArgumentNullException(nameof(onNext));
        }
        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        var subscription = source.Subscribe(onNext);
        return new CompositeDisposable(subscription, scope);
    }

    /// <summary>
    /// Subscribes to the observable with automatic cleanup when the provided scope is disposed.
    /// Includes error handling.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to subscribe to.</param>
    /// <param name="onNext">Action to execute when a value is emitted.</param>
    /// <param name="onError">Action to execute when an error occurs.</param>
    /// <param name="scope">The scope that controls the subscription lifetime.</param>
    /// <returns>A disposable that can be used to unsubscribe manually if needed.</returns>
    public static IDisposable SubscribeWithCleanup<T>(
        this IObservable<T> source,
        Action<T> onNext,
        Action<Exception> onError,
        IDisposable scope)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (onNext == null)
        {
            throw new ArgumentNullException(nameof(onNext));
        }
        if (onError == null)
        {
            throw new ArgumentNullException(nameof(onError));
        }
        if (scope == null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        var subscription = source.Subscribe(onNext, onError);
        return new CompositeDisposable(subscription, scope);
    }

    /// <summary>
    /// Creates an observable that only emits the latest value when subscribed.
    /// Useful for getting the current state without creating a long-lived subscription.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to observe.</param>
    /// <returns>An observable that emits only the latest value.</returns>
    /// <example>
    /// <code>
    /// // Get the latest todos without maintaining a subscription
    /// var latestTodos = await client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .ObserveLatest()
    ///     .SubscribeAsync();
    /// </code>
    /// </example>
    public static IObservable<T> ObserveLatest<T>(this IObservable<T> source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source
            .Replay(1)
            .RefCount()
            .Take(1);
    }

    /// <summary>
    /// Creates an observable that only emits distinct consecutive values.
    /// Uses the default equality comparer for the type.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <param name="source">The observable to filter.</param>
    /// <returns>An observable that only emits when the value changes.</returns>
    /// <example>
    /// <code>
    /// // Only update UI when todos actually change
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .DistinctUntilChanged()
    ///     .Subscribe(todos => UpdateUI(todos));
    /// </code>
    /// </example>
    public static IObservable<T> DistinctUntilChanged<T>(this IObservable<T> source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return source.DistinctUntilChanged();
    }

    /// <summary>
    /// Creates an observable that only emits distinct consecutive values based on a key selector.
    /// </summary>
    /// <typeparam name="T">The type of values emitted by the observable.</typeparam>
    /// <typeparam name="TKey">The type of the key to compare.</typeparam>
    /// <param name="source">The observable to filter.</param>
    /// <param name="keySelector">Function to extract the key from each value.</param>
    /// <returns>An observable that only emits when the key changes.</returns>
    /// <example>
    /// <code>
    /// // Only update when the count changes
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("todos:list")
    ///     .DistinctUntilChanged(todos => todos.Count)
    ///     .Subscribe(todos => UpdateCount(todos.Count));
    /// </code>
    /// </example>
    public static IObservable<T> DistinctUntilChanged<T, TKey>(
        this IObservable<T> source,
        Func<T, TKey> keySelector)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        return source.DistinctUntilChanged(keySelector);
    }
}

