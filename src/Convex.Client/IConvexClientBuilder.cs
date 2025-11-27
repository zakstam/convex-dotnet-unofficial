using Convex.Client.Infrastructure.Internal.Connection;
using Microsoft.Extensions.Logging;

namespace Convex.Client;

/// <summary>
/// Fluent builder interface for configuring and creating a ConvexClient instance.
/// Provides methods for configuring client options, connection settings, and middleware.
/// </summary>
/// <remarks>
/// This interface is implemented by <see cref="ConvexClientBuilder"/>.
/// Use the builder pattern to configure your client with a fluent API.
/// </remarks>
/// <example>
/// <code>
/// var client = new ConvexClientBuilder()
///     .UseDeployment("https://your-deployment.convex.cloud")
///     .WithTimeout(TimeSpan.FromSeconds(30))
///     .Build();
/// </code>
/// </example>
/// <seealso cref="ConvexClientBuilder"/>
public interface IConvexClientBuilder
{
    /// <summary>
    /// Configures a custom HttpClient for HTTP operations.
    /// </summary>
    IConvexClientBuilder WithHttpClient(HttpClient httpClient);

    /// <summary>
    /// Sets the default timeout for HTTP operations.
    /// </summary>
    IConvexClientBuilder WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Configures the reconnection policy for WebSocket connections.
    /// </summary>
    IConvexClientBuilder WithReconnectionPolicy(ReconnectionPolicy policy);

    /// <summary>
    /// Configures automatic reconnection with default settings.
    /// </summary>
    IConvexClientBuilder WithAutoReconnect();

    /// <summary>
    /// Configures unlimited reconnection attempts.
    /// </summary>
    IConvexClientBuilder WithUnlimitedReconnect();

    /// <summary>
    /// Disables automatic reconnection.
    /// </summary>
    IConvexClientBuilder WithoutReconnect();

    /// <summary>
    /// Configures the SynchronizationContext for automatic UI thread marshalling.
    /// </summary>
    IConvexClientBuilder WithSynchronizationContext(SynchronizationContext syncContext);

    /// <summary>
    /// Enables pre-connection (connects immediately on client creation).
    /// </summary>
    IConvexClientBuilder WithPreConnect();

    /// <summary>
    /// Configures a custom logger for the client.
    /// </summary>
    IConvexClientBuilder WithLogger(ILogger logger);

    /// <summary>
    /// Builds and returns the configured ConvexClient instance.
    /// </summary>
    IConvexClient Build();
}
