using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Checks health.
    /// </summary>
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


/// <summary>
/// Options for Convex HTTP client health check.
/// </summary>
public class ConvexHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the health check function name.
    /// If not set, only checks that the client is configured.
    /// </summary>
    public string? HealthCheckFunctionName { get; set; }

    /// <summary>
    /// Gets or sets the health check args.
    /// </summary>
    public object? HealthCheckArgs { get; set; }
}

/// <summary>
/// Extension methods for registering Convex health checks.
/// </summary>
public static class ConvexHealthCheckExtensions
{
    /// <summary>
    /// Adds a health check for the Convex HTTP client.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name.</param>
    /// <param name="options">Optional health check configuration.</param>
    /// <param name="failureStatus">The health status to report on failure.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddConvexHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "convex",
        ConvexHealthCheckOptions? options = null,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.Add(new HealthCheckRegistration(
            name,
            serviceProvider => new ConvexHealthCheck(
                serviceProvider.GetRequiredService<IConvexClient>(),
                serviceProvider.GetRequiredService<ILogger<ConvexHealthCheck>>(),
                options),
            failureStatus,
            tags));
    }
}

