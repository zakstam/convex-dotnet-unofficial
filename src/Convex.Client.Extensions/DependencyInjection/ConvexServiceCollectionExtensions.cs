using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Convex.Client.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring Convex client services (v2 API).
/// </summary>
public static class ConvexServiceCollectionExtensions
{
    /// <summary>
    /// Adds Convex client services (v2 unified HTTP + WebSocket) to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvex(this IServiceCollection services, string deploymentUrl)
    {
        if (string.IsNullOrEmpty(deploymentUrl))
        {

            throw new ArgumentException("Deployment URL cannot be null or empty", nameof(deploymentUrl));
        }


        services.TryAddSingleton<IConvexClient>(sp =>
        {
            return new ConvexClient(deploymentUrl);
        });

        return services;
    }

    /// <summary>
    /// Adds Convex HTTP client services using configuration.
    /// Binds options from appsettings.json using Microsoft.Extensions.Configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The configuration section name (default: "Convex").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // appsettings.json:
    /// {
    ///   "Convex": {
    ///     "DeploymentUrl": "https://your-app.convex.cloud",
    ///     "DefaultTimeout": "00:00:30",
    ///     "AdminKey": "your-admin-key"
    ///   }
    /// }
    ///
    /// // Program.cs:
    /// builder.Services.AddConvex(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddConvex(this IServiceCollection services, IConfiguration configuration, string sectionName = "Convex")
    {
        var options = configuration.GetSection(sectionName).Get<ConvexClientOptions>()
            ?? throw new InvalidOperationException($"Convex configuration section '{sectionName}' not found");

        return string.IsNullOrEmpty(options.DeploymentUrl)
            ? throw new InvalidOperationException("Convex DeploymentUrl is required in configuration")
            : services.AddConvex(options);
    }

    /// <summary>
    /// Adds Convex client services using options (v2 API).
    /// Provides full control over all ConvexClient configuration options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Convex client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvex(this IServiceCollection services, ConvexClientOptions options)
    {
        if (options == null)
        {

            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrEmpty(options.DeploymentUrl))
        {
            throw new InvalidOperationException("DeploymentUrl is required");
        }


        services.TryAddSingleton(options);

        services.TryAddSingleton<IConvexClient>(sp =>
        {
            var client = new ConvexClient(options.DeploymentUrl, options);

            // TODO: Restore when authentication slice migration is complete
            // Apply authentication if configured
            // if (!string.IsNullOrEmpty(options.AdminKey))
            // {
            //     client.SetAdminAuth(options.AdminKey);
            // }

            return client;
        });

        return services;
    }

    /// <summary>
    /// Adds Convex HTTP client services with configuration callback.
    /// Allows fluent configuration of all ConvexClient options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvex(options =>
    /// {
    ///     options.DeploymentUrl = "https://your-app.convex.cloud";
    ///     options.DefaultTimeout = TimeSpan.FromSeconds(30);
    ///     options.AutoConnect = true;
    ///     options.EnableQualityMonitoring = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddConvex(this IServiceCollection services, Action<ConvexClientOptions> configureOptions)
    {
        var options = new ConvexClientOptions();
        configureOptions(options);
        return services.AddConvex(options);
    }

#if FALSE // TODO: Re-implement AddConvexRealtime methods for V2 API
    /// <summary>
    /// Adds Convex real-time client services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvexRealtime(this IServiceCollection services, string deploymentUrl)
    {
        if (string.IsNullOrEmpty(deploymentUrl))
            throw new ArgumentException("Deployment URL cannot be null or empty", nameof(deploymentUrl));

        services.TryAddSingleton<IConvexRealtimeClient>(sp =>
        {
            var logger = sp.GetService<ILogger<ConvexRealtimeClient>>();
            // Note: ConvexRealtimeClient constructor needs to be updated to accept HttpClient and ILogger
            return new ConvexRealtimeClient(deploymentUrl);
        });

        // Also register as IConvexClient for compatibility
        services.TryAddSingleton<IConvexClient>(sp => sp.GetRequiredService<IConvexRealtimeClient>());

        return services;
    }

    /// <summary>
    /// Adds Convex real-time client services using configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The configuration section name (default: "Convex").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvexRealtime(this IServiceCollection services, IConfiguration configuration, string sectionName = "Convex")
    {
        var options = configuration.GetSection(sectionName).Get<ConvexClientOptions>()
            ?? throw new InvalidOperationException($"Convex configuration section '{sectionName}' not found");

        if (string.IsNullOrEmpty(options.DeploymentUrl))
            throw new InvalidOperationException("Convex DeploymentUrl is required in configuration");

        return services.AddConvexRealtime(options);
    }

    /// <summary>
    /// Adds Convex real-time client services using options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Convex client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvexRealtime(this IServiceCollection services, ConvexClientOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(options.DeploymentUrl))
            throw new InvalidOperationException("DeploymentUrl is required");

        services.TryAddSingleton(options);

        services.TryAddSingleton<IConvexRealtimeClient>(sp =>
        {
            var client = new ConvexRealtimeClient(options.DeploymentUrl);

            // Apply configuration
            if (options.Timeout.HasValue)
            {
                client.Timeout = options.Timeout.Value;
            }

            if (!string.IsNullOrEmpty(options.AdminKey))
            {
                client.SetAdminAuth(options.AdminKey);
            }

            if (options.ReconnectPolicy != null)
            {
                client.ReconnectPolicy = options.ReconnectPolicy;
            }

            return client;
        });

        // Also register as IConvexClient for compatibility
        services.TryAddSingleton<IConvexClient>(sp => sp.GetRequiredService<IConvexRealtimeClient>());

        return services;
    }

    /// <summary>
    /// Adds Convex real-time client services with configuration callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConvexRealtime(this IServiceCollection services, Action<ConvexClientOptions> configureOptions)
    {
        var options = new ConvexClientOptions();
        configureOptions(options);
        return services.AddConvexRealtime(options);
    }
#endif
}
