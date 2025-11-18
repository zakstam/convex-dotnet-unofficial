using Convex.Client.Shared.Connection;

namespace Convex.Client.Slices.Health;

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
    /// Gets the average latency from recent samples.
    /// Returns null if no samples are available.
    /// </summary>
    double? GetAverageLatency();

    /// <summary>
    /// Gets the number of messages received.
    /// </summary>
    long GetMessagesReceived();

    /// <summary>
    /// Gets the number of messages sent.
    /// </summary>
    long GetMessagesSent();

    /// <summary>
    /// Gets the reconnection count.
    /// </summary>
    int GetReconnectionCount();

    /// <summary>
    /// Gets the time since the last message was received.
    /// Returns null if no messages have been received yet.
    /// </summary>
    TimeSpan? GetTimeSinceLastMessage();

    /// <summary>
    /// Gets the time since the connection was established.
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
    /// Creates a health check based on current metrics.
    /// </summary>
    /// <param name="connectionState">The current connection state.</param>
    /// <param name="activeSubscriptions">The number of active subscriptions.</param>
    /// <returns>A health check result.</returns>
    ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions);
}

/// <summary>
/// Represents the overall health status of a Convex client connection.
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
/// Creates a new health check result.
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
    /// Gets the overall health status of the connection.
    /// </summary>
    public ConvexHealthStatus Status { get; } = status;

    /// <summary>
    /// Gets a human-readable description of the current health status.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the timestamp when this health check was performed.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState ConnectionState { get; } = connectionState;

    /// <summary>
    /// Gets the average latency in milliseconds over the recent measurement window.
    /// Returns null if insufficient data is available.
    /// </summary>
    public double? AverageLatencyMs { get; } = averageLatencyMs;

    /// <summary>
    /// Gets the number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; } = activeSubscriptions;

    /// <summary>
    /// Gets the number of times the connection has been re-established.
    /// </summary>
    public int ReconnectionCount { get; } = reconnectionCount;

    /// <summary>
    /// Gets the total number of messages received from the server.
    /// </summary>
    public long MessagesReceived { get; } = messagesReceived;

    /// <summary>
    /// Gets the total number of messages sent to the server.
    /// </summary>
    public long MessagesSent { get; } = messagesSent;

    /// <summary>
    /// Gets the time elapsed since the last successful message was received.
    /// Returns null if no messages have been received yet.
    /// </summary>
    public TimeSpan? TimeSinceLastMessage { get; } = timeSinceLastMessage;

    /// <summary>
    /// Gets any errors that have occurred recently.
    /// Returns an empty collection if no recent errors.
    /// </summary>
    public IReadOnlyList<Exception> RecentErrors { get; } = recentErrors ?? [];

    /// <summary>
    /// Gets additional diagnostic data as key-value pairs.
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
