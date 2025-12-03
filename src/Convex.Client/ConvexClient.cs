using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.ConsistentQueries;
using Convex.Client.Infrastructure.Internal.Connection;
using Convex.Client.Infrastructure.Internal.Threading;
using Convex.Client.Infrastructure.Internal.WebSocket;
using Convex.Client.Infrastructure.Middleware;
using Convex.Client.Infrastructure.Quality;
using Convex.Client.Infrastructure.Extensions;
using Convex.Client.Infrastructure.Connection;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Infrastructure.Builders;
using Convex.Client.Features.DataAccess.Actions;
using Convex.Client.Features.DataAccess.Caching;
using Convex.Client.Features.DataAccess.Mutations;
using Convex.Client.Features.DataAccess.Queries;

namespace Convex.Client;

/// <summary>
/// Unified Convex client that provides both HTTP operations (queries/mutations)
/// and WebSocket operations (real-time subscriptions) in a single, easy-to-use interface.
/// </summary>
/// <remarks>
/// <para>
/// The ConvexClient is the main entry point for interacting with your Convex backend.
/// It provides a fluent API for queries, mutations, actions, and real-time subscriptions.
/// </para>
/// <para>
/// The client automatically manages WebSocket connections - they connect lazily when you create
/// your first subscription and reconnect automatically with exponential backoff on failures.
/// </para>
/// <para>
/// Always dispose the client when done to properly clean up resources:
/// <code>client.Dispose();</code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple usage
/// using var client = new ConvexClient("https://your-deployment.convex.cloud");
/// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos").ExecuteAsync();
///
/// // With builder for advanced configuration
/// var client = new ConvexClientBuilder()
///     .UseDeployment("https://your-deployment.convex.cloud")
///     .WithAutoReconnect(maxAttempts: 5)
///     .WithTimeout(TimeSpan.FromSeconds(30))
///     .Build();
///
/// // Real-time subscription
/// client.Observe&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
///     .Subscribe(todos => Console.WriteLine($"Got {todos.Count} todos"));
/// </code>
/// </example>
/// <seealso cref="IConvexClient"/>
/// <seealso cref="ConvexClientBuilder"/>
public sealed class ConvexClient : IConvexClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly SyncContextCapture _syncContext;
    private readonly Lazy<ConvexWebSocketClient> _webSocketClient;
    private readonly IHttpClientProvider _httpProvider;
    private readonly IConvexSerializer _serializer;
    private readonly QueriesSlice _queries;
    private readonly MutationsSlice _mutations;
    private readonly ActionsSlice _actions;
    private TimeSpan _timeout;
    private readonly Features.RealTime.Pagination.PaginationSlice _pagination;
    private readonly Features.Storage.Files.FileStorageSlice _fileStorage;
    private readonly Features.Storage.VectorSearch.VectorSearchSlice _vectorSearch;
    private readonly Features.Operational.HttpActions.HttpActionsSlice _httpActions;
    private readonly Features.Operational.Scheduling.SchedulingSlice _scheduling;
    private readonly ReactiveCacheImplementation _reactiveCache;
    private readonly Features.Security.Authentication.AuthenticationSlice _authentication;
    private readonly Features.Observability.Health.HealthSlice _health;
    private readonly Features.Observability.Diagnostics.DiagnosticsSlice _diagnostics;
    private readonly Features.Observability.Resilience.ResilienceSlice _resilience;
    private readonly object _connectionStateLock = new();
    private readonly QueryDependencyRegistry _dependencyRegistry;
    private readonly MiddlewarePipeline _middlewarePipeline;
    private Timer? _qualityCheckTimer;
    private ConnectionQuality _lastQuality = ConnectionQuality.Unknown;
    private bool _isDisposed;

    // PreConnect state tracking
    private readonly Task? _preConnectTask;

    // Event handler tracking for proper cleanup
    private EventHandler<ConnectionState>? _connectionStateChangedHandler;

    // Reactive subjects for observable streams
    private readonly Subject<ConnectionState> _connectionStateSubject = new();
    private readonly Subject<ConnectionQuality> _connectionQualitySubject = new();
    private readonly Subject<Features.Security.Authentication.AuthenticationState> _authenticationStateSubject = new();

    /// <inheritdoc/>
    public string DeploymentUrl { get; }

    /// <inheritdoc/>
    public TimeSpan Timeout
    {
        get => _timeout;
        set
        {
            if (value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be greater than zero.");
            if (value > TimeSpan.FromHours(24))
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout cannot exceed 24 hours.");
            _timeout = value;
            _httpClient.Timeout = value;
        }
    }

    /// <inheritdoc/>
    public ConnectionState ConnectionState
    {
        get
        {
            lock (_connectionStateLock)
            {
                return _webSocketClient.IsValueCreated
                    ? _webSocketClient.Value.ConnectionState
                    : ConnectionState.Disconnected;
            }
        }
    }

    /// <inheritdoc/>
    public IObservable<ConnectionState> ConnectionStateChanges => _connectionStateSubject.AsObservable();

    /// <inheritdoc/>
    public IObservable<ConnectionQuality> ConnectionQualityChanges => _connectionQualitySubject.AsObservable();

    /// <inheritdoc/>
    public IObservable<Features.Security.Authentication.AuthenticationState> AuthenticationStateChanges => _authenticationStateSubject.AsObservable();

    /// <summary>
    /// Gets the error that occurred during PreConnect, if any.
    /// Returns null if PreConnect was not enabled or if it succeeded.
    /// </summary>
    /// <value>
    /// The exception that occurred during PreConnect initialization, or null if PreConnect
    /// was not enabled, hasn't completed yet, or succeeded.
    /// </value>
    /// <remarks>
    /// Check this property after construction if PreConnect was enabled to determine if
    /// the initial connection attempt failed. The client will still be usable, but you may
    /// want to handle the error or retry the connection.
    /// </remarks>
    /// <example>
    /// <code>
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .PreConnect()
    ///     .Build();
    ///
    /// // Check if PreConnect succeeded
    /// if (client.PreConnectError != null)
    /// {
    ///     Console.WriteLine($"PreConnect failed: {client.PreConnectError.Message}");
    ///     // Optionally retry or handle the error
    /// }
    /// </code>
    /// </example>
    public Exception? PreConnectError { get; private set; }


    /// <summary>
    /// Ensures the WebSocket connection is established and ready.
    /// If PreConnect was enabled, this will wait for the pre-connection to complete.
    /// If PreConnect was not enabled, this will establish the connection now.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation. Can be used to cancel the connection attempt.</param>
    /// <returns>A task that completes when the connection is established.</returns>
    /// <exception cref="InvalidOperationException">Thrown if PreConnect failed. Check <see cref="PreConnectError"/> for details.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is typically not needed as connections are established automatically when you create subscriptions.
    /// Use this method when you want to ensure the connection is ready before creating subscriptions, or when
    /// you need to wait for PreConnect to complete.
    /// </para>
    /// <para>
    /// If PreConnect was enabled and failed, this method will throw an InvalidOperationException.
    /// Check <see cref="PreConnectError"/> before calling this method to handle errors gracefully.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Ensure connection before creating subscriptions
    /// await client.EnsureConnectedAsync();
    ///
    /// // Now safe to create subscriptions
    /// client.Observe&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Subscribe(todos => UpdateUI(todos));
    ///
    /// // With cancellation support
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    /// try
    /// {
    ///     await client.EnsureConnectedAsync(cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     Console.WriteLine("Connection attempt timed out");
    /// }
    /// </code>
    /// </example>
    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default)
    {
        // If PreConnect task exists, wait for it to complete
        if (_preConnectTask != null)
        {
            await _preConnectTask.WaitAsync(cancellationToken).ConfigureAwait(false);

            // If PreConnect failed, throw the stored error
            if (PreConnectError != null)
            {
                throw new InvalidOperationException(
                    $"PreConnect to {DeploymentUrl} failed. See inner exception for details.",
                    PreConnectError);
            }

            return;
        }

        // Otherwise, ensure WebSocket is initialized and connect if needed
        EnsureWebSocketInitialized();

        if (_webSocketClient.Value.ConnectionState == ConnectionState.Disconnected)
        {
            await _webSocketClient.Value.ConnectAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a new ConvexClient with the specified deployment URL.
    /// Uses default configuration options (30 second timeout, automatic reconnection, etc.).
    /// </summary>
    /// <param name="deploymentUrl">The Convex deployment URL (e.g., "https://happy-animal-123.convex.cloud").</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="deploymentUrl"/> is null or whitespace.</exception>
    /// <remarks>
    /// For advanced configuration, use <see cref="ConvexClientBuilder"/> instead.
    /// Always dispose the client when done: <code>client.Dispose();</code>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple client creation
    /// using var client = new ConvexClient("https://your-deployment.convex.cloud");
    ///
    /// // Use the client
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="ConvexClient(string, ConvexClientOptions?)"/>
    /// <seealso cref="ConvexClientBuilder"/>
    public ConvexClient(string deploymentUrl) : this(deploymentUrl, null)
    {
    }

    /// <summary>
    /// Creates a new ConvexClient with the specified deployment URL and options.
    /// Allows fine-grained control over client behavior including timeouts, reconnection policies, and logging.
    /// </summary>
    /// <param name="deploymentUrl">The Convex deployment URL (e.g., "https://happy-animal-123.convex.cloud").</param>
    /// <param name="options">Optional client configuration. If null, default options are used (30 second timeout, automatic reconnection, etc.).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="deploymentUrl"/> is null or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// For most use cases, the simple constructor <see cref="ConvexClient(string)"/> is sufficient.
    /// Use this constructor when you need custom configuration like:
    /// </para>
    /// <list type="bullet">
    /// <item>Custom timeout values</item>
    /// <item>Custom reconnection policies</item>
    /// <item>Logging configuration</item>
    /// <item>PreConnect for early connection establishment</item>
    /// <item>Custom HttpClient instances</item>
    /// </list>
    /// <para>
    /// Always dispose the client when done: <code>client.Dispose();</code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Client with custom options
    /// var options = new ConvexClientOptions
    /// {
    ///     DefaultTimeout = TimeSpan.FromSeconds(60),
    ///     EnableDebugLogging = true
    /// };
    /// using var client = new ConvexClient("https://your-deployment.convex.cloud", options);
    ///
    /// // Or use the builder for fluent configuration
    /// var client = new ConvexClientBuilder()
    ///     .UseDeployment("https://your-deployment.convex.cloud")
    ///     .WithTimeout(TimeSpan.FromSeconds(60))
    ///     .EnableDebugLogging()
    ///     .Build();
    /// </code>
    /// </example>
    /// <seealso cref="ConvexClientOptions"/>
    /// <seealso cref="ConvexClientBuilder"/>
    public ConvexClient(string deploymentUrl, ConvexClientOptions? options)
    {
        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or empty.", nameof(deploymentUrl));
        }

        options?.Validate();

        DeploymentUrl = deploymentUrl;

        // Get logger and debug settings early for all components
        var logger = options?.Logger;
        var enableDebugLogging = options?.EnableDebugLogging ?? false;

        QualityMonitor = new ConnectionQualityMonitor(logger, enableDebugLogging);
        _reactiveCache = new ReactiveCacheImplementation(logger, enableDebugLogging);
        _health = new Features.Observability.Health.HealthSlice(logger, enableDebugLogging);
        _diagnostics = new Features.Observability.Diagnostics.DiagnosticsSlice();
        _dependencyRegistry = new QueryDependencyRegistry();
        _middlewarePipeline = new MiddlewarePipeline(logger, enableDebugLogging);

        // Capture SynchronizationContext for automatic UI thread marshalling
        _syncContext = options?.SynchronizationContext != null
            ? new SyncContextCapture(options.SynchronizationContext)
            : new SyncContextCapture();

        // Initialize HTTP client
        var httpClient = options?.HttpClient ?? new HttpClient();
        _httpClient = httpClient;
        _ownsHttpClient = options?.HttpClient == null;
        _timeout = options?.DefaultTimeout ?? TimeSpan.FromSeconds(30);
        _httpClient.Timeout = _timeout;

        // Initialize Shared infrastructure for slices
        _httpProvider = new DefaultHttpClientProvider(httpClient, deploymentUrl, logger, enableDebugLogging);
        _serializer = new DefaultConvexSerializer();
        _resilience = new Features.Observability.Resilience.ResilienceSlice(logger, enableDebugLogging);
        _queries = new QueriesSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _mutations = new MutationsSlice(_httpProvider, _serializer, _reactiveCache, InvalidateDependentQueriesAsync, _syncContext, logger, enableDebugLogging);
        _actions = new ActionsSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        TimestampManager = new TimestampManager(httpClient, deploymentUrl);
        _fileStorage = new Features.Storage.Files.FileStorageSlice(_httpProvider, _serializer, httpClient, logger, enableDebugLogging);
        _vectorSearch = new Features.Storage.VectorSearch.VectorSearchSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _httpActions = new Features.Operational.HttpActions.HttpActionsSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _scheduling = new Features.Operational.Scheduling.SchedulingSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _pagination = new Features.RealTime.Pagination.PaginationSlice(_httpProvider, _serializer, logger, enableDebugLogging);

        // Initialize authentication slice with logger and debug logging
        _authentication = new Features.Security.Authentication.AuthenticationSlice(logger, enableDebugLogging);

        // Wire up authentication to HTTP provider (slice coordination through facade)
        if (_httpProvider is DefaultHttpClientProvider defaultProvider)
        {
            defaultProvider.SetAuthHeadersProvider(ct => _authentication.GetAuthHeadersAsync(ct));
        }

        // Lazy initialization of WebSocket client (only connects on first subscription)
        // Pass syncContext, reconnectionPolicy, and logger directly to WebSocketClient
        var reconnectionPolicy = options?.ReconnectionPolicy ?? ReconnectionPolicy.Default();
        _webSocketClient = new Lazy<ConvexWebSocketClient>(() =>
        {
            var client = new ConvexWebSocketClient(deploymentUrl, _syncContext, reconnectionPolicy, logger);

            // Wire up authentication to WebSocket client (slice coordination through facade)
            client.SetAuthTokenProvider(ct => _authentication.GetAuthTokenAsync(ct));

            // Create and store event handler for proper cleanup later
            _connectionStateChangedHandler = (s, state) =>
            {
                // Track quality metrics based on connection state
                if (state == ConnectionState.Connected)
                {
                    QualityMonitor.RecordConnected();
                }
                else if (state == ConnectionState.Disconnected)
                {
                    QualityMonitor.RecordDisconnected();
                }

                // Publish to observable stream
                _connectionStateSubject.OnNext(state);
            };

            // Wire up connection state changes to our event
            // Note: WebSocketClient already handles thread marshalling internally
            client.ConnectionStateChanged += _connectionStateChangedHandler;

            return client;
        });

        // Start periodic quality monitoring if enabled
        if (options?.EnableQualityMonitoring != false)
        {
            var interval = options?.QualityCheckInterval ?? TimeSpan.FromSeconds(10);
            StartQualityMonitoring(interval);
        }

        // Pre-connect if requested (trigger WebSocket initialization and connection)
        if (options?.PreConnect == true)
        {
            var client = _webSocketClient.Value; // Initialize WebSocket client

            // Start connection in background with proper error handling
            _preConnectTask = Task.Run(async () =>
            {
                try
                {
                    await client.ConnectAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Store error for diagnostics but don't fail construction
                    PreConnectError = ex;
                    // Note: Error will be logged by WebSocketClient, we just track it here
                }
            });
        }
    }

    #region Queries (HTTP)

    /// <inheritdoc/>
    public IQueryBuilder<TResult> Query<TResult>(string functionName)
        => _queries.Query<TResult>(functionName);

    /// <inheritdoc/>
    public IBatchQueryBuilder Batch()
        => _queries.Batch();

    #endregion

    #region Mutations (HTTP)

    /// <inheritdoc/>
    public IMutationBuilder<TResult> Mutate<TResult>(string functionName)
        => new MutationBuilder<TResult>(
            _httpProvider,
            _serializer,
            functionName,
            _reactiveCache,
            InvalidateDependentQueriesAsync,
            ExecuteThroughMiddleware<TResult>,
            _syncContext);

    #endregion

    #region Actions (HTTP)

    /// <inheritdoc/>
    public IActionBuilder<TResult> Action<TResult>(string functionName)
        => new ActionBuilder<TResult>(_httpProvider, _serializer, functionName, ExecuteThroughMiddleware<TResult>);

    #endregion

    #region Subscriptions (WebSocket)

    /// <inheritdoc/>
    public IObservable<T> Observe<T>(string functionName)
    {
        EnsureWebSocketInitialized();

        // Get reactive observable from cache - this will receive SetQuery() notifications
        var cacheObservable = _reactiveCache.GetObservable<T>(functionName)
            .Where(value => value is not null)
            .Select(value => value!);

        // Create the async enumerable source from WebSocket and convert to IObservable
        // The .Do() operator writes WebSocket values to cache, which triggers cacheObservable
        var wsObservable = _webSocketClient.Value.LiveQuery<T>(functionName)
            .ToObservable()
            .Do(value => _reactiveCache.SetAndNotify(functionName, value, CacheEntrySource.Subscription))
            .Publish()
            .RefCount();

        // Return an observable that subscribes to wsObservable when someone subscribes to it
        // This keeps the WebSocket subscription alive while only emitting cacheObservable values
        // WebSocket updates write to cache via .Do(), which triggers cacheObservable
        // SetQuery() optimistic updates also write to cache, triggering cacheObservable
        return Observable.Create<T>(observer =>
        {
            // Subscribe to wsObservable to keep WebSocket subscription active
            var wsSubscription = wsObservable.Subscribe();
            // Subscribe to cacheObservable to emit values (no duplicates since wsObservable doesn't emit directly)
            var cacheSubscription = cacheObservable.Subscribe(observer);
            // Return composite subscription that disposes both
            return new CompositeDisposable(wsSubscription, cacheSubscription);
        });
    }

    /// <inheritdoc/>
    public IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        EnsureWebSocketInitialized();

        // Generate cache key with args (matches convex-js behavior)
        var cacheKey = $"{functionName}:{_serializer.Serialize(args)}";

        // Get reactive observable from cache - this will receive SetQuery() notifications
        var cacheObservable = _reactiveCache.GetObservable<T>(cacheKey)
            .Where(value => value is not null)
            .Select(value => value!);

        // Create the async enumerable source with args from WebSocket and convert to IObservable
        // The .Do() operator writes WebSocket values to cache, which triggers cacheObservable
        var wsObservable = _webSocketClient.Value.LiveQuery<T, TArgs>(functionName, args)
            .ToObservable()
            .Do(value => _reactiveCache.SetAndNotify(cacheKey, value, CacheEntrySource.Subscription))
            .Publish()
            .RefCount();

        // Return an observable that subscribes to wsObservable when someone subscribes to it
        // This keeps the WebSocket subscription alive while only emitting cacheObservable values
        // WebSocket updates write to cache via .Do(), which triggers cacheObservable
        // SetQuery() optimistic updates also write to cache, triggering cacheObservable
        return Observable.Create<T>(observer =>
        {
            // Subscribe to wsObservable to keep WebSocket subscription active
            var wsSubscription = wsObservable.Subscribe();
            // Subscribe to cacheObservable to emit values (no duplicates since wsObservable doesn't emit directly)
            var cacheSubscription = cacheObservable.Subscribe(observer);
            // Return composite subscription that disposes both
            return new CompositeDisposable(wsSubscription, cacheSubscription);
        });
    }

    #endregion

    #region Cached Values

    /// <inheritdoc/>
    public T? GetCachedValue<T>(string functionName)
        => _reactiveCache.GetCurrentValue<T>(functionName);

    /// <inheritdoc/>
    public bool TryGetCachedValue<T>(string functionName, out T? value)
        => _reactiveCache.TryGet(functionName, out value);

    #endregion Cached Values

    #region Connection Management

    // Note: Connection is now fully automatic via subscriptions.
    // The WebSocket client connects automatically when the first subscription is created
    // and reconnects automatically with exponential backoff on disconnection.

    #endregion

    #region Infrastructure

    /// <inheritdoc/>
    public TimestampManager TimestampManager { get; }

    #endregion Infrastructure

    #region IConvexClient Feature Service Implementations

    /// <inheritdoc/>
    public Features.Storage.Files.IConvexFileStorage Files => _fileStorage;

    /// <inheritdoc/>
    public Features.Storage.VectorSearch.IConvexVectorSearch VectorSearch => _vectorSearch;

    /// <inheritdoc/>
    public Features.Operational.HttpActions.IConvexHttpActions Http => _httpActions;

    /// <inheritdoc/>
    public Features.Operational.Scheduling.IConvexScheduler Scheduler => _scheduling;

    /// <inheritdoc/>
    public Features.Security.Authentication.IConvexAuthentication Auth => _authentication;

    /// <inheritdoc/>
    public IConvexCache Cache => _reactiveCache;

    /// <inheritdoc/>
    public Features.Observability.Health.IConvexHealth Health => _health;

    /// <inheritdoc/>
    public Features.Observability.Diagnostics.IConvexDiagnostics Diagnostics => _diagnostics;

    /// <inheritdoc/>
    public Features.Observability.Resilience.IConvexResilience Resilience => _resilience;

    /// <inheritdoc/>
    public Features.RealTime.Pagination.IConvexPagination Pagination => _pagination;

    #endregion IConvexClient Feature Service Implementations

    #region Cache & Dependency Tracking

    /// <inheritdoc/>
    public void DefineQueryDependency(string mutationName, params string[] invalidates) => _dependencyRegistry.DefineQueryDependency(mutationName, invalidates);

    /// <inheritdoc/>
    public async Task InvalidateQueryAsync(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName))
        {
            throw new ArgumentNullException(nameof(queryName));
        }

        _ = _reactiveCache.Remove(queryName);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task InvalidateQueriesAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        _ = _reactiveCache.RemovePattern(pattern);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates queries based on mutation dependencies.
    /// Called automatically after successful mutation execution.
    /// </summary>
    internal async Task InvalidateDependentQueriesAsync(string mutationName)
    {
        foreach (var queryPattern in _dependencyRegistry.GetQueriesToInvalidate(mutationName))
        {
            // Support both exact matches and patterns
            if (queryPattern.Contains('*') || queryPattern.Contains('?'))
            {
                _ = _reactiveCache.RemovePattern(queryPattern);
            }
            else
            {
                _ = _reactiveCache.Remove(queryPattern);
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Health Checks

    /// <summary>
    /// Gets the current health status of the Convex client connection.
    /// Provides information about connection state, active subscriptions, and overall health metrics.
    /// </summary>
    /// <returns>A task that completes with a health check result containing connection metrics and status.</returns>
    /// <remarks>
    /// This method is useful for monitoring and diagnostics. It provides a snapshot of the client's
    /// current state including connection status and subscription counts.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check client health
    /// var health = await client.GetHealthAsync();
    /// Console.WriteLine($"Connection state: {health.ConnectionState}");
    /// Console.WriteLine($"Is healthy: {health.IsHealthy}");
    ///
    /// // Use in health check endpoints
    /// app.MapGet("/health", async (IConvexClient client) =>
    /// {
    ///     var health = await client.GetHealthAsync();
    ///     return health.IsHealthy ? Results.Ok(health) : Results.StatusCode(503);
    /// });
    /// </code>
    /// </example>
    /// <seealso cref="Health"/>
    /// <seealso cref="ConnectionState"/>
    public Task<Features.Observability.Health.ConvexHealthCheck> GetHealthAsync()
    {
        // Get current connection state
        var connectionState = ConnectionState;

        // Count active subscriptions (if WebSocket is initialized)
        var activeSubscriptions = 0;
        if (_webSocketClient.IsValueCreated)
        {
            // WebSocket client would need to expose subscription count
            // For now, we'll use 0 as a placeholder
            activeSubscriptions = 0;
        }

        // Create health check from new health slice
        var healthCheck = _health.CreateHealthCheck(connectionState, activeSubscriptions);

        return Task.FromResult(healthCheck);
    }

    #endregion

    #region Connection Quality

    /// <summary>
    /// Gets the current connection quality assessment.
    /// Quality is determined by latency, packet loss, reconnections, and stability.
    /// </summary>
    /// <returns>A task that completes with detailed connection quality information including quality level, latency metrics, and stability indicators.</returns>
    /// <remarks>
    /// <para>
    /// Connection quality is assessed periodically and can be used to adapt UI behavior or trigger
    /// diagnostics. Quality levels range from Excellent to Terrible.
    /// </para>
    /// <para>
    /// Subscribe to <see cref="ConnectionQualityChanges"/> to be notified when quality changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Check current connection quality
    /// var quality = await client.GetConnectionQualityAsync();
    /// Console.WriteLine($"Quality: {quality.Quality}");
    /// Console.WriteLine($"Average latency: {quality.AverageLatencyMs}ms");
    ///
    /// // Adapt UI based on quality
    /// if (quality.Quality == ConnectionQuality.Poor || quality.Quality == ConnectionQuality.Terrible)
    /// {
    ///     ShowConnectionWarning();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ConnectionQualityChanges"/>
    /// <seealso cref="ConnectionQuality"/>
    public Task<ConnectionQualityInfo> GetConnectionQualityAsync()
    {
        var isConnected = ConnectionState == ConnectionState.Connected;
        var qualityInfo = QualityMonitor.AssessQuality(isConnected);
        return Task.FromResult(qualityInfo);
    }

    /// <summary>
    /// Periodically checks connection quality and raises ConnectionQualityChanged event if it changes.
    /// </summary>
    private void CheckAndRaiseQualityChange(object? state)
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            var isConnected = ConnectionState == ConnectionState.Connected;
            var qualityInfo = QualityMonitor.AssessQuality(isConnected);

            // Check if quality level has changed
            if (qualityInfo.Quality != _lastQuality)
            {
                _lastQuality = qualityInfo.Quality;

                // Publish quality change on UI thread if captured
                _syncContext.Post(() =>
                {
                    _connectionQualitySubject.OnNext(qualityInfo.Quality);
                });
            }
        }
        catch
        {
            // Ignore errors in quality checking - it's a best-effort monitoring feature
        }
    }

    /// <summary>
    /// Starts periodic connection quality monitoring.
    /// </summary>
    /// <param name="interval">The interval between quality checks.</param>
    private void StartQualityMonitoring(TimeSpan interval)
    {
        _qualityCheckTimer = new Timer(
            CheckAndRaiseQualityChange,
            null,
            interval,
            interval);
    }

    /// <summary>
    /// Gets the internal connection quality monitor for advanced scenarios.
    /// </summary>
    internal ConnectionQualityMonitor QualityMonitor { get; }

    #endregion

    #region Middleware

    /// <summary>
    /// Adds a middleware to the pipeline.
    /// This is an internal method used by ConvexClientBuilder.
    /// </summary>
    internal void AddMiddleware(IConvexMiddleware middleware) => _middlewarePipeline.Add(middleware);

    /// <summary>
    /// Executes a request through the middleware pipeline.
    /// This is the central integration point where all requests flow through middleware.
    /// </summary>
    private async Task<TResult> ExecuteThroughMiddleware<TResult>(
        string method,
        string functionName,
        object? args,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        // Build the middleware pipeline with the slice execution as the final handler
        var pipeline = _middlewarePipeline.Build(async request =>
        {
            // Final handler: execute the actual request using slices
            var result = method switch
            {
                "query" => args == null
                    ? await _queries.Query<TResult>(functionName).ExecuteAsync(cancellationToken)
                    : await _queries.Query<TResult>(functionName).WithArgs(args).ExecuteAsync(cancellationToken),

                "mutation" => args == null
                    ? await _mutations.Mutate<TResult>(functionName).ExecuteAsync(cancellationToken)
                    : await _mutations.Mutate<TResult>(functionName).WithArgs(args).ExecuteAsync(cancellationToken),

                "action" => args == null
                    ? await _actions.Action<TResult>(functionName).ExecuteAsync(cancellationToken)
                    : await _actions.Action<TResult>(functionName).WithArgs(args).ExecuteAsync(cancellationToken),

                _ => throw new InvalidOperationException($"Unknown method type: {method}")
            };

            return ConvexResponse.Success(result);
        });

        // Create the request wrapper
        var convexRequest = new ConvexRequest(functionName, method, args, cancellationToken)
        {
            Timeout = timeout
        };

        // Execute through the pipeline
        var response = await pipeline(convexRequest);

        // Handle the response
        return !response.IsSuccess
            ? throw response.Error ?? new InvalidOperationException("Request failed without an error")
            : response.GetValue<TResult>();
    }

    #endregion

    #region Disposal

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Stop quality monitoring timer
        _qualityCheckTimer?.Dispose();
        _qualityCheckTimer = null;

        // Unregister event handler and dispose WebSocket client (thread-safe)
        lock (_connectionStateLock)
        {
            if (_webSocketClient.IsValueCreated)
            {
                if (_connectionStateChangedHandler != null)
                {
                    _webSocketClient.Value.ConnectionStateChanged -= _connectionStateChangedHandler;
                    _connectionStateChangedHandler = null;
                }

                _webSocketClient.Value.Dispose();
            }
        }

        // Dispose HTTP client if we created it
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }

        // Dispose reactive cache (clears entries and completes subjects)
        _reactiveCache.Dispose();

        // Complete and dispose reactive subjects
        _connectionStateSubject.OnCompleted();
        _connectionStateSubject.Dispose();
        _connectionQualitySubject.OnCompleted();
        _connectionQualitySubject.Dispose();
        _authenticationStateSubject.OnCompleted();
        _authenticationStateSubject.Dispose();
    }

    #endregion

    /// <summary>
    /// Ensures the WebSocket client is initialized.
    /// </summary>
    private void EnsureWebSocketInitialized() =>
        // Access .Value to trigger lazy initialization
        _ = _webSocketClient.Value;
}
