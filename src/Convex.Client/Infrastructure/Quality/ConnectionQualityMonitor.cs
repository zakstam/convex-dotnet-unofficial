using System.Collections.Concurrent;

namespace Convex.Client.Infrastructure.Quality;

/// <summary>
/// Monitors connection quality in real-time by tracking latency, errors, reconnections,
/// and connection stability. Provides quality assessments and adaptive behavior recommendations.
/// </summary>
internal sealed class ConnectionQualityMonitor
{
    private const int MaxLatencySamples = 100;
    private const int MaxErrorSamples = 20;
    private const int MonitoringWindowMinutes = 5;

    private readonly ConcurrentQueue<LatencySample> _latencySamples = new();
    private readonly ConcurrentQueue<ErrorSample> _errorSamples = new();
    private readonly ConcurrentQueue<ConnectionEvent> _connectionEvents = new();

    private readonly object _lock = new();

    private ConnectionQuality _currentQuality = ConnectionQuality.Unknown;
    private DateTimeOffset? _lastMessageTime;
    private DateTimeOffset _monitorStartTime = DateTimeOffset.UtcNow;
    private long _totalMessagesReceived;
    private long _totalMessagesSent;
    private int _totalReconnections;
    private TimeSpan _totalConnectedTime;
    private TimeSpan _totalDisconnectedTime;
    private DateTimeOffset? _lastConnectionTime;

    /// <summary>
    /// Gets the current connection quality.
    /// </summary>
    public ConnectionQuality CurrentQuality
    {
        get
        {
            lock (_lock)
            {
                return _currentQuality;
            }
        }
    }

