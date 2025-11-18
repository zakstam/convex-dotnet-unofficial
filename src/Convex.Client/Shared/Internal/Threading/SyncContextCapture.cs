namespace Convex.Client.Shared.Internal.Threading;

/// <summary>
/// Captures the SynchronizationContext at creation time and provides methods to marshal callbacks to that context.
/// This enables automatic UI thread marshalling for event callbacks.
/// </summary>
public sealed class SyncContextCapture
{
    private readonly SynchronizationContext? _capturedContext;

    /// <summary>
    /// Captures the current SynchronizationContext.
    /// If no context exists (console apps), callbacks will execute on the calling thread.
    /// </summary>
    public SyncContextCapture() => _capturedContext = SynchronizationContext.Current;

    /// <summary>
    /// Explicitly captures the provided SynchronizationContext.
    /// </summary>
    /// <param name="context">The context to capture, or null for no marshalling.</param>
    public SyncContextCapture(SynchronizationContext? context) => _capturedContext = context;

    /// <summary>
    /// Gets whether a SynchronizationContext was captured.
    /// </summary>
    public bool HasContext => _capturedContext != null;

    /// <summary>
    /// Posts an action to the captured SynchronizationContext.
    /// If no context was captured, the action executes immediately on the calling thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Post(Action action)
    {
        if (_capturedContext != null)
        {
            _capturedContext.Post(_ => action(), null);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// Posts an action with a parameter to the captured SynchronizationContext.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="state">The parameter to pass to the action.</param>
    public void Post<T>(Action<T> action, T state)
    {
        if (_capturedContext != null)
        {
            _capturedContext.Post(_ => action(state), null);
        }
        else
        {
            action(state);
        }
    }

    /// <summary>
    /// Sends an action to the captured SynchronizationContext synchronously.
    /// If no context was captured, the action executes immediately on the calling thread.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void Send(Action action)
    {
        if (_capturedContext != null)
        {
            _capturedContext.Send(_ => action(), null);
        }
        else
        {
            action();
        }
    }
}
