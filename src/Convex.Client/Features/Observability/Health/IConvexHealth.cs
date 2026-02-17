using Convex.Client.Infrastructure.Connection;

namespace Convex.Client.Features.Observability.Health;

/// <summary>
/// Monitors the health of a Convex client connection by tracking metrics over time.
/// Thread-safe for concurrent metric recording and health checks.
/// </summary>
public interface IConvexHealth
{
    /// <summary>
    /// Records that a message was received.
    /// </summary>
    void RecordMessageReceived();

    /// <summary>
    /// Records that a message was sent.
    /// </summary>
    void RecordMessageSent();

    /// <summary>
    /// Records a latency measurement in milliseconds.
    /// </summary>
    /// <param name="latencyMs">The latency to record.</param>
    void RecordLatency(double latencyMs);

    /// <summary>
    /// Records that a reconnection occurred.
    /// </summary>
    void RecordReconnection();

    /// <summary>
    /// Records that the connection was established.
    /// </summary>
    void RecordConnectionEstablished();

    /// <summary>
    /// Records an error that occurred.
    /// </summary>
    /// <param name="error">The error to record.</param>
    void RecordError(Exception error);

    /// <summary>
    /// Gets average latency.
    /// Returns null if no samples are available.
    /// </summary>
    double? GetAverageLatency();

    /// <summary>
    /// Gets messages received.
    /// </summary>
    long GetMessagesReceived();

    /// <summary>
    /// Gets messages sent.
    /// </summary>
    long GetMessagesSent();

    /// <summary>
    /// Gets reconnection count.
    /// </summary>
    int GetReconnectionCount();

    /// <summary>
    /// Gets time since last message.
    /// Returns null if no messages have been received yet.
    /// </summary>
    TimeSpan? GetTimeSinceLastMessage();

    /// <summary>
    /// Gets connection uptime.
    /// Returns null if never connected.
    /// </summary>
    TimeSpan? GetConnectionUptime();

    /// <summary>
    /// Gets recent errors.
    /// </summary>
    IReadOnlyList<Exception> GetRecentErrors();

    /// <summary>
    /// Resets all metrics.
    /// </summary>
    void Reset();

    /// <summary>
    /// Creates health check.
    /// </summary>
    /// <param name="connectionState">The current connection state.</param>
    /// <param name="activeSubscriptions">The number of active subscriptions.</param>
    /// <returns>A health check result.</returns>
    ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions);
}

/// <summary>
/// Defines the convex health status values.
/// </summary>
public enum ConvexHealthStatus
{
    /// <summary>
    /// The connection is healthy and operating normally.
    /// All metrics are within acceptable ranges.
    /// </summary>
    Healthy,

    /// <summary>
    /// The connection is degraded but still functional.
    /// Some metrics are outside optimal ranges but service is available.
    /// </summary>
    Degraded,

    /// <summary>
    /// The connection is unhealthy and may not be functioning properly.
    /// Critical metrics are outside acceptable ranges.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// The health status cannot be determined.
    /// This typically occurs when insufficient data is available.
    /// </summary>
    Unknown
}

/// <summary>
/// Provides health check information for a Convex client connection.
/// Includes connection status, performance metrics, and diagnostic data.
/// </summary>
/// <remarks>
/// Executes the convex health check operation.
/// </remarks>
public sealed class ConvexHealthCheck(
    ConvexHealthStatus status,
    string description,
    ConnectionState connectionState,
    double? averageLatencyMs = null,
    int activeSubscriptions = 0,
    int reconnectionCount = 0,
    long messagesReceived = 0,
    long messagesSent = 0,
    TimeSpan? timeSinceLastMessage = null,
    IReadOnlyList<Exception>? recentErrors = null,
    IReadOnlyDictionary<string, object>? additionalData = null)
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    public ConvexHealthStatus Status { get; } = status;

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the connection state.
    /// </summary>
    public ConnectionState ConnectionState { get; } = connectionState;

    /// <summary>
    /// Gets the average latency ms.
    /// Returns null if insufficient data is available.
    /// </summary>
    public double? AverageLatencyMs { get; } = averageLatencyMs;

    /// <summary>
    /// Gets the active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; } = activeSubscriptions;

    /// <summary>
    /// Gets the reconnection count.
    /// </summary>
    public int ReconnectionCount { get; } = reconnectionCount;

    /// <summary>
    /// Gets the messages received.
    /// </summary>
    public long MessagesReceived { get; } = messagesReceived;

    /// <summary>
    /// Gets the messages sent.
    /// </summary>
    public long MessagesSent { get; } = messagesSent;

    /// <summary>
    /// Gets the time since last message.
    /// Returns null if no messages have been received yet.
    /// </summary>
    public TimeSpan? TimeSinceLastMessage { get; } = timeSinceLastMessage;

    /// <summary>
    /// Gets the recent errors.
    /// Returns an empty collection if no recent errors.
    /// </summary>
    public IReadOnlyList<Exception> RecentErrors { get; } = recentErrors ?? [];

    /// <summary>
    /// Gets the dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalData { get; } = additionalData ?? new Dictionary<string, object>();

    /// <summary>
    /// Returns a formatted string representation of the health check.
    /// </summary>
    public override string ToString()
    {
        return $"Health: {Status} - {Description} " +
               $"(ConnectionState: {ConnectionState}, " +
               $"Latency: {AverageLatencyMs?.ToString("F2") ?? "N/A"}ms, " +
               $"Subscriptions: {ActiveSubscriptions}, " +
               $"Reconnections: {ReconnectionCount})";
    }
}
