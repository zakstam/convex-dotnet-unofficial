using System.Reactive.Linq;

namespace Convex.Client.Slices.Subscriptions;

/// <summary>
/// Extension methods for IObservable that provide collection synchronization utilities.
/// </summary>
/// <remarks>
/// For filtering, transforming, and error handling, use System.Reactive.Linq operators directly:
/// - Filtering: observable.Where(predicate)
/// - Transforming: observable.Select(selector)
/// - Error handling: observable.Catch() or observable.Subscribe(onNext, onError)
/// - Waiting for value: observable.FirstAsync()
/// - Throttling: observable.Throttle(timespan)
/// - Debouncing: observable.Debounce(timespan)
/// </remarks>
public static class SubscriptionExtensions
{
    #region Collection Sync Extensions

    /// <summary>
    /// Creates an observable list that automatically synchronizes with the observable stream.
    /// The list will update whenever new data arrives from the server.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="observable">The observable providing collection data.</param>
    /// <param name="synchronizationContext">Optional synchronization context for UI thread marshalling.</param>
    /// <returns>An ObservableConvexList that is bound to the observable.</returns>
    /// <example>
    /// <code>
    /// // Create an observable and get an auto-syncing observable list
    /// var observable = client.Observe&lt;Todo[]&gt;("todos:list");
    /// var todoList = observable.ToObservableList();
    ///
    /// // Use with UI data binding (WPF, MAUI, etc.)
    /// listView.ItemsSource = todoList;
    /// </code>
    /// </example>
    public static ObservableConvexList<T> ToObservableList<T>(
        this IObservable<IEnumerable<T>> observable,
        SynchronizationContext? synchronizationContext = null)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        var list = new ObservableConvexList<T>(synchronizationContext);
        _ = list.BindToObservable(observable);
        return list;
    }

    /// <summary>
    /// Binds an observable to an existing collection, replacing all items when updates arrive.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="observable">The observable providing collection data.</param>
    /// <param name="collection">The collection to synchronize.</param>
    /// <returns>An IDisposable that unbinds when disposed.</returns>
    /// <example>
    /// <code>
    /// var myCollection = new ObservableCollection&lt;Todo&gt;();
    /// var observable = client.Observe&lt;Todo[]&gt;("todos:list");
    ///
    /// using var binding = observable.BindToCollection(myCollection);
    /// </code>
    /// </example>
    public static IDisposable BindToCollection<T>(
        this IObservable<IEnumerable<T>> observable,
        ICollection<T> collection)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        return observable.Subscribe(
            onNext: items =>
            {
                collection.Clear();
                foreach (var item in items)
                {
                    collection.Add(item);
                }
            },
            onError: ex =>
            {
                // Error handling can be customized by the caller using .Catch() or other Rx operators
            });
    }

    /// <summary>
    /// Binds an observable to a list, replacing all items when updates arrive using ReplaceAll if available.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="observable">The observable providing list data.</param>
    /// <param name="list">The list to synchronize.</param>
    /// <returns>An IDisposable that unbinds when disposed.</returns>
    public static IDisposable BindToList<T>(
        this IObservable<IEnumerable<T>> observable,
        IList<T> list)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        return observable.Subscribe(
            onNext: items =>
            {
                list.Clear();
                foreach (var item in items)
                {
                    list.Add(item);
                }
            },
            onError: ex =>
            {
                // Error handling can be customized by the caller using .Catch() or other Rx operators
            });
    }

    /// <summary>
    /// Synchronizes a single value observable to a mutable reference.
    /// Updates the reference whenever the observable value changes.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="observable">The observable providing the value.</param>
    /// <param name="setter">Action to set the value.</param>
    /// <returns>An IDisposable that unbinds when disposed.</returns>
    /// <example>
    /// <code>
    /// private User? _currentUser;
    /// var observable = client.Observe&lt;User&gt;("users:current");
    ///
    /// using var binding = observable.SyncTo(value => _currentUser = value);
    /// </code>
    /// </example>
    public static IDisposable SyncTo<T>(
        this IObservable<T> observable,
        Action<T> setter)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        if (setter == null)
        {
            throw new ArgumentNullException(nameof(setter));
        }

        return observable.Subscribe(
            onNext: setter,
            onError: ex =>
            {
                // Error handling can be customized by the caller using .Catch() or other Rx operators
            });
    }

    #endregion
}
