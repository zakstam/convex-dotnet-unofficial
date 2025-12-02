using System;
using System.Linq;
using Convex.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Convex.Client.Extensions.Clerk.Blazor;

/// <summary>
/// Extension methods for adding Clerk authentication to Convex client services in Blazor WebAssembly applications.
/// This package provides automatic JavaScript injection and zero-configuration setup.
/// </summary>
public static class ClerkBlazorServiceCollectionExtensions
{
    /// <summary>
    /// Adds Convex client services with Clerk authentication for Blazor WebAssembly.
    /// Automatically injects required JavaScript code and registers BlazorClerkTokenService.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="clerkConfiguration">The configuration section for Clerk options.</param>
    /// <param name="convexConfiguration">The configuration section for Convex options.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvexWithClerkForBlazor(
    ///     builder.Configuration.GetSection("Clerk"),
    ///     builder.Configuration.GetSection("Convex")
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddConvexWithClerkForBlazor(
        this IServiceCollection services,
        IConfiguration clerkConfiguration,
        IConfiguration convexConfiguration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (clerkConfiguration == null)
        {
            throw new ArgumentNullException(nameof(clerkConfiguration));
        }

        if (convexConfiguration == null)
        {
            throw new ArgumentNullException(nameof(convexConfiguration));
        }

        // Register BlazorClerkTokenService as the implementation of IClerkTokenService
        _ = services.AddScoped<IClerkTokenService, BlazorClerkTokenService>();
        _ = services.AddScoped<BlazorClerkTokenService>();

        // Register Clerk options from configuration
        _ = services.AddOptions<ClerkOptions>()
            .Bind(clerkConfiguration)
            .ValidateOnStart();

        // Register Convex options from configuration
        _ = services.AddOptions<ConvexOptions>()
            .Bind(convexConfiguration)
            .ValidateOnStart();

        // Register Clerk token provider
        _ = services.AddClerkAuthTokenProvider();

        // Register Convex client with Clerk authentication
        _ = services.AddConvexCoreWithClerk();

        return services;
    }

    /// <summary>
    /// Adds Convex client services with Clerk authentication for Blazor WebAssembly using action-based configuration.
    /// Automatically injects required JavaScript code and registers BlazorClerkTokenService.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureClerk">An action to configure the <see cref="ClerkOptions"/>.</param>
    /// <param name="configureConvex">An action to configure the <see cref="ConvexOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvexWithClerkForBlazor(
    ///     clerkOptions => {
    ///         clerkOptions.PublishableKey = "pk_test_...";
    ///         clerkOptions.TokenTemplate = "convex";
    ///     },
    ///     convexOptions => {
    ///         convexOptions.DeploymentUrl = "https://your-app.convex.cloud";
    ///     }
    /// );
    /// </code>
    /// </example>
    public static IServiceCollection AddConvexWithClerkForBlazor(
        this IServiceCollection services,
        Action<ClerkOptions> configureClerk,
        Action<ConvexOptions> configureConvex)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureClerk == null)
        {
            throw new ArgumentNullException(nameof(configureClerk));
        }

        if (configureConvex == null)
        {
            throw new ArgumentNullException(nameof(configureConvex));
        }

        // Register BlazorClerkTokenService as the implementation of IClerkTokenService
        _ = services.AddScoped<IClerkTokenService, BlazorClerkTokenService>();
        _ = services.AddScoped<BlazorClerkTokenService>();

        // Register Clerk options
        _ = services.AddOptions<ClerkOptions>()
            .Configure(configureClerk)
            .ValidateOnStart();

        // Register Convex options
        _ = services.AddOptions<ConvexOptions>()
            .Configure(configureConvex)
            .ValidateOnStart();

        // Register Clerk token provider
        _ = services.AddClerkAuthTokenProvider();

        // Register Convex client with Clerk authentication
        _ = services.AddConvexCoreWithClerk();

        return services;
    }

    /// <summary>
    /// Adds Convex client services with Clerk authentication configured via the builder pattern.
    /// </summary>
    private static IServiceCollection AddConvexCoreWithClerk(this IServiceCollection services)
    {
        // Use the base Clerk package's AddConvexWithClerk method but override IClerkTokenService
        // We've already registered BlazorClerkTokenService, so we just need to ensure Convex is set up
        // Call AddConvexCore through reflection since it's private
        var extensionType = typeof(ClerkOptions).Assembly.GetType("Convex.Client.Extensions.Clerk.ClerkServiceCollectionExtensions");
        if (extensionType != null)
        {
            // Get the private AddConvexCoreWithClerk method
            var addConvexCoreMethod = extensionType.GetMethod("AddConvexCoreWithClerk",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (addConvexCoreMethod != null)
            {
                _ = addConvexCoreMethod.Invoke(null, new object[] { services });
                return services;
            }
        }

        // Fallback: manually set up Convex with Clerk
        // This mirrors the logic from ClerkServiceCollectionExtensions
        var convexExtensionType = typeof(ConvexOptions).Assembly.GetType("Convex.Client.Extensions.DependencyInjection.ConvexServiceCollectionExtensions");
        if (convexExtensionType != null)
        {
            var addConvexCoreMethod = convexExtensionType.GetMethod("AddConvexCore",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (addConvexCoreMethod != null)
            {
                _ = addConvexCoreMethod.Invoke(null, new object[] { services });
            }
        }

        // Register IConvexClient with Clerk authentication (override the default registration)
        var existingRegistration = services.FirstOrDefault(s => s.ServiceType == typeof(IConvexClient));
        if (existingRegistration != null)
        {
            _ = services.Remove(existingRegistration);
        }
        _ = services.AddScoped<IConvexClient>(sp =>
        {
            var factory = sp.GetRequiredService<IConvexClientFactory>();
            var client = factory.CreateClient();
            var tokenProvider = sp.GetRequiredService<ClerkAuthTokenProvider>();

            // Configure client with Clerk authentication
            client.Auth.SetAuthTokenProviderAsync(tokenProvider).GetAwaiter().GetResult();

            return client;
        });

        return services;
    }
}

