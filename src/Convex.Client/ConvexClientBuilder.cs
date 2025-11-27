using Convex.Client.Infrastructure.Internal.Connection;
using Convex.Client.Infrastructure.Middleware;
using Convex.Client.Infrastructure.Validation;
using Microsoft.Extensions.Logging;

namespace Convex.Client;

/// <summary>
/// Fluent builder for creating and configuring ConvexClient instances.
/// Provides a convenient way to configure client options, middleware, and connection settings.
/// </summary>
/// <remarks>
/// <para>
/// Use the builder pattern to configure your ConvexClient with a fluent API.
/// All configuration methods return the builder instance for method chaining.
/// </para>
/// <para>
/// Always call <see cref="Build"/> or <see cref="BuildAsync(CancellationToken)"/> at the end
/// to create the configured client instance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage
/// var client = new ConvexClientBuilder()
///     .UseDeployment("https://your-deployment.convex.cloud")
///     .Build();
///
/// // Advanced configuration
/// var client = new ConvexClientBuilder()
///     .UseDeployment("https://your-deployment.convex.cloud")
///     .WithTimeout(TimeSpan.FromSeconds(60))
///     .WithAutoReconnect(maxAttempts: 5)
///     .WithLogging(logger)
///     .EnableDebugLogging()
///     .PreConnect()
///     .Build();
///
/// // Development defaults
/// var devClient = new ConvexClientBuilder()
///     .UseDeployment("https://your-deployment.convex.cloud")
///     .WithDevelopmentDefaults()
///     .Build();
/// </code>
/// </example>
/// <seealso cref="ConvexClient"/>
/// <seealso cref="IConvexClientBuilder"/>
public sealed class ConvexClientBuilder
{
    private string? _deploymentUrl;
    private readonly ConvexClientOptions _options = new();
    private readonly List<IConvexMiddleware> _middleware = [];

    /// <summary>
    /// Sets the Convex deployment URL.
    /// This is required and must be called before building the client.
    /// </summary>
    /// <param name="deploymentUrl">The Convex deployment URL (e.g., "https://happy-animal-123.convex.cloud"). You can find this in your Convex dashboard.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deploymentUrl"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://happy-animal-123.convex.cloud")
    ///     .Build();
    /// </code>
    /// </example>
    public ConvexClientBuilder UseDeployment(string deploymentUrl)
    {
        _deploymentUrl = deploymentUrl;
        return this;
    }

    /// <summary>
    /// Sets the HttpClient to use for HTTP operations.
    /// If not specified, a new HttpClient will be created and owned by the ConvexClient.
    /// </summary>
    /// <param name="httpClient">The HttpClient instance to use. The client will not dispose this HttpClient - you are responsible for its lifecycle.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
    /// <remarks>
    /// Use this when you need to share an HttpClient instance or configure custom HTTP behavior
    /// (e.g., custom headers, proxy settings, etc.). The ConvexClient will not dispose this HttpClient.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use a shared HttpClient
    /// var httpClient = new HttpClient();
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithHttpClient(httpClient)
    ///     .Build();
    /// // Remember to dispose httpClient separately if needed
    /// </code>
    /// </example>
    public ConvexClientBuilder WithHttpClient(HttpClient httpClient)
    {
        _options.HttpClient = httpClient;
        return this;
    }

