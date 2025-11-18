using Convex.Client.Shared.Internal.Connection;
using Convex.Client.Shared.Middleware;
using Convex.Client.Shared.Validation;
using Microsoft.Extensions.Logging;

namespace Convex.Client;

/// <summary>
/// Fluent builder for creating and configuring ConvexClient instances.
/// </summary>
public sealed class ConvexClientBuilder
{
    private string? _deploymentUrl;
    private readonly ConvexClientOptions _options = new();
    private readonly List<IConvexMiddleware> _middleware = [];

    /// <summary>
    /// Sets the Convex deployment URL.
    /// </summary>
    /// <param name="deploymentUrl">The deployment URL (e.g., "https://happy-animal-123.convex.cloud").</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder UseDeployment(string deploymentUrl)
    {
        _deploymentUrl = deploymentUrl;
        return this;
    }

    /// <summary>
    /// Sets the HttpClient to use for HTTP operations.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithHttpClient(HttpClient httpClient)
    {
        _options.HttpClient = httpClient;
        return this;
    }

    /// <summary>
    /// Sets the default timeout for HTTP operations.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithTimeout(TimeSpan timeout)
    {
        _options.DefaultTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures automatic reconnection for WebSocket connections.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of reconnection attempts (-1 for unlimited).</param>
    /// <param name="delayMs">Base delay between attempts in milliseconds (default 1000ms).</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithAutoReconnect(int maxAttempts = 5, int delayMs = 1000)
    {
        _options.ReconnectionPolicy = new ReconnectionPolicy
        {
            MaxAttempts = maxAttempts,
            BaseDelay = TimeSpan.FromMilliseconds(delayMs),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseExponentialBackoff = true,
            UseJitter = true
        };

        return this;
    }

    /// <summary>
    /// Configures the reconnection policy.
    /// </summary>
    /// <param name="policy">The reconnection policy to use.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithReconnectionPolicy(ReconnectionPolicy policy)
    {
        _options.ReconnectionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the SynchronizationContext for event marshalling.
    /// </summary>
    /// <param name="synchronizationContext">The SynchronizationContext to use.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithSyncContext(SynchronizationContext? synchronizationContext)
    {
        _options.SynchronizationContext = synchronizationContext;
        return this;
    }

    /// <summary>
    /// Sets the logger for structured logging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithLogging(ILogger logger)
    {
        _options.Logger = logger;
        return this;
    }

    /// <summary>
    /// Enables or disables debug-level logging.
    /// When enabled, debug-level logs will be emitted if a logger is configured via WithLogging().
    /// Debug logs include detailed information about requests, responses, and internal operations.
    /// </summary>
    /// <param name="enabled">Whether to enable debug logging (default: true).</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder EnableDebugLogging(bool enabled = true)
    {
        _options.EnableDebugLogging = enabled;
        return this;
    }

    /// <summary>
    /// Enables pre-connection of the WebSocket when the client is created.
    /// By default, the WebSocket connects lazily on first subscription.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder PreConnect()
    {
        _options.PreConnect = true;
        return this;
    }

    /// <summary>
    /// Adds a middleware instance to the pipeline.
    /// Middleware is executed in the order it is added.
    /// </summary>
    /// <param name="middleware">The middleware instance to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder UseMiddleware(IConvexMiddleware middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _middleware.Add(middleware);
        return this;
    }

    /// <summary>
    /// Adds a middleware type to the pipeline.
    /// The middleware will be instantiated with its parameterless constructor.
    /// Middleware is executed in the order it is added.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder UseMiddleware<TMiddleware>() where TMiddleware : IConvexMiddleware, new()
    {
        _middleware.Add(new TMiddleware());
        return this;
    }

    /// <summary>
    /// Adds a middleware type to the pipeline with a factory function.
    /// Middleware is executed in the order it is added.
    /// </summary>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <param name="factory">Factory function to create the middleware instance.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder UseMiddleware<TMiddleware>(Func<TMiddleware> factory)
        where TMiddleware : IConvexMiddleware
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _middleware.Add(factory());
        return this;
    }

