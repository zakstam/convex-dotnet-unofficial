using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Convex.Client.Shared.Extensions;

/// <summary>
/// Extension methods for bridging IAsyncEnumerable to IObservable.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Converts an IAsyncEnumerable to an IObservable.
    /// The observable will emit items as they are produced by the async enumerable.
    /// </summary>
    /// <typeparam name="T">The type of items in the sequence.</typeparam>
    /// <param name="source">The async enumerable to convert.</param>
    /// <param name="cancellationToken">Optional cancellation token for the enumeration.</param>
    /// <returns>An observable that emits items from the async enumerable.</returns>
    public static IObservable<T> ToObservable<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<T>(async (observer, ct) =>
        {
            // Combine the provided cancellation token with the observer's cancellation token
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken);

            try
            {
                await foreach (var item in source.WithCancellation(linkedCts.Token).ConfigureAwait(false))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested)
            {
                // Cancellation is normal completion for observables
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }

            return Disposable.Empty;
        });
    }

    /// <summary>
    /// Converts an IAsyncEnumerable to an IObservable with custom error handling.
    /// </summary>
    /// <typeparam name="T">The type of items in the sequence.</typeparam>
    /// <param name="source">The async enumerable to convert.</param>
    /// <param name="onError">Custom error handler that returns true to continue or false to propagate the error.</param>
    /// <param name="cancellationToken">Optional cancellation token for the enumeration.</param>
    /// <returns>An observable that emits items from the async enumerable.</returns>
    public static IObservable<T> ToObservable<T>(
        this IAsyncEnumerable<T> source,
        Func<Exception, bool> onError,
        CancellationToken cancellationToken = default)
    {
        return Observable.Create<T>(async (observer, ct) =>
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellationToken);

            try
            {
                await foreach (var item in source.WithCancellation(linkedCts.Token).ConfigureAwait(false))
                {
                    observer.OnNext(item);
                }
                observer.OnCompleted();
            }
            catch (OperationCanceledException) when (linkedCts.Token.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                if (!onError(ex))
                {
                    observer.OnError(ex);
                }
            }

            return Disposable.Empty;
        });
    }
}
