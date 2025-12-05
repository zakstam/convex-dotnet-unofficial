using System.Linq;
using Convex.Client;
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Infrastructure.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Convex.BetterAuth.Extensions;

/// <summary>
/// Extension methods for registering Better Auth services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Better Auth services to the service collection.
    /// This automatically wires up the token provider to the Convex client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration section containing BetterAuth options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvex(config.GetSection("Convex"));
    /// builder.Services.AddConvexBetterAuth(builder.Configuration.GetSection("BetterAuth"));
    /// </code>
    /// </example>
    public static IServiceCollection AddConvexBetterAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BetterAuthOptions>(configuration);
        return services.AddBetterAuthCore();
    }

    /// <summary>
    /// Adds Better Auth services to the service collection with custom options.
    /// This automatically wires up the token provider to the Convex client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure Better Auth options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddConvex(config.GetSection("Convex"));
    /// builder.Services.AddConvexBetterAuth(options =>
    /// {
    ///     options.SiteUrl = "https://your-deployment.convex.site";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddConvexBetterAuth(
        this IServiceCollection services,
        Action<BetterAuthOptions> configure)
    {
        services.Configure(configure);
        return services.AddBetterAuthCore();
    }

    /// <summary>
    /// Adds a custom session storage implementation for Better Auth.
    /// Call this before AddConvexBetterAuth to override the default in-memory storage.
    /// </summary>
    /// <typeparam name="TStorage">The session storage implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // For Blazor WebAssembly, create a localStorage-based storage
    /// builder.Services.AddBetterAuthSessionStorage&lt;LocalStorageSessionStorage&gt;();
    /// builder.Services.AddConvexBetterAuth(config.GetSection("BetterAuth"));
    /// </code>
    /// </example>
    public static IServiceCollection AddBetterAuthSessionStorage<TStorage>(
        this IServiceCollection services)
        where TStorage : class, ISessionStorage
    {
        // Use singleton for Blazor WASM to maintain state across components
        services.AddSingleton<ISessionStorage, TStorage>();
        return services;
    }

    /// <summary>
    /// Core registration for Better Auth services.
    /// Registers auth services and wires up the token provider to the Convex client.
    /// </summary>
    private static IServiceCollection AddBetterAuthCore(this IServiceCollection services)
    {
        // Register session storage (default to in-memory if not already registered)
        services.TryAddSingleton<ISessionStorage, InMemorySessionStorage>();

        // Register HttpClientFactory if not already registered
        services.AddHttpClient();

        // Register the auth service as singleton so auth state is shared across components
        services.TryAddSingleton<IBetterAuthService>(sp =>
        {
            var httpClientFactory = sp.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            var sessionStorage = sp.GetRequiredService<ISessionStorage>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BetterAuthOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BetterAuthService>>();
            return new BetterAuthService(httpClient, sessionStorage, options, logger);
        });

        // Register the token provider (exchanges session tokens for Convex JWTs)
        services.TryAddSingleton<BetterAuthTokenProvider>(sp =>
        {
            var authService = sp.GetRequiredService<IBetterAuthService>();
            var httpClientFactory = sp.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BetterAuthOptions>>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BetterAuthTokenProvider>>();
            return new BetterAuthTokenProvider(authService, httpClient, options, logger);
        });
        services.TryAddSingleton<IAuthTokenProvider>(sp => sp.GetRequiredService<BetterAuthTokenProvider>());

        // Wire up the token provider to the Convex client automatically
        // This removes the existing IConvexClient registration and replaces it with one
        // that has the Better Auth token provider configured
        var existingRegistration = services.FirstOrDefault(s => s.ServiceType == typeof(IConvexClient));
        if (existingRegistration != null)
        {
            services.Remove(existingRegistration);
        }

        // Re-register IConvexClient with Better Auth token provider wired up
        services.AddScoped<IConvexClient>(sp =>
        {
            var factory = sp.GetRequiredService<IConvexClientFactory>();
            var client = factory.CreateClient();
            var tokenProvider = sp.GetRequiredService<BetterAuthTokenProvider>();

            // Configure client with Better Auth authentication
            // GetAwaiter().GetResult() is safe here during DI resolution
            client.Auth.SetAuthTokenProviderAsync(tokenProvider).GetAwaiter().GetResult();

            return client;
        });

        return services;
    }
}
