using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Extensions.HealthChecks;

/// <summary>
/// Health check for Convex HTTP client connectivity.
/// Verifies that the client can communicate with the Convex deployment.
/// </summary>
public class ConvexHealthCheck(
    IConvexClient client,
    ILogger<ConvexHealthCheck> logger,
    ConvexHealthCheckOptions? options = null) : IHealthCheck
{
    private readonly IConvexClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger<ConvexHealthCheck> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConvexHealthCheckOptions _options = options ?? new ConvexHealthCheckOptions();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use custom health check function if provided
            if (!string.IsNullOrEmpty(_options.HealthCheckFunctionName))
            {
                if (_options.HealthCheckArgs != null)
                {
                    _ = await _client.Query<object>(_options.HealthCheckFunctionName)
                        .WithArgs(_options.HealthCheckArgs)
                        .ExecuteAsync(cancellationToken);
                }
                else
                {
                    _ = await _client.Query<object>(_options.HealthCheckFunctionName)
                        .ExecuteAsync(cancellationToken);
                }

                return HealthCheckResult.Healthy("Convex client is responsive");
            }

            // Otherwise just verify client is configured
            if (string.IsNullOrEmpty(_client.DeploymentUrl))
            {
                return HealthCheckResult.Unhealthy("Convex deployment URL not configured");
            }

            return HealthCheckResult.Healthy($"Convex client configured for {_client.DeploymentUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convex health check failed");
            return HealthCheckResult.Unhealthy("Convex client health check failed", ex);
        }
    }
}

#if FALSE // TODO: Re-implement ConvexRealtimeHealthCheck for V2 API
/// <summary>
/// Health check for Convex real-time client connectivity and WebSocket status.
/// </summary>
public class ConvexRealtimeHealthCheck : IHealthCheck
{
    private readonly IConvexRealtimeClient _client;
    private readonly ILogger<ConvexRealtimeHealthCheck> _logger;
    private readonly ConvexRealtimeHealthCheckOptions _options;

    public ConvexRealtimeHealthCheck(
        IConvexRealtimeClient client,
        ILogger<ConvexRealtimeHealthCheck> logger,
        ConvexRealtimeHealthCheckOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new ConvexRealtimeHealthCheckOptions();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Ensure truly async behavior
        await Task.CompletedTask;

        try
        {
            var state = _client.ConnectionState;
            var metrics = _client.Metrics.GetSnapshot();

            // Build health check data
            var data = new Dictionary<string, object>
            {
                { "connectionState", state.ToString() },
                { "totalRequests", metrics.TotalRequests },
                { "inflightRequests", metrics.InflightRequests },
                { "successRate", metrics.SuccessRate }
            };

            // Add optional metric data
            if (_options.IncludeDetailedMetrics)
            {
                data["connectionCount"] = metrics.ConnectionCount;
                data["reconnectionAttempts"] = metrics.ReconnectionAttempts;
                data["reconnectionSuccessRate"] = metrics.ReconnectionSuccessRate;
                data["totalConnectedTime"] = metrics.TotalConnectedTime.TotalSeconds;

                if (metrics.CurrentConnectionDuration.HasValue)
                    data["currentConnectionDuration"] = metrics.CurrentConnectionDuration.Value.TotalSeconds;

                if (metrics.AverageRequestDuration.HasValue)
                    data["avgLatencyMs"] = metrics.AverageRequestDuration.Value.TotalMilliseconds;

                if (metrics.P95RequestDuration.HasValue)
                    data["p95LatencyMs"] = metrics.P95RequestDuration.Value.TotalMilliseconds;
            }

            // Determine health status
            if (state == RealtimeCommunication.Contracts.ConnectionState.Connected)
            {
                // Check metrics thresholds
                if (metrics.SuccessRate < _options.MinSuccessRate)
                {
                    return HealthCheckResult.Degraded(
                        $"Convex success rate ({metrics.SuccessRate:F1}%) below threshold ({_options.MinSuccessRate}%)",
                        data: data);
                }

                if (metrics.P95RequestDuration.HasValue &&
                    metrics.P95RequestDuration.Value > _options.MaxP95Latency)
                {
                    return HealthCheckResult.Degraded(
                        $"Convex P95 latency ({metrics.P95RequestDuration.Value.TotalMilliseconds:F0}ms) exceeds threshold ({_options.MaxP95Latency.TotalMilliseconds}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy("Convex real-time client is connected and healthy", data);
            }
            else if (state == RealtimeCommunication.Contracts.ConnectionState.Connecting ||
                     state == RealtimeCommunication.Contracts.ConnectionState.Reconnecting)
            {
                return HealthCheckResult.Degraded($"Convex real-time client is {state.ToString().ToLower()}", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy($"Convex real-time client is {state.ToString().ToLower()}", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convex real-time health check failed");
            return HealthCheckResult.Unhealthy("Convex real-time client health check failed", ex);
        }
    }
}
#endif

/// <summary>
/// Options for Convex HTTP client health check.
/// </summary>
public class ConvexHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the name of a health check query function to call (optional).
    /// If not set, only checks that the client is configured.
    /// </summary>
    public string? HealthCheckFunctionName { get; set; }

    /// <summary>
    /// Gets or sets arguments to pass to the health check function (optional).
    /// </summary>
    public object? HealthCheckArgs { get; set; }
}

#if FALSE // TODO: Re-implement ConvexRealtimeHealthCheckOptions for V2 API
/// <summary>
/// Options for Convex real-time client health check.
/// </summary>
public class ConvexRealtimeHealthCheckOptions
{
    /// <summary>
    /// Gets or sets whether to include detailed metrics in health check data (default: true).
    /// </summary>
    public bool IncludeDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum acceptable success rate percentage (default: 95%).
    /// </summary>
    public double MinSuccessRate { get; set; } = 95.0;

    /// <summary>
    /// Gets or sets the maximum acceptable P95 latency (default: 5 seconds).
    /// </summary>
    public TimeSpan MaxP95Latency { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Extension methods for adding Convex health checks.
/// </summary>
public static class ConvexHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check for the Convex HTTP client.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name (default: "convex").</param>
    /// <param name="options">Optional health check configuration.</param>
    /// <param name="failureStatus">The health status to report on failure (default: Unhealthy).</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddConvexCheck(
        this IHealthChecksBuilder builder,
        string name = "convex",
        ConvexHealthCheckOptions? options = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new ConvexHealthCheck(
                sp.GetRequiredService<IConvexClient>(),
                sp.GetRequiredService<ILogger<ConvexHealthCheck>>(),
                options),
            failureStatus,
            tags));
    }

    /// <summary>
    /// Adds a health check for the Convex real-time client.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name (default: "convex-realtime").</param>
    /// <param name="options">Optional health check configuration.</param>
    /// <param name="failureStatus">The health status to report on failure (default: Unhealthy).</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddConvexRealtimeCheck(
        this IHealthChecksBuilder builder,
        string name = "convex-realtime",
        ConvexRealtimeHealthCheckOptions? options = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp => new ConvexRealtimeHealthCheck(
                sp.GetRequiredService<IConvexRealtimeClient>(),
                sp.GetRequiredService<ILogger<ConvexRealtimeHealthCheck>>(),
                options),
            failureStatus,
            tags));
    }
}
#endif
