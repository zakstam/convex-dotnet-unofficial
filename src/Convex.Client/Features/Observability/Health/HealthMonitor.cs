using System.Collections.Concurrent;
using Convex.Client.Infrastructure.Connection;
using Microsoft.Extensions.Logging;
using Convex.Client.Infrastructure.Telemetry;

namespace Convex.Client.Features.Observability.Health;

/// <summary>
/// Internal implementation of health monitoring.
/// Tracks connection metrics and determines health status based on configurable thresholds.
/// </summary>
internal sealed class HealthMonitor(ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly object _metricsLock = new();
    private readonly ConcurrentQueue<double> _latencySamples = new();
    private readonly ConcurrentQueue<Exception> _recentErrors = new();
    private readonly int _maxLatencySamples = 100;
    private readonly int _maxRecentErrors = 10;
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private long _messagesReceived;
    private long _messagesSent;
    private int _reconnectionCount;
    private DateTimeOffset? _lastMessageReceivedAt;
    private DateTimeOffset? _connectionEstablishedAt;

    public void RecordMessageReceived()
    {
        _ = Interlocked.Increment(ref _messagesReceived);
        _lastMessageReceivedAt = DateTimeOffset.UtcNow;
    }

    public void RecordMessageSent() => _ = Interlocked.Increment(ref _messagesSent);

    public void RecordLatency(double latencyMs)
    {
        _latencySamples.Enqueue(latencyMs);

        // Keep only the most recent samples
        while (_latencySamples.Count > _maxLatencySamples)
        {
            _ = _latencySamples.TryDequeue(out _);
        }
    }

    public void RecordReconnection()
    {
        _ = Interlocked.Increment(ref _reconnectionCount);
        _connectionEstablishedAt = DateTimeOffset.UtcNow;
    }

    public void RecordConnectionEstablished() => _connectionEstablishedAt = DateTimeOffset.UtcNow;

    public void RecordError(Exception error)
    {
        _recentErrors.Enqueue(error);

        // Keep only the most recent errors
        while (_recentErrors.Count > _maxRecentErrors)
        {
            _ = _recentErrors.TryDequeue(out _);
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Health] Error recorded: ErrorType={ErrorType}, RecentErrorCount={Count}",
                error.GetType().Name, _recentErrors.Count);
        }
    }

    public double? GetAverageLatency()
    {
        var samples = _latencySamples.ToArray();
        return samples.Length > 0 ? samples.Average() : null;
    }

    public long GetMessagesReceived() => Interlocked.Read(ref _messagesReceived);

    public long GetMessagesSent() => Interlocked.Read(ref _messagesSent);

    public int GetReconnectionCount() => _reconnectionCount;

    public TimeSpan? GetTimeSinceLastMessage()
    {
        var lastMessage = _lastMessageReceivedAt;
        return lastMessage.HasValue ? DateTimeOffset.UtcNow - lastMessage.Value : null;
    }

    public TimeSpan? GetConnectionUptime()
    {
        var connectedAt = _connectionEstablishedAt;
        return connectedAt.HasValue ? DateTimeOffset.UtcNow - connectedAt.Value : null;
    }

    public IReadOnlyList<Exception> GetRecentErrors() => [.. _recentErrors];

    public void Reset()
    {
        lock (_metricsLock)
        {
            _latencySamples.Clear();
            _recentErrors.Clear();
            _messagesReceived = 0;
            _messagesSent = 0;
            _reconnectionCount = 0;
            _lastMessageReceivedAt = null;
            _connectionEstablishedAt = null;
        }
    }

    public ConvexHealthCheck CreateHealthCheck(ConnectionState connectionState, int activeSubscriptions)
    {
        var builder = new HealthCheckBuilder()
            .WithConnectionState(connectionState)
            .WithActiveSubscriptions(activeSubscriptions)
            .WithAverageLatency(GetAverageLatency())
            .WithMessagesReceived(GetMessagesReceived())
            .WithMessagesSent(GetMessagesSent())
            .WithReconnectionCount(GetReconnectionCount())
            .WithTimeSinceLastMessage(GetTimeSinceLastMessage());

        // Add recent errors
        foreach (var error in GetRecentErrors())
        {
            _ = builder.AddError(error);
        }

        // Add additional diagnostics
        var uptime = GetConnectionUptime();
        if (uptime.HasValue)
        {
            _ = builder.AddData("ConnectionUptime", uptime.Value);
        }

        // Determine health status
        var (status, description) = DetermineHealthStatus(connectionState, activeSubscriptions);
        _ = builder.WithStatus(status).WithDescription(description);

        // Log health check result
        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug(
                "[Health] Check created: Status={Status}, State={ConnectionState}, ActiveSubs={ActiveSubscriptions}, AvgLatency={AvgLatency}ms, Errors={ErrorCount}",
                status, connectionState, activeSubscriptions, GetAverageLatency() ?? 0, _recentErrors.Count);
        }

        // Always log non-healthy states (not just debug)
        if (status == ConvexHealthStatus.Unhealthy)
        {
            _logger?.LogWarning("[Health] Status UNHEALTHY: Description={Description}", description);
        }
        else if (status == ConvexHealthStatus.Degraded)
        {
            _logger?.LogWarning("[Health] Status DEGRADED: Description={Description}", description);
        }

        return builder.Build();
    }

    private (ConvexHealthStatus status, string description) DetermineHealthStatus(
        ConnectionState connectionState,
        int activeSubscriptions)
    {
        // Check connection state first
        if (connectionState == ConnectionState.Disconnected)
        {
            return (ConvexHealthStatus.Unhealthy, "Connection is disconnected");
        }

        if (connectionState == ConnectionState.Connecting || connectionState == ConnectionState.Reconnecting)
        {
            return (ConvexHealthStatus.Degraded, $"Connection is {connectionState.ToString().ToLowerInvariant()}");
        }

        // Check for recent errors
        var recentErrorCount = _recentErrors.Count;
        if (recentErrorCount >= 5)
        {
            return (ConvexHealthStatus.Degraded, $"Multiple recent errors ({recentErrorCount})");
        }

        // Check latency
        var avgLatency = GetAverageLatency();
        if (avgLatency.HasValue)
        {
            if (avgLatency.Value > 1000) // > 1 second
            {
                return (ConvexHealthStatus.Degraded, $"High latency ({avgLatency.Value:F0}ms)");
            }
            if (avgLatency.Value > 500) // > 500ms
            {
                return (ConvexHealthStatus.Degraded, $"Elevated latency ({avgLatency.Value:F0}ms)");
            }
        }

        // Check for stale connection (no messages in a while)
        var timeSinceLastMessage = GetTimeSinceLastMessage();
        if (timeSinceLastMessage.HasValue && timeSinceLastMessage.Value > TimeSpan.FromMinutes(5))
        {
            return (ConvexHealthStatus.Degraded, $"No messages received in {timeSinceLastMessage.Value.TotalMinutes:F1} minutes");
        }

        // Check reconnection count
        if (_reconnectionCount > 10)
        {
            return (ConvexHealthStatus.Degraded, $"Frequent reconnections ({_reconnectionCount} times)");
        }

        // All checks passed
        var latencyInfo = avgLatency.HasValue ? $" (latency: {avgLatency.Value:F0}ms)" : "";
        return (ConvexHealthStatus.Healthy, $"Connection is healthy{latencyInfo}");
    }
}

