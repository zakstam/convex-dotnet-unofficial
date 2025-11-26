namespace Convex.Client.Features.RealTime.Subscriptions;

/// <summary>
/// Subscriptions slice - provides utility classes and extension methods for working with real-time subscriptions.
/// This is a utility slice that enhances the subscription functionality exposed on IConvexClient.
///
/// Note: Unlike operational slices, this slice provides optional utility classes (ObservableConvexList,
/// SubscriptionExtensions) rather than core functionality. The core subscription functionality is
/// directly exposed on IConvexClient via Observe() methods.
/// </summary>
public static class SubscriptionsSlice
{
    // This is a utility slice with no state or instance members.
    // All functionality is provided through:
    // - ObservableConvexList<T> - thread-safe observable collection
    // - SubscriptionExtensions - extension methods for IObservable<T>

    /// <summary>
    /// Gets the version of the Subscriptions slice.
    /// </summary>
    public static string Version => "1.0.0";
}