    /// <summary>
    /// Records a latency measurement in milliseconds.
    /// </summary>
    public void RecordLatency(double latencyMs)
    {
        var sample = new LatencySample(DateTimeOffset.UtcNow, latencyMs);
        _latencySamples.Enqueue(sample);

        // Keep only recent samples
        while (_latencySamples.Count > MaxLatencySamples)
        {
            _ = _latencySamples.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Records that a message was received.
    /// </summary>
    public void RecordMessageReceived()
    {
        lock (_lock)
        {
            _lastMessageTime = DateTimeOffset.UtcNow;
            _totalMessagesReceived++;
        }
    }

    /// <summary>
    /// Records that a message was sent.
    /// </summary>
    public void RecordMessageSent()
    {
        lock (_lock)
        {
            _totalMessagesSent++;
        }
    }

    /// <summary>
    /// Records an error that occurred.
    /// </summary>
    public void RecordError(Exception error)
    {
        var sample = new ErrorSample(DateTimeOffset.UtcNow, error);
        _errorSamples.Enqueue(sample);

        // Keep only recent errors
        while (_errorSamples.Count > MaxErrorSamples)
        {
            _ = _errorSamples.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Records a reconnection event.
    /// </summary>
    public void RecordReconnection()
    {
        lock (_lock)
        {
            _totalReconnections++;
        }

        var evt = new ConnectionEvent(DateTimeOffset.UtcNow, ConnectionEventType.Reconnected);
        _connectionEvents.Enqueue(evt);

        // Keep only recent events
        while (_connectionEvents.Count > 100)
        {
            _ = _connectionEvents.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Records that the connection was established.
    /// </summary>
    public void RecordConnected()
    {
        lock (_lock)
        {
            _lastConnectionTime = DateTimeOffset.UtcNow;
        }

        var evt = new ConnectionEvent(DateTimeOffset.UtcNow, ConnectionEventType.Connected);
        _connectionEvents.Enqueue(evt);
    }

    /// <summary>
    /// Records that the connection was lost.
    /// </summary>
    public void RecordDisconnected()
    {
        lock (_lock)
        {
            if (_lastConnectionTime.HasValue)
            {
                var connectedDuration = DateTimeOffset.UtcNow - _lastConnectionTime.Value;
                _totalConnectedTime += connectedDuration;
                _lastConnectionTime = null;
            }
        }

        var evt = new ConnectionEvent(DateTimeOffset.UtcNow, ConnectionEventType.Disconnected);
        _connectionEvents.Enqueue(evt);
    }

    /// <summary>
    /// Creates a comprehensive quality assessment based on recent metrics.
    /// </summary>
    public ConnectionQualityInfo AssessQuality(bool isConnected)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var cutoffTime = now.AddMinutes(-MonitoringWindowMinutes);

            // Calculate latency metrics
            var recentLatencies = _latencySamples
                .Where(s => s.Timestamp > cutoffTime)
                .Select(s => s.LatencyMs)
                .ToList();

            double? avgLatency = null;
            double? latencyVariance = null;

            if (recentLatencies.Count > 0)
            {
                avgLatency = recentLatencies.Average();

                if (recentLatencies.Count > 1)
                {
                    var mean = avgLatency.Value;
                    var sumOfSquares = recentLatencies.Sum(l => Math.Pow(l - mean, 2));
                    latencyVariance = Math.Sqrt(sumOfSquares / recentLatencies.Count);
                }
            }

            // Calculate error metrics
            var recentErrors = _errorSamples
                .Where(s => s.Timestamp > cutoffTime)
                .ToList();

            var errorCount = recentErrors.Count;

            // Calculate reconnection metrics in the window
            var recentReconnections = _connectionEvents
                .Where(e => e.Timestamp > cutoffTime && e.Type == ConnectionEventType.Reconnected)
                .Count();

            // Estimate packet loss based on errors and reconnections
            double? packetLossRate = null;
            var totalMessages = _totalMessagesReceived + _totalMessagesSent;
            if (totalMessages > 10)
            {
                var estimatedLostMessages = errorCount + (recentReconnections * 2);
                packetLossRate = Math.Min(100.0, (estimatedLostMessages / (double)totalMessages) * 100.0);
            }

            // Calculate uptime
            var monitoringDuration = now - _monitorStartTime;
            var currentConnectionDuration = _lastConnectionTime.HasValue
                ? now - _lastConnectionTime.Value
                : TimeSpan.Zero;

            var totalConnected = _totalConnectedTime + currentConnectionDuration;
            var uptimePercentage = monitoringDuration.TotalSeconds > 0
                ? Math.Min(100.0, (totalConnected.TotalSeconds / monitoringDuration.TotalSeconds) * 100.0)
                : 100.0;

            // Calculate time since last message
            TimeSpan? timeSinceLastMessage = _lastMessageTime.HasValue
                ? now - _lastMessageTime.Value
                : null;

            // Determine quality and score
            var (quality, description, score) = DetermineQuality(
                isConnected,
                avgLatency,
                latencyVariance,
                packetLossRate,
                recentReconnections,
                errorCount,
                uptimePercentage,
                timeSinceLastMessage);

            // Update current quality
            _currentQuality = quality;

            // Build additional data
            var additionalData = new Dictionary<string, object>
            {
                ["TotalMessagesReceived"] = _totalMessagesReceived,
                ["TotalMessagesSent"] = _totalMessagesSent,
                ["TotalReconnections"] = _totalReconnections,
                ["MonitoringDuration"] = monitoringDuration,
                ["LatencySampleCount"] = recentLatencies.Count
            };

            return new ConnectionQualityInfo(
                quality: quality,
                description: description,
                averageLatencyMs: avgLatency,
                latencyVarianceMs: latencyVariance,
                packetLossRate: packetLossRate,
                reconnectionCount: recentReconnections,
                errorCount: errorCount,
                timeSinceLastMessage: timeSinceLastMessage,
                uptimePercentage: uptimePercentage,
                qualityScore: score,
                additionalData: additionalData);
        }
    }

    /// <summary>
    /// Determines the connection quality based on multiple factors.
    /// </summary>
    private (ConnectionQuality quality, string description, int score) DetermineQuality(
        bool isConnected,
        double? avgLatency,
        double? latencyVariance,
        double? packetLossRate,
        int reconnections,
        int errors,
        double uptimePercentage,
        TimeSpan? timeSinceLastMessage)
    {
        // Start with perfect score
        var score = 100;

        // If not connected, quality is poor at best
        if (!isConnected)
        {
            return (ConnectionQuality.Poor, "Not connected", 20);
        }

        // Check if we have sufficient data
        if (!avgLatency.HasValue || timeSinceLastMessage == null)
        {
            return (ConnectionQuality.Unknown, "Insufficient data to assess quality", 50);
        }

        // Check for stale connection
        if (timeSinceLastMessage.Value.TotalMinutes > 5)
        {
            return (ConnectionQuality.Poor, "No recent messages (connection may be stale)", 30);
        }

        // Deduct points for latency
        if (avgLatency.Value > 1000)
        {
            score -= 50; // Very high latency
        }
        else if (avgLatency.Value > 500)
        {
            score -= 30; // High latency
        }
        else if (avgLatency.Value > 300)
        {
            score -= 15; // Moderate latency
        }
        else if (avgLatency.Value > 100)
        {
            score -= 5; // Slightly elevated latency
        }

        // Deduct points for latency variance (instability)
        if (latencyVariance.HasValue)
        {
            if (latencyVariance.Value > 200)
            {
                score -= 20; // Very unstable
            }
            else if (latencyVariance.Value > 100)
            {
                score -= 10; // Moderately unstable
            }
            else if (latencyVariance.Value > 50)
            {
                score -= 5; // Slightly unstable
            }
        }

        // Deduct points for packet loss
        if (packetLossRate.HasValue)
        {
            if (packetLossRate.Value > 10)
            {
                score -= 30; // Severe packet loss
            }
            else if (packetLossRate.Value > 5)
            {
                score -= 20; // Significant packet loss
            }
            else if (packetLossRate.Value > 1)
            {
                score -= 10; // Some packet loss
            }
        }

        // Deduct points for reconnections
        if (reconnections > 10)
        {
            score -= 25; // Frequent reconnections
        }
        else if (reconnections > 5)
        {
            score -= 15; // Multiple reconnections
        }
        else if (reconnections > 2)
        {
            score -= 5; // A few reconnections
        }

        // Deduct points for errors
        if (errors > 10)
        {
            score -= 20; // Many errors
        }
        else if (errors > 5)
        {
            score -= 10; // Several errors
        }
        else if (errors > 2)
        {
            score -= 5; // Few errors
        }

        // Deduct points for low uptime
        if (uptimePercentage < 50)
        {
            score -= 30; // Very low uptime
        }
        else if (uptimePercentage < 75)
        {
            score -= 15; // Low uptime
        }
        else if (uptimePercentage < 90)
        {
            score -= 5; // Slightly low uptime
        }

        // Ensure score is in valid range
        score = Math.Max(0, Math.Min(100, score));

        // Map score to quality level
        var (quality, description) = score switch
        {
            >= 85 => (ConnectionQuality.Excellent, "Excellent connection quality - low latency, stable, no issues"),
            >= 70 => (ConnectionQuality.Good, "Good connection quality - acceptable latency, mostly stable"),
            >= 50 => (ConnectionQuality.Fair, "Fair connection quality - higher latency or occasional issues"),
            >= 30 => (ConnectionQuality.Poor, "Poor connection quality - high latency, packet loss, or frequent reconnections"),
            _ => (ConnectionQuality.Terrible, "Terrible connection quality - severe issues affecting reliability")
        };

        return (quality, description, score);
    }

    /// <summary>
    /// Resets all monitoring data.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _latencySamples.Clear();
            _errorSamples.Clear();
            _connectionEvents.Clear();
            _currentQuality = ConnectionQuality.Unknown;
            _lastMessageTime = null;
            _monitorStartTime = DateTimeOffset.UtcNow;
            _totalMessagesReceived = 0;
            _totalMessagesSent = 0;
            _totalReconnections = 0;
            _totalConnectedTime = TimeSpan.Zero;
            _totalDisconnectedTime = TimeSpan.Zero;
            _lastConnectionTime = null;
        }
    }

    private record LatencySample(DateTimeOffset Timestamp, double LatencyMs);
    private record ErrorSample(DateTimeOffset Timestamp, Exception Error);
    private record ConnectionEvent(DateTimeOffset Timestamp, ConnectionEventType Type);

    private enum ConnectionEventType
    {
        Connected,
        Disconnected,
        Reconnected
    }
}
