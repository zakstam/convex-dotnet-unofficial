using System.Reactive.Disposables;

#if NET8_0_OR_GREATER || NET9_0_OR_GREATER
namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Helper class for tracking and managing multiple subscriptions in Blazor components.
/// Automatically disposes all tracked subscriptions when disposed, preventing memory leaks.
/// </summary>
/// <example>
/// <code>
/// public partial class MyComponent : ComponentBase, IDisposable
/// {
///     private readonly SubscriptionTracker _subscriptions = new();
///
///     protected override void OnInitialized()
///     {
///         _subscriptions.Add(
///             ConvexClient.Observe&lt;Message[]&gt;("functions/getMessages")
///                 .SubscribeToUI(this, messages => { /* handle */ })
///         );
///
///         _subscriptions.Add(
///             ConvexClient.Observe&lt;User&gt;("functions/getCurrentUser")
///                 .SubscribeToUI(this, user => { /* handle */ })
///         );
///     }
///
///     public void Dispose()
///     {
///         _subscriptions.Dispose(); // Disposes all subscriptions
///     }
/// }
/// </code>
/// </example>
public sealed class SubscriptionTracker : IDisposable
{
    private readonly List<IDisposable> _subscriptions = [];
    private bool _disposed;

    /// <summary>
    /// Adds a subscription to be tracked.
    /// </summary>
    /// <param name="subscription">The subscription to track.</param>
    /// <returns>The same subscription for method chaining.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the tracker has been disposed.</exception>
    public IDisposable Add(IDisposable? subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (subscription != null)
        {
            _subscriptions.Add(subscription);
        }

        return subscription ?? Disposable.Empty;
    }

    /// <summary>
    /// Adds multiple subscriptions to be tracked.
    /// </summary>
    /// <param name="subscriptions">The subscriptions to track.</param>
    public void AddRange(params IDisposable?[] subscriptions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var subscription in subscriptions)
        {
            if (subscription != null)
            {
                _subscriptions.Add(subscription);
            }
        }
    }

    /// <summary>
    /// Removes a subscription from tracking (without disposing it).
    /// </summary>
    /// <param name="subscription">The subscription to remove.</param>
    /// <returns>True if the subscription was found and removed, false otherwise.</returns>
    public bool Remove(IDisposable? subscription) => !_disposed && subscription != null && _subscriptions.Remove(subscription);

    /// <summary>
    /// Gets the number of tracked subscriptions.
    /// </summary>
    public int Count => _subscriptions.Count;

    /// <summary>
    /// Disposes all tracked subscriptions and clears the tracker.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var subscription in _subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _subscriptions.Clear();
        _disposed = true;
    }

    /// <summary>
    /// Clears all subscriptions without disposing them.
    /// Useful when you want to manually manage disposal.
    /// </summary>
    public void Clear()
    {
        if (_disposed)
        {
            return;
        }

        _subscriptions.Clear();
    }
}

/// <summary>
/// Extension methods for SubscriptionTracker to provide fluent API.
/// </summary>
public static class SubscriptionTrackerExtensions
{
    /// <summary>
    /// Adds a subscription to the tracker and returns the tracker for method chaining.
    /// </summary>
    /// <param name="tracker">The subscription tracker.</param>
    /// <param name="subscription">The subscription to track.</param>
    /// <returns>The tracker for method chaining.</returns>
    /// <example>
    /// <code>
    /// private readonly SubscriptionTracker _subscriptions = new();
    ///
    /// protected override void OnInitialized()
    /// {
    ///     _subscriptions
    ///         .Track(ConvexClient.Observe&lt;Message[]&gt;("functions/getMessages")
    ///             .SubscribeToUI(this, messages => { /* handle */ }))
    ///         .Track(ConvexClient.Observe&lt;User&gt;("functions/getCurrentUser")
    ///             .SubscribeToUI(this, user => { /* handle */ }));
    /// }
    /// </code>
    /// </example>
    public static SubscriptionTracker Track(this SubscriptionTracker tracker, IDisposable? subscription)
    {
        _ = tracker.Add(subscription);
        return tracker;
    }
}
#endif

