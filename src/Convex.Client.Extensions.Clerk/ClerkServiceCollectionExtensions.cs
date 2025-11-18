using System.Linq;
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Shared.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Convex.Client.Extensions.Clerk;

/// <summary>
/// Extension methods for adding Clerk authentication to Convex client services.
/// </summary>
public static class ClerkServiceCollectionExtensions
{
    /// <summary>
    /// Adds Convex client services with Clerk authentication to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureClerk">An action to configure the <see cref="ClerkOptions"/>.</param>
    /// <param name="configureConvex">An action to configure the <see cref="ConvexOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvexWithClerk(
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
    public static IServiceCollection AddConvexWithClerk(
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
    /// Adds Convex client services with Clerk authentication using configuration from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="clerkConfiguration">The configuration section for Clerk options.</param>
    /// <param name="convexConfiguration">The configuration section for Convex options.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddConvexWithClerk(
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
    /// Adds Clerk authentication token provider to the service collection.
    /// Use this when you want to manually configure the Convex client with Clerk authentication.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action to configure the <see cref="ClerkOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddClerkAuthTokenProvider(
        this IServiceCollection services,
        Action<ClerkOptions>? configure = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register Clerk options if not already registered
        if (configure != null)
        {
            _ = services.AddOptions<ClerkOptions>()
                .Configure(configure)
                .ValidateOnStart();
        }
        else
        {
            // Only add if not already registered
            if (!services.Any(s => s.ServiceType == typeof(IOptions<ClerkOptions>)))
            {
                _ = services.AddOptions<ClerkOptions>();
            }
        }

        // Register IClerkTokenService - users must provide their own implementation
        // This allows flexibility with different Clerk SDK implementations
        services.TryAddScoped<IClerkTokenService, DefaultClerkTokenService>();

        // Register the token provider
        services.TryAddScoped<ClerkAuthTokenProvider>(sp =>
        {
            var clerkTokenService = sp.GetRequiredService<IClerkTokenService>();
            var options = sp.GetRequiredService<IOptions<ClerkOptions>>().Value;
            var logger = sp.GetService<ILogger<ClerkAuthTokenProvider>>();
            return new ClerkAuthTokenProvider(clerkTokenService, options, logger);
        });

        // Also register as IAuthTokenProvider for convenience
        services.TryAddScoped<IAuthTokenProvider>(sp => sp.GetRequiredService<ClerkAuthTokenProvider>());

        return services;
    }

    /// <summary>
    /// Adds Convex client services with Clerk authentication configured via the builder pattern.
    /// </summary>
    private static IServiceCollection AddConvexCoreWithClerk(this IServiceCollection services)
    {
        // Call AddConvexCore through reflection since it's private
        // This ensures Convex core services are registered
        var extensionType = typeof(ConvexOptions).Assembly.GetType("Convex.Client.Extensions.DependencyInjection.ConvexServiceCollectionExtensions");
        if (extensionType != null)
        {
            var addConvexCoreMethod = extensionType.GetMethod("AddConvexCore",
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
            if (client is ConvexClient convexClient)
            {
                convexClient.AuthenticationSlice.SetAuthTokenProviderAsync(tokenProvider).GetAwaiter().GetResult();
            }
            else
            {
                throw new InvalidOperationException(
                    "Clerk authentication requires ConvexClient. " +
                    "The factory returned a client that does not support AuthenticationSlice.");
            }

            return client;
        });

        return services;
    }
}

/// <summary>
/// Default implementation of IClerkTokenService that uses CustomTokenRetriever from options.
/// Users should provide their own implementation based on their Clerk SDK.
/// </summary>
internal sealed class DefaultClerkTokenService(IOptions<ClerkOptions> options) : IClerkTokenService
{
    private readonly ClerkOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public bool IsAuthenticated => _options.CustomTokenRetriever != null;

    public bool IsLoading => false;

    public async Task<string?> GetTokenAsync(string tokenTemplate = "convex", bool skipCache = false, CancellationToken cancellationToken = default)
    {
        if (_options.CustomTokenRetriever == null)
        {
            throw new InvalidOperationException(
                "CustomTokenRetriever must be configured in ClerkOptions, or provide a custom IClerkTokenService implementation.");
        }

        return await _options.CustomTokenRetriever(cancellationToken);
    }
}

