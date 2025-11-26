namespace Convex.Client.Features.Observability.Diagnostics;

/// <summary>
/// Diagnostics slice - provides performance tracking and disconnection monitoring.
/// This is a self-contained vertical slice that handles all diagnostic functionality.
/// </summary>
public class DiagnosticsSlice : IConvexDiagnostics
{
    private readonly PerformanceTrackerImplementation _performance;
    private readonly DisconnectTrackerImplementation _disconnects;

    public DiagnosticsSlice()
    {
        _performance = new PerformanceTrackerImplementation();
        _disconnects = new DisconnectTrackerImplementation();
    }

    public IPerformanceTracker Performance => _performance;

    public IDisconnectTracker Disconnects => _disconnects;
}
