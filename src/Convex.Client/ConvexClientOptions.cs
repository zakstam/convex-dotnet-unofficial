using Convex.Client.Infrastructure.Internal.Connection;
using Convex.Client.Infrastructure.Interceptors;
using Convex.Client.Infrastructure.Validation;
using Microsoft.Extensions.Logging;

namespace Convex.Client;

/// <summary>
/// Configuration options for ConvexClient.
/// </summary>
public sealed class ConvexClientOptions
{
    /// <summary>
    /// Gets or sets the Convex deployment URL.
    /// This property is used by dependency injection configuration.
    /// When constructing ConvexClient directly, pass the URL to the constructor instead.
    /// </summary>
    public string? DeploymentUrl { get; set; }

    /// <summary>
    /// Gets or sets the admin authentication key for privileged operations.
    /// Use this for server-side operations only - never expose admin keys in client applications.
    /// </summary>
    public string? AdminKey { get; set; }

    /// <summary>
    /// Gets or sets the HttpClient to use for HTTP operations.
    /// If not set, a new HttpClient will be created.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>
    /// Gets or sets the default timeout for HTTP operations.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the reconnection policy for WebSocket connections.
    /// If not set, ReconnectionPolicy.Default() will be used.
    /// </summary>
    public ReconnectionPolicy? ReconnectionPolicy { get; set; }

    /// <summary>
    /// Gets or sets the SynchronizationContext to use for event marshalling.
    /// If not set, SynchronizationContext.Current will be captured at client creation.
    /// </summary>
    public SynchronizationContext? SynchronizationContext { get; set; }

    /// <summary>
    /// Gets or sets the logger for structured logging.
    /// </summary>
    public ILogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets whether to enable debug-level logging.
    /// When enabled, debug-level logs will be emitted if a logger is configured.
    /// Debug logs include detailed information about requests, responses, and internal operations.
    /// Default is false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to automatically connect the WebSocket on first subscription.
    /// Default is true.
    /// Note: This property is currently reserved for future use. WebSocket connections are always automatic.
    /// </summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to pre-connect the WebSocket when the client is created.
    /// Default is false (lazy connection).
    /// </summary>
    public bool PreConnect { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable connection quality monitoring.
    /// When enabled, the client will periodically assess connection quality
    /// and raise ConnectionQualityChanged events when quality changes.
    /// Default is true.
    /// </summary>
    public bool EnableQualityMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for checking connection quality.
    /// Quality is assessed at this interval when quality monitoring is enabled.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan QualityCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the list of interceptors to apply to requests and responses.
    /// Interceptors are executed in the order they are added.
    /// </summary>
    public List<IConvexInterceptor> Interceptors { get; set; } = [];

    /// <summary>
    /// Gets or sets the schema validation options.
    /// When configured, responses will be validated against expected types.
    /// </summary>
    public SchemaValidationOptions? SchemaValidation { get; set; }

    /// <summary>
    /// Gets or sets the schema validator to use for validation.
    /// If not set and SchemaValidation is configured, RuntimeSchemaValidator will be used.
    /// </summary>
    public ISchemaValidator? SchemaValidator { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any option value is invalid (e.g., DefaultTimeout &lt;= 0, invalid ReconnectionPolicy, QualityCheckInterval &lt;= 0).</exception>
    public void Validate()
    {
        if (DefaultTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("DefaultTimeout must be greater than zero.", nameof(DefaultTimeout));
        }

        if (ReconnectionPolicy != null)
        {
            if (ReconnectionPolicy.BaseDelay <= TimeSpan.Zero)
            {
                throw new ArgumentException("ReconnectionPolicy.BaseDelay must be greater than zero.", nameof(ReconnectionPolicy));
            }

            if (ReconnectionPolicy.MaxDelay < ReconnectionPolicy.BaseDelay)
            {
                throw new ArgumentException("ReconnectionPolicy.MaxDelay must be >= BaseDelay.", nameof(ReconnectionPolicy));
            }
        }

        if (QualityCheckInterval <= TimeSpan.Zero)
        {
            throw new ArgumentException("QualityCheckInterval must be greater than zero.", nameof(QualityCheckInterval));
        }
    }
}