    /// <summary>
    /// Sets the default timeout for HTTP operations (queries, mutations, actions).
    /// Individual operations can override this with their own timeout settings.
    /// </summary>
    /// <param name="timeout">The timeout duration. Must be greater than zero and less than or equal to 24 hours.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero or greater than 24 hours.</exception>
    /// <remarks>
    /// The default timeout is 30 seconds. Use longer timeouts for operations that may take longer
    /// (e.g., actions that call external APIs).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set timeout for all operations
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithTimeout(TimeSpan.FromSeconds(60))
    ///     .Build();
    ///
    /// // Override for specific operation
    /// await client.Action&lt;string&gt;("functions/slowOperation")
    ///     .WithTimeout(TimeSpan.FromMinutes(5))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    public ConvexClientBuilder WithTimeout(TimeSpan timeout)
    {
        _options.DefaultTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Configures automatic reconnection for WebSocket connections.
    /// Uses exponential backoff with jitter to avoid thundering herd problems.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of reconnection attempts. Use -1 for unlimited attempts (recommended for production). Default is 5.</param>
    /// <param name="delayMs">Base delay between reconnection attempts in milliseconds. Default is 1000ms (1 second).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Reconnection uses exponential backoff: the delay doubles after each failed attempt,
    /// up to a maximum of 30 seconds. Jitter is added to prevent synchronized reconnection attempts.
    /// </para>
    /// <para>
    /// For production applications, consider using -1 (unlimited) to ensure the client always reconnects.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Limited reconnection attempts (good for testing)
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithAutoReconnect(maxAttempts: 5, delayMs: 1000)
    ///     .Build();
    ///
    /// // Unlimited reconnection (recommended for production)
    /// var productionClient = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithAutoReconnect(maxAttempts: -1)
    ///     .Build();
    /// </code>
    /// </example>
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
    /// Configures a custom reconnection policy for WebSocket connections.
    /// Use this when you need fine-grained control over reconnection behavior.
    /// </summary>
    /// <param name="policy">The reconnection policy to use. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <remarks>
    /// For most use cases, <see cref="WithAutoReconnect(int, int)"/> is sufficient.
    /// Use this method when you need custom reconnection logic.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom reconnection policy
    /// var customPolicy = new ReconnectionPolicy
    /// {
    ///     MaxAttempts = 10,
    ///     BaseDelay = TimeSpan.FromSeconds(2),
    ///     MaxDelay = TimeSpan.FromMinutes(1),
    ///     UseExponentialBackoff = true,
    ///     UseJitter = true
    /// };
    ///
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithReconnectionPolicy(customPolicy)
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="ReconnectionPolicy"/>
    public ConvexClientBuilder WithReconnectionPolicy(ReconnectionPolicy policy)
    {
        _options.ReconnectionPolicy = policy;
        return this;
    }

    /// <summary>
    /// Sets the SynchronizationContext for automatic UI thread marshalling.
    /// Subscription callbacks and connection state changes will be marshalled to this context.
    /// </summary>
    /// <param name="synchronizationContext">The SynchronizationContext to use for marshalling events to the UI thread. If null, SynchronizationContext.Current will be captured at client creation.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is useful in UI applications (WPF, WinForms, MAUI) where you need to ensure
    /// subscription callbacks run on the UI thread. If not set, the current SynchronizationContext
    /// will be captured automatically when the client is created.
    /// </para>
    /// <para>
    /// For Blazor applications, use the Blazor extensions package which handles this automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a WPF application
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithSyncContext(SynchronizationContext.Current)
    ///     .Build();
    ///
    /// // Subscription callbacks will now run on UI thread automatically
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Subscribe(todos => {
    ///         // This runs on UI thread - safe to update UI directly
    ///         TodoList.ItemsSource = todos;
    ///     });
    /// </code>
    /// </example>
    public ConvexClientBuilder WithSyncContext(SynchronizationContext? synchronizationContext)
    {
        _options.SynchronizationContext = synchronizationContext;
        return this;
    }

    /// <summary>
    /// Sets the logger for structured logging.
    /// When configured, the client will log important events, errors, and debug information.
    /// </summary>
    /// <param name="logger">The logger instance (typically from Microsoft.Extensions.Logging).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    /// <remarks>
    /// Combine with <see cref="EnableDebugLogging(bool)"/> to control the verbosity of logs.
    /// Debug logging includes detailed information about requests, responses, and internal operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using Microsoft.Extensions.Logging
    /// var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    /// var logger = loggerFactory.CreateLogger&lt;ConvexClient&gt;();
    ///
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithLogging(logger)
    ///     .EnableDebugLogging()
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="EnableDebugLogging(bool)"/>
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
    /// <remarks>
    /// <para>
    /// When enabled, the WebSocket connection is established immediately when the client is created,
    /// rather than waiting for the first subscription. This can help catch connection issues early.
    /// </para>
    /// <para>
    /// If pre-connection fails, check <see cref="ConvexClient.PreConnectError"/> for details.
    /// The client will still be usable, but you may want to handle the error.
    /// </para>
    /// <para>
    /// For most use cases, lazy connection (default) is sufficient and more efficient.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Pre-connect to catch connection issues early
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .PreConnect()
    ///     .Build();
    ///
    /// // Check if pre-connection succeeded
    /// if (client.PreConnectError != null)
    /// {
    ///     Console.WriteLine($"Pre-connection failed: {client.PreConnectError.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ConvexClient.PreConnectError"/>
    public ConvexClientBuilder PreConnect()
    {
        _options.PreConnect = true;
        return this;
    }

    /// <summary>
    /// Adds a middleware instance to the pipeline.
    /// Middleware is executed in the order it is added, allowing you to intercept and modify requests/responses.
    /// </summary>
    /// <param name="middleware">The middleware instance to add. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is null.</exception>
    /// <remarks>
    /// Middleware can be used for:
    /// <list type="bullet">
    /// <item>Request/response logging</item>
    /// <item>Adding custom headers</item>
    /// <item>Retry logic</item>
    /// <item>Metrics collection</item>
    /// <item>Request transformation</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Add custom middleware
    /// var customMiddleware = new MyCustomMiddleware();
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .UseMiddleware(customMiddleware)
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="IConvexMiddleware"/>
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
    /// This is the final step in the builder pattern - call this to create the configured client.
    /// </summary>
    /// <returns>A new ConvexClient instance configured with all the specified options.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the deployment URL is not set. Call <see cref="UseDeployment(string)"/> first.</exception>
    /// <remarks>
    /// <para>
    /// After building, the client is ready to use. Remember to dispose it when done:
    /// <code>client.Dispose();</code>
    /// </para>
    /// <para>
    /// If PreConnect was enabled, the WebSocket connection will be established in the background.
    /// Check <see cref="ConvexClient.PreConnectError"/> to see if it succeeded.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Build and use the client
    /// using var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithTimeout(TimeSpan.FromSeconds(30))
    ///     .Build();
    ///
    /// // Use the client
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="BuildAsync(CancellationToken)"/>
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
    /// This method is provided for API consistency, but currently returns synchronously.
    /// Connection happens automatically on first subscription.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Currently not used but provided for future async operations.</param>
    /// <returns>A task that completes with a new ConvexClient instance configured with all the specified options.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the deployment URL is not set. Call <see cref="UseDeployment(string)"/> first.</exception>
    /// <remarks>
    /// <para>
    /// This method currently returns synchronously but is provided for consistency with async patterns.
    /// Future versions may perform async initialization here.
    /// </para>
    /// <para>
    /// Connection management is fully automatic - the WebSocket connects when you create your first subscription.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Build asynchronously
    /// var client = await new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithTimeout(TimeSpan.FromSeconds(30))
    ///     .BuildAsync();
    ///
    /// // Use the client
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="Build"/>
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
