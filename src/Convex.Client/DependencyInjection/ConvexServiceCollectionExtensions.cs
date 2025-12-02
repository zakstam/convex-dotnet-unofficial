using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Convex.Client.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Convex client services to an <see cref="IServiceCollection"/>.
/// </summary>
public static class ConvexServiceCollectionExtensions
{
    /// <summary>
    /// Adds Convex client services to the service collection with the specified configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An action to configure the <see cref="ConvexOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConvex(
        this IServiceCollection services,
        Action<ConvexOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        _ = services.AddOptions<ConvexOptions>()
            .Configure(configureOptions)
            .ValidateOnStart();

        return services.AddConvexCore();
    }

    /// <summary>
    /// Adds Convex client services to the service collection with configuration from an <see cref="IConfiguration"/> section.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConvex(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _ = services.AddOptions<ConvexOptions>()
            .Bind(configuration)
            .ValidateOnStart();

        return services.AddConvexCore();
    }

    /// <summary>
    /// Adds a named Convex client to the service collection.
    /// Use <see cref="IConvexClientFactory"/> to retrieve named clients.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="name">The logical name of the client to configure.</param>
    /// <param name="configureOptions">An action to configure the <see cref="ConvexOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConvex(
        this IServiceCollection services,
        string name,
        Action<ConvexOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        _ = services.AddOptions<ConvexOptions>(name)
            .Configure(configureOptions)
            .ValidateOnStart();

        return services.AddConvexCore();
    }

    /// <summary>
    /// Adds a named Convex client to the service collection with configuration from an <see cref="IConfiguration"/> section.
    /// Use <see cref="IConvexClientFactory"/> to retrieve named clients.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="name">The logical name of the client to configure.</param>
    /// <param name="configuration">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConvex(
        this IServiceCollection services,
        string name,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _ = services.AddOptions<ConvexOptions>(name)
            .Bind(configuration)
            .ValidateOnStart();

        return services.AddConvexCore();
    }

    /// <summary>
    /// Adds core Convex services to the service collection.
    /// </summary>
    private static IServiceCollection AddConvexCore(this IServiceCollection services)
    {
        // Register the factory as a singleton
        services.TryAddSingleton<IConvexClientFactory, ConvexClientFactory>();

        // Register IConvexClient as a transient service that resolves the default client
        services.TryAddTransient<IConvexClient>(sp =>
        {
            var factory = sp.GetRequiredService<IConvexClientFactory>();
            return factory.CreateClient();
        });

        return services;
    }
}
