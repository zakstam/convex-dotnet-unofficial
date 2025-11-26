using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Convex.Client.Features.RealTime.Subscriptions;

/// <summary>
/// A thread-safe observable collection designed for use with Convex subscriptions.
/// Provides automatic synchronization, change notifications, and UI thread marshalling.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// This class is optimized for scenarios where:
/// - Collection is updated from subscription callbacks (potentially on background threads)
/// - UI needs to be notified of changes (via INotifyCollectionChanged)
/// - Thread safety is required for concurrent access
/// </remarks>
/// <example>
/// <code>
/// // Create an observable list and bind to an observable stream
/// var todoList = new ObservableConvexList&lt;Todo&gt;();
///
/// var observable = client.Observe&lt;Todo[]&gt;("todos:list");
/// todoList.BindToObservable(observable);
///
/// // Use with UI data binding (WPF, MAUI, etc.)
/// listView.ItemsSource = todoList;
/// </code>
/// </example>
/// <remarks>
/// Creates a new observable list with optional synchronization context.
/// </remarks>
/// <param name="synchronizationContext">
/// Optional synchronization context for marshalling change notifications to a specific thread (e.g., UI thread).
/// If null, the current SynchronizationContext will be captured.
/// </param>
public sealed class ObservableConvexList<T>(SynchronizationContext? synchronizationContext = null) : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
{
    private readonly object _lock = new();
    private readonly List<T> _items = [];
    private readonly SynchronizationContext? _synchronizationContext = synchronizationContext ?? SynchronizationContext.Current;
    private bool _isDisposed;
    private IDisposable? _subscriptionBinding;

    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Creates a new observable list with initial items.
    /// </summary>
    /// <param name="items">Initial items to populate the collection.</param>
    /// <param name="synchronizationContext">Optional synchronization context.</param>
    public ObservableConvexList(IEnumerable<T> items, SynchronizationContext? synchronizationContext = null)
        : this(synchronizationContext)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        _items.AddRange(items);
    }

    #region IList<T> Implementation

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _items.Count;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _items[index];
            }
        }
        set
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var oldItem = _items[index];
                _items[index] = value;
                RaiseCollectionChanged(NotifyCollectionChangedAction.Replace, value, oldItem, index);
            }
        }
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    public void Add(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Add(item);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, _items.Count - 1);
            RaisePropertyChanged(nameof(Count));
        }
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Clear();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Reset);
            RaisePropertyChanged(nameof(Count));
        }
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    public bool Contains(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _items.Contains(item);
        }
    }

    /// <summary>
    /// Copies the elements to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.CopyTo(array, arrayIndex);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            // Return a snapshot enumerator to avoid collection modified exceptions
            return new List<T>(_items).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    public int IndexOf(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _items.IndexOf(item);
        }
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    public void Insert(int index, T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Insert(index, item);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            RaisePropertyChanged(nameof(Count));
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific item from the collection.
    /// </summary>
    public bool Remove(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            var index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                RaisePropertyChanged(nameof(Count));
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            var item = _items[index];
            _items.RemoveAt(index);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            RaisePropertyChanged(nameof(Count));
        }
    }

    #endregion

    #region Additional Collection Operations

    /// <summary>
    /// Adds multiple items to the collection.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            var itemsList = items.ToList();
            _items.AddRange(itemsList);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, itemsList, _items.Count - itemsList.Count);
            RaisePropertyChanged(nameof(Count));
        }
    }

    /// <summary>
    /// Replaces all items in the collection with new items.
    /// This is more efficient than Clear() + AddRange() for large collections.
    /// </summary>
    /// <param name="items">The new items for the collection.</param>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Clear();
            _items.AddRange(items);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Reset);
            RaisePropertyChanged(nameof(Count));
        }
    }

    /// <summary>
    /// Removes all items that match the predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match items.</param>
    /// <returns>The number of items removed.</returns>
    public int RemoveAll(Predicate<T> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            var removed = _items.RemoveAll(predicate);
            if (removed > 0)
            {
                RaiseCollectionChanged(NotifyCollectionChangedAction.Reset);
                RaisePropertyChanged(nameof(Count));
            }
            return removed;
        }
    }

    #endregion

    #region Observable Integration

    /// <summary>
    /// Binds this collection to an observable stream.
    /// The collection will automatically update when new data arrives from the observable.
    /// </summary>
    /// <param name="observable">The observable to bind to.</param>
    /// <returns>An IDisposable that unbinds the observable when disposed.</returns>
    public IDisposable BindToObservable(IObservable<IEnumerable<T>> observable)
    {
        if (observable == null)
        {
            throw new ArgumentNullException(nameof(observable));
        }

        lock (_lock)
        {
            ThrowIfDisposed();

            // Unbind any existing observable
            _subscriptionBinding?.Dispose();

            // Bind to the new observable
            _subscriptionBinding = observable.Subscribe(
                onNext: items => ReplaceAll(items),
                onError: ex => { /* Error can be handled by the caller using Rx operators */ }
            );
        }

        return new SubscriptionBinding(this);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the collection and unbinds any active subscriptions.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _subscriptionBinding?.Dispose();
            _subscriptionBinding = null;

            _items.Clear();
            CollectionChanged = null;
            PropertyChanged = null;

            _isDisposed = true;
        }
    }

    #endregion

    #region Private Helpers

    private void RaiseCollectionChanged(NotifyCollectionChangedAction action) => RaiseEvent(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action)));

    private void RaiseCollectionChanged(NotifyCollectionChangedAction action, T item, int index) => RaiseEvent(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index)));

    private void RaiseCollectionChanged(NotifyCollectionChangedAction action, T newItem, T oldItem, int index) => RaiseEvent(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index)));

    private void RaiseCollectionChanged(NotifyCollectionChangedAction action, IList items, int index) => RaiseEvent(() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, items, index)));

    private void RaisePropertyChanged(string propertyName) => RaiseEvent(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));

    private void RaiseEvent(Action action)
    {
        if (_synchronizationContext != null && _synchronizationContext != SynchronizationContext.Current)
        {
            _synchronizationContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ObservableConvexList<T>));
        }
    }

    #endregion

    #region Nested Types

    private sealed class SubscriptionBinding(ObservableConvexList<T> list) : IDisposable
    {
        private ObservableConvexList<T>? _list = list;

        public void Dispose()
        {
            var list = _list;
            if (list != null)
            {
                lock (list._lock)
                {
                    list._subscriptionBinding?.Dispose();
                    list._subscriptionBinding = null;
                }
                _list = null;
            }
        }
    }

    #endregion
}
