using Convex.Client.Infrastructure.Connection;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Observability.Health;

/// <summary>
/// Health slice - provides connection health monitoring and metrics tracking.
/// This is a self-contained vertical slice that handles all health monitoring functionality.
/// </summary>
public class HealthSlice : IConvexHealth
{
    private readonly HealthMonitor _implementation;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthSlice"/> class.
    /// </summary>
    public HealthSlice(ILogger? logger = null, bool enableDebugLogging = false)
        => _implementation = new HealthMonitor(logger, enableDebugLogging);

    /// <summary>
    /// Records message received.
    /// </summary>
    public void RecordMessageReceived()
        => _implementation.RecordMessageReceived();

    /// <summary>
    /// Records message sent.
    /// </summary>
    public void RecordMessageSent()
        => _implementation.RecordMessageSent();

    /// <summary>
    /// Records latency.
    /// </summary>
    public void RecordLatency(double latencyMs)
        => _implementation.RecordLatency(latencyMs);

    /// <summary>
    /// Records reconnection.
    /// </summary>
    public void RecordReconnection()
        => _implementation.RecordReconnection();

    /// <summary>
    /// Records connection established.
    /// </summary>
    public void RecordConnectionEstablished()
        => _implementation.RecordConnectionEstablished();

    /// <summary>
    /// Records error.
    /// </summary>
    public void RecordError(Exception error)
        => _implementation.RecordError(error);

    /// <summary>
    /// Gets average latency.
    /// </summary>
    public double? GetAverageLatency()
        => _implementation.GetAverageLatency();

    /// <summary>
    /// Gets messages received.
    /// </summary>
    public long GetMessagesReceived()
        => _implementation.GetMessagesReceived();

    /// <summary>
    /// Gets messages sent.
    /// </summary>
    public long GetMessagesSent()
        => _implementation.GetMessagesSent();

    /// <summary>
    /// Gets reconnection count.
    /// </summary>
    public int GetReconnectionCount()
        => _implementation.GetReconnectionCount();

    /// <summary>
    /// Gets time since last message.
    /// </summary>
    public TimeSpan? GetTimeSinceLastMessage()
        => _implementation.GetTimeSinceLastMessage();

    /// <summary>
    /// Gets connection uptime.
    /// </summary>
    public TimeSpan? GetConnectionUptime()
        => _implementation.GetConnectionUptime();

    /// <summary>
    /// Gets recent errors.
    /// </summary>
    public IReadOnlyList<Exception> GetRecentErrors()
        => _implementation.GetRecentErrors();

    /// <summary>
    /// Executes the reset operation.
    /// </summary>
    public void Reset()
        => _implementation.Reset();

    /// <summary>
    /// Creates health check.
    /// </summary>
    public ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions)
        => _implementation.CreateHealthCheck(connectionState, activeSubscriptions);
}