    /// <summary>
    /// Adds a simple inline middleware using a function.
    /// </summary>
    /// <param name="middleware">The middleware function.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder Use(Func<ConvexRequest, ConvexRequestDelegate, Task<ConvexResponse>> middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _middleware.Add(new InlineMiddleware(middleware));
        return this;
    }

    /// <summary>
    /// Configures schema validation for query, mutation, and subscription responses.
    /// </summary>
    /// <param name="configure">Action to configure validation options.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithSchemaValidation(Action<SchemaValidationOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var options = new SchemaValidationOptions();
        configure(options);
        _options.SchemaValidation = options;

        // Use RuntimeSchemaValidator by default
        if (_options.SchemaValidator == null)
        {
            _options.SchemaValidator = new RuntimeSchemaValidator();
        }

        return this;
    }

    /// <summary>
    /// Enables strict schema validation (all operations validated, throws on mismatch).
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithStrictSchemaValidation()
    {
        _options.SchemaValidation = SchemaValidationOptions.Strict();
        _options.SchemaValidator = new RuntimeSchemaValidator();
        return this;
    }

    /// <summary>
    /// Adds request logging middleware to track HTTP requests and response times.
    /// Provides statistics via GetStats() method on the middleware instance.
    /// </summary>
    /// <param name="enabled">Whether request logging is enabled (default: true).</param>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithRequestLogging(bool enabled = true)
    {
        var requestLoggingMiddleware = new RequestLoggingMiddleware(enabled);
        _middleware.Add(requestLoggingMiddleware);
        return this;
    }

    /// <summary>
    /// Applies development-friendly default settings:
    /// - Request logging enabled
    /// - Auto-reconnect with 5 attempts
    /// - 30 second timeout
    /// - Debug logging enabled
    /// Ideal for local development and debugging.
    /// Note: Add your own logger with WithLogging() if needed.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithDevelopmentDefaults()
    {
        return WithRequestLogging(enabled: true)
            .WithAutoReconnect(maxAttempts: 5, delayMs: 1000)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .EnableDebugLogging(true);
    }

    /// <summary>
    /// Applies production-friendly default settings:
    /// - No request logging (performance)
    /// - Unlimited auto-reconnect attempts
    /// - 10 second timeout
    /// Optimized for production reliability and performance.
    /// Note: Add your own logger with WithLogging() if needed.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    public ConvexClientBuilder WithProductionDefaults()
    {
        return WithAutoReconnect(maxAttempts: -1, delayMs: 1000)
            .WithTimeout(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Builds the ConvexClient with the configured options.
    /// </summary>
    /// <returns>A new ConvexClient instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the deployment URL is not set.</exception>
    public ConvexClient Build()
    {
        if (string.IsNullOrWhiteSpace(_deploymentUrl))
        {
            throw new InvalidOperationException("Deployment URL must be set using UseDeployment() before building the client.");
        }

        _options.Validate();

        var client = new ConvexClient(_deploymentUrl, _options);

        // Add all middleware to the client
        foreach (var middleware in _middleware)
        {
            client.AddMiddleware(middleware);
        }

        return client;
    }

    /// <summary>
    /// Builds the ConvexClient asynchronously.
    /// Note: Connection happens automatically on first subscription.
    /// The PreConnect option is maintained for API compatibility but connection
    /// management is now fully automatic.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a new ConvexClient instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the deployment URL is not set.</exception>
    public Task<ConvexClient> BuildAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_deploymentUrl))
        {
            throw new InvalidOperationException("Deployment URL must be set using UseDeployment() before building the client.");
        }

        _options.Validate();

        var client = new ConvexClient(_deploymentUrl, _options);

        // Add all middleware to the client
        foreach (var middleware in _middleware)
        {
            client.AddMiddleware(middleware);
        }

        // Connection happens automatically on first subscription.
        // PreConnect option is maintained for API compatibility but is no longer needed.
        return Task.FromResult(client);
    }

    /// <summary>
    /// Internal middleware wrapper for inline middleware functions.
    /// </summary>
    private sealed class InlineMiddleware(Func<ConvexRequest, ConvexRequestDelegate, Task<ConvexResponse>> middleware) : IConvexMiddleware
    {
        private readonly Func<ConvexRequest, ConvexRequestDelegate, Task<ConvexResponse>> _middleware = middleware;

        public Task<ConvexResponse> InvokeAsync(ConvexRequest request, ConvexRequestDelegate next) => _middleware(request, next);
    }
}
