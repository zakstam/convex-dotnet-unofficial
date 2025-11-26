namespace Convex.Client.Infrastructure.Quality;

/// <summary>
/// Represents the quality level of the connection to the Convex backend.
/// </summary>
public enum ConnectionQuality
{
    /// <summary>
    /// Connection quality cannot be determined (insufficient data).
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Excellent connection quality.
    /// Low latency (&lt;100ms), no packet loss, stable connection.
    /// </summary>
    Excellent = 1,

    /// <summary>
    /// Good connection quality.
    /// Moderate latency (100-300ms), minimal packet loss, stable connection.
    /// </summary>
    Good = 2,

    /// <summary>
    /// Fair connection quality.
    /// Higher latency (300-500ms), some packet loss, occasional issues.
    /// </summary>
    Fair = 3,

    /// <summary>
    /// Poor connection quality.
    /// High latency (500-1000ms), significant packet loss, frequent reconnections.
    /// </summary>
    Poor = 4,

    /// <summary>
    /// Terrible connection quality.
    /// Very high latency (&gt;1000ms), severe packet loss, constant reconnections.
    /// </summary>
    Terrible = 5
}

/// <summary>
/// Provides detailed information about the current connection quality.
/// </summary>
/// <remarks>
/// Creates a new instance of ConnectionQualityInfo.
/// </remarks>
public sealed class ConnectionQualityInfo(
    ConnectionQuality quality,
    string description,
    double? averageLatencyMs = null,
    double? latencyVarianceMs = null,
    double? packetLossRate = null,
    int reconnectionCount = 0,
    int errorCount = 0,
    TimeSpan? timeSinceLastMessage = null,
    double uptimePercentage = 100.0,
    int qualityScore = 100,
    IReadOnlyDictionary<string, object>? additionalData = null)
{
    /// <summary>
    /// Gets the current connection quality level.
    /// </summary>
    public ConnectionQuality Quality { get; } = quality;

    /// <summary>
    /// Gets a human-readable description of the quality.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the timestamp when this quality assessment was made.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the average latency in milliseconds over recent samples.
    /// Null if insufficient data.
    /// </summary>
    public double? AverageLatencyMs { get; } = averageLatencyMs;

    /// <summary>
    /// Gets the latency variance (standard deviation) in milliseconds.
    /// Higher values indicate unstable connection.
    /// Null if insufficient data.
    /// </summary>
    public double? LatencyVarianceMs { get; } = latencyVarianceMs;

    /// <summary>
    /// Gets the packet loss rate as a percentage (0-100).
    /// Estimated from failed messages and reconnections.
    /// Null if insufficient data.
    /// </summary>
    public double? PacketLossRate { get; } = packetLossRate;

    /// <summary>
    /// Gets the number of reconnections in the monitoring window.
    /// </summary>
    public int ReconnectionCount { get; } = reconnectionCount;

    /// <summary>
    /// Gets the total number of errors in the monitoring window.
    /// </summary>
    public int ErrorCount { get; } = errorCount;

    /// <summary>
    /// Gets the time since the last successful message.
    /// Null if no messages have been received.
    /// </summary>
    public TimeSpan? TimeSinceLastMessage { get; } = timeSinceLastMessage;

    /// <summary>
    /// Gets the connection uptime percentage (0-100) over the monitoring window.
    /// 100% means always connected, 0% means always disconnected.
    /// </summary>
    public double UptimePercentage { get; } = uptimePercentage;

    /// <summary>
    /// Gets the quality score (0-100) where higher is better.
    /// Combines latency, packet loss, reconnections, and errors into a single metric.
    /// </summary>
    public int QualityScore { get; } = qualityScore;

    /// <summary>
    /// Gets additional diagnostic data for advanced scenarios.
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalData { get; } = additionalData ?? new Dictionary<string, object>();

    /// <summary>
    /// Returns a detailed string representation of the connection quality.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"Quality: {Quality}",
            $"Score: {QualityScore}/100",
            $"Description: {Description}"
        };

        if (AverageLatencyMs.HasValue)
        {
            parts.Add($"Avg Latency: {AverageLatencyMs.Value:F2}ms");
        }

        if (LatencyVarianceMs.HasValue)
        {
            parts.Add($"Latency Variance: {LatencyVarianceMs.Value:F2}ms");
        }

        if (PacketLossRate.HasValue)
        {
            parts.Add($"Packet Loss: {PacketLossRate.Value:F2}%");
        }

        parts.Add($"Uptime: {UptimePercentage:F1}%");
        parts.Add($"Reconnections: {ReconnectionCount}");
        parts.Add($"Errors: {ErrorCount}");

        return string.Join(", ", parts);
    }
}
