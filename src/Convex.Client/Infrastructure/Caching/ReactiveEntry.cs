using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Convex.Client.Infrastructure.Caching;

/// <summary>
/// A cache entry that wraps a value with a reactive Subject for notifications.
/// When the value changes, all subscribers are notified via the Subject.
/// </summary>
internal sealed class ReactiveEntry : IDisposable
{
    private readonly Subject<object?> _subject = new();
    private readonly object _lock = new();
    private bool _isDisposed;

    /// <summary>
    /// Gets the current cached value.
    /// </summary>
    public object? Value { get; private set; }

    /// <summary>
    /// Gets the type of the cached value.
    /// </summary>
    public Type? ValueType { get; private set; }

    /// <summary>
    /// Gets the source of this cache entry.
    /// </summary>
    public CacheEntrySource Source { get; private set; }

    /// <summary>
    /// Gets the timestamp when this entry was last updated.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Gets an observable that emits when the cached value changes.
    /// </summary>
    public IObservable<object?> Observable => _subject.AsObservable();

    /// <summary>
    /// Sets the cached value and notifies all subscribers.
    /// </summary>
    /// <param name="value">The new value to cache.</param>
    /// <param name="source">The source of this value.</param>
    public void SetValue(object? value, CacheEntrySource source)
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            Value = value;
            ValueType = value?.GetType();
            Source = source;
            Timestamp = DateTimeOffset.UtcNow;

            _subject.OnNext(value);
        }
    }

    /// <summary>
    /// Clears the cached value and notifies subscribers with null.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            Value = null;
            ValueType = null;
            Timestamp = DateTimeOffset.UtcNow;

            _subject.OnNext(null);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _subject.OnCompleted();
            _subject.Dispose();
        }
    }
}
