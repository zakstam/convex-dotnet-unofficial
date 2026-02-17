namespace Convex.Client.Features.Observability.Diagnostics;

/// <summary>
/// Diagnostics slice - provides performance tracking and disconnection monitoring.
/// This is a self-contained vertical slice that handles all diagnostic functionality.
/// </summary>
public class DiagnosticsSlice : IConvexDiagnostics
{
    private readonly PerformanceTrackerImplementation _performance;
    private readonly DisconnectTrackerImplementation _disconnects;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsSlice"/> class.
    /// </summary>
    public DiagnosticsSlice()
    {
        _performance = new PerformanceTrackerImplementation();
        _disconnects = new DisconnectTrackerImplementation();
    }

    /// <summary>
    /// Gets the performance.
    /// </summary>
    public IPerformanceTracker Performance => _performance;

    /// <summary>
    /// Gets the disconnects.
    /// </summary>
    public IDisconnectTracker Disconnects => _disconnects;
}
