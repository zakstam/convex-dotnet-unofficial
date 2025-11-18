namespace Convex.Client.Shared.Internal.Threading;

/// <summary>
/// Helper utilities for thread marshalling and event invocation.
/// Automatically marshals callbacks to the UI thread when needed.
/// </summary>
internal static class ThreadMarshallingHelper
{
    /// <summary>
    /// Invokes an event handler, marshalling to the captured SynchronizationContext if present.
    /// </summary>
    /// <typeparam name="T">The type of the event argument.</typeparam>
    /// <param name="handler">The event handler to invoke.</param>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The event arguments.</param>
    /// <param name="syncContext">The captured SynchronizationContext.</param>
    public static void InvokeEvent<T>(EventHandler<T>? handler, object sender, T args, SyncContextCapture? syncContext)
    {
        if (handler == null)
        {
            return;
        }

        if (syncContext?.HasContext == true)
        {
            syncContext.Post(() => handler(sender, args));
        }
        else
        {
            handler(sender, args);
        }
    }

    /// <summary>
    /// Invokes a callback action, marshalling to the captured SynchronizationContext if present.
    /// </summary>
    /// <typeparam name="T">The type of the callback parameter.</typeparam>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="value">The value to pass to the callback.</param>
    /// <param name="syncContext">The captured SynchronizationContext.</param>
    public static void InvokeCallback<T>(Action<T>? callback, T value, SyncContextCapture? syncContext)
    {
        if (callback == null)
        {
            return;
        }

        if (syncContext?.HasContext == true)
        {
            syncContext.Post(callback, value);
        }
        else
        {
            callback(value);
        }
    }

    /// <summary>
    /// Invokes a callback action without parameters, marshalling to the captured SynchronizationContext if present.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="syncContext">The captured SynchronizationContext.</param>
    public static void InvokeCallback(Action? callback, SyncContextCapture? syncContext)
    {
        if (callback == null)
        {
            return;
        }

        if (syncContext?.HasContext == true)
        {
            syncContext.Post(callback);
        }
        else
        {
            callback();
        }
    }

    /// <summary>
    /// Determines whether thread marshalling is needed for the current context.
    /// </summary>
    /// <param name="syncContext">The captured SynchronizationContext.</param>
    /// <returns>True if marshalling is needed, false otherwise.</returns>
    public static bool NeedsMarshalling(SyncContextCapture? syncContext) =>
        syncContext?.HasContext == true;
}
