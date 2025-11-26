using System.Collections;
using System.Collections.ObjectModel;

namespace Convex.Client.Features.DataAccess.Mutations;

/// <summary>
/// A collection wrapper that provides automatic snapshot and rollback capabilities for optimistic updates.
/// This class simplifies managing collection state during mutations by automatically tracking changes
/// and providing easy rollback on failure.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// This class is designed to work seamlessly with IMutationBuilder.OptimisticWithAutoRollback().
/// It provides snapshot/rollback functionality along with collection mutation methods.
/// </remarks>
/// <example>
/// <code>
/// // Create an optimistic collection
/// var messages = new OptimisticCollection&lt;Message&gt;(existingMessages);
///
/// // Use with mutation builder for automatic rollback
/// await client.Mutation("messages:send")
///     .WithArgs(new { text = "Hello!" })
///     .OptimisticWithAutoRollback(
///         getter: () => messages.Items.ToList(),
///         setter: value => messages.Replace(value),
///         update: items => items.Append(newMessage).ToList()
///     )
///     .ExecuteAsync();
/// </code>
/// </example>
/// <remarks>
/// Creates a new optimistic collection with initial items.
/// </remarks>
/// <param name="items">Initial items for the collection.</param>
public sealed class OptimisticCollection<T>(IEnumerable<T> items) : IReadOnlyList<T>, IDisposable
{
    private readonly object _lock = new();
    private List<T> _items = [.. items ?? []];
    private List<T>? _snapshot;
    private bool _isDisposed;

    /// <summary>
    /// Occurs when the collection changes (items added, removed, cleared, or replaced).
    /// </summary>
    public event EventHandler? CollectionChanged;

    /// <summary>
    /// Creates a new empty optimistic collection.
    /// </summary>
    public OptimisticCollection() : this([])
    {
    }

    /// <summary>
    /// Gets the current items in the collection as a read-only list.
    /// </summary>
    public IReadOnlyList<T> Items
    {
        get
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return new ReadOnlyCollection<T>(_items);
            }
        }
    }

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
    /// Gets whether a snapshot is currently active.
    /// </summary>
    public bool HasSnapshot
    {
        get
        {
            lock (_lock)
            {
                return _snapshot != null;
            }
        }
    }

    /// <summary>
    /// Gets the item at the specified index.
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
    }

    /// <summary>
    /// Creates a snapshot of the current collection state.
    /// This allows rolling back to this state later if needed.
    /// </summary>
    public void CreateSnapshot()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _snapshot = [.. _items];
        }
    }

    /// <summary>
    /// Rolls back the collection to the last snapshot.
    /// If no snapshot exists, this method does nothing.
    /// </summary>
    /// <returns>True if rollback occurred, false if no snapshot exists.</returns>
    public bool Rollback()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            if (_snapshot == null)
            {
                return false;
            }

            _items = [.. _snapshot];
            _snapshot = null;
            RaiseCollectionChanged();
            return true;
        }
    }

    /// <summary>
    /// Commits the current state and discards the snapshot.
    /// After this, rollback is no longer possible.
    /// </summary>
    public void CommitSnapshot()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _snapshot = null;
        }
    }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Add(item);
            RaiseCollectionChanged();
        }
    }

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
            _items.AddRange(items);
            RaiseCollectionChanged();
        }
    }

    /// <summary>
    /// Removes an item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>True if the item was removed, false if it wasn't found.</returns>
    public bool Remove(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            var removed = _items.Remove(item);
            if (removed)
            {
                RaiseCollectionChanged();
            }
            return removed;
        }
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.RemoveAt(index);
            RaiseCollectionChanged();
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
                RaiseCollectionChanged();
            }
            return removed;
        }
    }

    /// <summary>
    /// Clears all items from the collection.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            _items.Clear();
            RaiseCollectionChanged();
        }
    }

    /// <summary>
    /// Replaces all items in the collection with the specified items.
    /// </summary>
    /// <param name="items">The new items for the collection.</param>
    public void Replace(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            _items = [.. items];
            RaiseCollectionChanged();
        }
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>True if the item is found, false otherwise.</returns>
    public bool Contains(T item)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            return _items.Contains(item);
        }
    }

    /// <summary>
    /// Searches for an element that matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The first matching element, or default if not found.</returns>
    public T? Find(Predicate<T> predicate)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        lock (_lock)
        {
            ThrowIfDisposed();
            return _items.Find(predicate);
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
    /// Disposes the collection and clears all items and snapshots.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _items.Clear();
            _snapshot = null;
            CollectionChanged = null;
            _isDisposed = true;
        }
    }

    private void RaiseCollectionChanged()
    {
        // Raise outside the lock to prevent deadlocks
        EventHandler? handler;
        lock (_lock)
        {
            handler = CollectionChanged;
        }

        handler?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(OptimisticCollection<T>));
        }
    }
}
