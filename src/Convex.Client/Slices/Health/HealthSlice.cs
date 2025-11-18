using Convex.Client.Shared.Connection;

namespace Convex.Client.Slices.Health;

/// <summary>
/// Health slice - provides connection health monitoring and metrics tracking.
/// This is a self-contained vertical slice that handles all health monitoring functionality.
/// </summary>
public class HealthSlice : IConvexHealth
{
    private readonly HealthMonitor _implementation;

    public HealthSlice() => _implementation = new HealthMonitor();

    public void RecordMessageReceived()
        => _implementation.RecordMessageReceived();

    public void RecordMessageSent()
        => _implementation.RecordMessageSent();

    public void RecordLatency(double latencyMs)
        => _implementation.RecordLatency(latencyMs);

    public void RecordReconnection()
        => _implementation.RecordReconnection();

    public void RecordConnectionEstablished()
        => _implementation.RecordConnectionEstablished();

    public void RecordError(Exception error)
        => _implementation.RecordError(error);

    public double? GetAverageLatency()
        => _implementation.GetAverageLatency();

    public long GetMessagesReceived()
        => _implementation.GetMessagesReceived();

    public long GetMessagesSent()
        => _implementation.GetMessagesSent();

    public int GetReconnectionCount()
        => _implementation.GetReconnectionCount();

    public TimeSpan? GetTimeSinceLastMessage()
        => _implementation.GetTimeSinceLastMessage();

    public TimeSpan? GetConnectionUptime()
        => _implementation.GetConnectionUptime();

    public IReadOnlyList<Exception> GetRecentErrors()
        => _implementation.GetRecentErrors();

    public void Reset()
        => _implementation.Reset();

    public ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions)
        => _implementation.CreateHealthCheck(connectionState, activeSubscriptions);
}
