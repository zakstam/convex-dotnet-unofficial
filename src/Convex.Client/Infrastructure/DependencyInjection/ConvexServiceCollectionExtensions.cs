using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Convex.Client.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Convex client services.
/// </summary>
public static class ConvexServiceCollectionExtensions
{
    /// <summary>
    /// Adds Convex client services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddConvexClient(
        this IServiceCollection services,
        string deploymentUrl)
    {
        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or empty.", nameof(deploymentUrl));
        }

        return AddConvexClient(services, options => options.DeploymentUrl = deploymentUrl);
    }

    /// <summary>
    /// Adds Convex client services to the specified <see cref="IServiceCollection"/> with configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">A delegate to configure the Convex client options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddConvexClient(
        this IServiceCollection services,
        Action<ConvexClientOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new ConvexClientOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.DeploymentUrl))
        {
            throw new InvalidOperationException("DeploymentUrl must be configured.");
        }

        // Register v2 ConvexClient (unified HTTP + WebSocket)
        services.TryAddSingleton<IConvexClient>(sp =>
        {
            return new ConvexClient(options.DeploymentUrl);
        });

        return services;
    }

}

/// <summary>
/// Configuration options for Convex client.
/// </summary>
public class ConvexClientOptions
{
    /// <summary>
    /// Gets or sets the Convex deployment URL (HTTPS).
    /// The WebSocket URL for real-time features is automatically derived from this.
    /// </summary>
    public string DeploymentUrl { get; set; } = string.Empty;
}