/// <summary>
/// Builder for creating health check results with a fluent API.
/// </summary>
internal sealed class HealthCheckBuilder
{
    private ConvexHealthStatus _status = ConvexHealthStatus.Unknown;
    private string _description = "Health status unknown";
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private double? _averageLatencyMs;
    private int _activeSubscriptions;
    private int _reconnectionCount;
    private long _messagesReceived;
    private long _messagesSent;
    private TimeSpan? _timeSinceLastMessage;
    private List<Exception>? _recentErrors;
    private Dictionary<string, object>? _additionalData;

    public HealthCheckBuilder WithStatus(ConvexHealthStatus status)
    {
        _status = status;
        return this;
    }

    public HealthCheckBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public HealthCheckBuilder WithConnectionState(ConnectionState connectionState)
    {
        _connectionState = connectionState;
        return this;
    }

    public HealthCheckBuilder WithAverageLatency(double? averageLatencyMs)
    {
        _averageLatencyMs = averageLatencyMs;
        return this;
    }

    public HealthCheckBuilder WithActiveSubscriptions(int count)
    {
        _activeSubscriptions = count;
        return this;
    }

    public HealthCheckBuilder WithReconnectionCount(int count)
    {
        _reconnectionCount = count;
        return this;
    }

    public HealthCheckBuilder WithMessagesReceived(long count)
    {
        _messagesReceived = count;
        return this;
    }

    public HealthCheckBuilder WithMessagesSent(long count)
    {
        _messagesSent = count;
        return this;
    }

    public HealthCheckBuilder WithTimeSinceLastMessage(TimeSpan? timeSpan)
    {
        _timeSinceLastMessage = timeSpan;
        return this;
    }

    public HealthCheckBuilder AddError(Exception error)
    {
        _recentErrors ??= [];
        _recentErrors.Add(error);
        return this;
    }

    public HealthCheckBuilder AddData(string key, object value)
    {
        _additionalData ??= [];
        _additionalData[key] = value;
        return this;
    }

    public ConvexHealthCheck Build()
    {
        return new ConvexHealthCheck(
            _status,
            _description,
            _connectionState,
            _averageLatencyMs,
            _activeSubscriptions,
            _reconnectionCount,
            _messagesReceived,
            _messagesSent,
            _timeSinceLastMessage,
            _recentErrors?.AsReadOnly(),
            _additionalData);
    }
}
