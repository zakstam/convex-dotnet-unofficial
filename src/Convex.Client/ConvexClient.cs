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
using Convex.Client.Features.DataAccess.Mutations;
using Convex.Client.Features.DataAccess.Queries;

namespace Convex.Client;

/// <summary>
/// Unified Convex client that provides both HTTP operations (queries/mutations)
/// and WebSocket operations (real-time subscriptions) in a single, easy-to-use interface.
/// </summary>
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
    private readonly Dictionary<string, object?> _cachedValues;
    private readonly object _connectionStateLock = new();
    private readonly QueryDependencyRegistry _dependencyRegistry;
    private readonly MiddlewarePipeline _middlewarePipeline = new();
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
    public Exception? PreConnectError { get; private set; }


    /// <summary>
    /// Ensures the WebSocket connection is established and ready.
    /// If PreConnect was enabled, this will wait for the pre-connection to complete.
    /// If PreConnect was not enabled, this will establish the connection now.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the connection is established.</returns>
    /// <exception cref="InvalidOperationException">Thrown if PreConnect failed.</exception>
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
    /// </summary>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <exception cref="ArgumentException">Thrown when deploymentUrl is null or whitespace.</exception>
    public ConvexClient(string deploymentUrl) : this(deploymentUrl, null)
    {
    }

    /// <summary>
    /// Creates a new ConvexClient with the specified deployment URL and options.
    /// </summary>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <param name="options">Optional client configuration.</param>
    /// <exception cref="ArgumentException">Thrown when deploymentUrl is null or whitespace.</exception>
    public ConvexClient(string deploymentUrl, ConvexClientOptions? options)
    {
        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or empty.", nameof(deploymentUrl));
        }

        options?.Validate();

        DeploymentUrl = deploymentUrl;
        _cachedValues = [];
        QualityMonitor = new ConnectionQualityMonitor();
        CachingSlice = new Features.DataAccess.Caching.CachingSlice();
        HealthSlice = new Features.Observability.Health.HealthSlice();
        DiagnosticsSlice = new Features.Observability.Diagnostics.DiagnosticsSlice();
        _dependencyRegistry = new QueryDependencyRegistry();

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
        _httpProvider = new DefaultHttpClientProvider(httpClient, deploymentUrl);
        _serializer = new DefaultConvexSerializer();
        var logger = options?.Logger;
        var enableDebugLogging = options?.EnableDebugLogging ?? false;
        ResilienceSlice = new Features.Observability.Resilience.ResilienceSlice(logger, enableDebugLogging);
        _queries = new QueriesSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _mutations = new MutationsSlice(_httpProvider, _serializer, CachingSlice, InvalidateDependentQueriesAsync, _syncContext, logger, enableDebugLogging);
        _actions = new ActionsSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        TimestampManager = new TimestampManager(httpClient, deploymentUrl);
        FileStorageSlice = new Features.Storage.Files.FileStorageSlice(_httpProvider, _serializer, httpClient, logger, enableDebugLogging);
        VectorSearchSlice = new Features.Storage.VectorSearch.VectorSearchSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        HttpActionsSlice = new Features.Operational.HttpActions.HttpActionsSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        SchedulingSlice = new Features.Operational.Scheduling.SchedulingSlice(_httpProvider, _serializer, logger, enableDebugLogging);
        _pagination = new Features.RealTime.Pagination.PaginationSlice(_httpProvider, _serializer, logger, enableDebugLogging);

        // Initialize authentication slice with logger and debug logging
        AuthenticationSlice = new Features.Security.Authentication.AuthenticationSlice(logger, enableDebugLogging);

        // Wire up authentication to HTTP provider (slice coordination through facade)
        if (_httpProvider is DefaultHttpClientProvider defaultProvider)
        {
            defaultProvider.SetAuthHeadersProvider(ct => AuthenticationSlice.GetAuthHeadersAsync(ct));
        }

        // Lazy initialization of WebSocket client (only connects on first subscription)
        // Pass syncContext, reconnectionPolicy, and logger directly to WebSocketClient
        var reconnectionPolicy = options?.ReconnectionPolicy ?? ReconnectionPolicy.Default();
        _webSocketClient = new Lazy<ConvexWebSocketClient>(() =>
        {
            var client = new ConvexWebSocketClient(deploymentUrl, _syncContext, reconnectionPolicy, logger);

            // Wire up authentication to WebSocket client (slice coordination through facade)
            client.SetAuthTokenProvider(ct => AuthenticationSlice.GetAuthTokenAsync(ct));

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
            CachingSlice,
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

        // Create the async enumerable source from WebSocket and convert to IObservable
        var source = _webSocketClient.Value.LiveQuery<T>(functionName);
        var observable = source.ToObservable();

        // Track values for caching
        return observable.Do(value => _cachedValues[functionName] = value);
    }

    /// <inheritdoc/>
    public IObservable<T> Observe<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        EnsureWebSocketInitialized();

        // Create the async enumerable source with args from WebSocket and convert to IObservable
        var source = _webSocketClient.Value.LiveQuery<T, TArgs>(functionName, args);
        var observable = source.ToObservable();

        // Track values for caching (use function name with args hash for cache key)
        // Use ConvexSerializer to ensure deterministic key ordering (matches convex-js behavior)
        var cacheKey = $"{functionName}:{_serializer.Serialize(args)}";
        return observable.Do(value => _cachedValues[cacheKey] = value);
    }

    #endregion

    #region Cached Values

    /// <inheritdoc/>
    public T? GetCachedValue<T>(string functionName)
        => _cachedValues.TryGetValue(functionName, out var cached) && cached is T typedValue
            ? typedValue
            : default;

    /// <inheritdoc/>
    public bool TryGetCachedValue<T>(string functionName, out T? value)
    {
        if (_cachedValues.TryGetValue(functionName, out var cached) && cached is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    #endregion

    #region Connection Management

    // Note: Connection is now fully automatic via subscriptions.
    // The WebSocket client connects automatically when the first subscription is created
    // and reconnects automatically with exponential backoff on disconnection.

    #endregion

    #region Feature Access

    /// <summary>
    /// Gets the FileStorage slice for file upload and download operations.
    /// </summary>
    public Features.Storage.Files.FileStorageSlice FileStorageSlice { get; }

    /// <summary>
    /// Gets the VectorSearch slice for vector similarity search operations.
    /// </summary>
    public Features.Storage.VectorSearch.VectorSearchSlice VectorSearchSlice { get; }

    /// <summary>
    /// Gets the HTTP actions slice (migrated to vertical slice architecture).
    /// </summary>
    public Features.Operational.HttpActions.HttpActionsSlice HttpActionsSlice { get; }

    /// <summary>
    /// Gets the scheduling slice (migrated to vertical slice architecture).
    /// </summary>
    public Features.Operational.Scheduling.SchedulingSlice SchedulingSlice { get; }

    /// <summary>
    /// Gets the new pagination slice (migrated to vertical slice architecture).
    /// Provides cursor-based pagination for Convex queries.
    /// </summary>
    public Features.RealTime.Pagination.IConvexPagination PaginationSlice => _pagination;

    /// <summary>
    /// Gets the new caching slice (migrated to vertical slice architecture).
    /// Provides in-memory caching with optimistic updates and pattern-based invalidation.
    /// Note: The QueryCache property above is kept for backward compatibility.
    /// </summary>
    public Features.DataAccess.Caching.CachingSlice CachingSlice { get; }

    /// <summary>
    /// Gets the Authentication slice for managing authentication state and tokens.
    /// </summary>
    public Features.Security.Authentication.AuthenticationSlice AuthenticationSlice { get; }

    /// <summary>
    /// Gets the Health slice for monitoring connection health and metrics.
    /// </summary>
    public Features.Observability.Health.HealthSlice HealthSlice { get; }

    /// <summary>
    /// Gets the Diagnostics slice for performance tracking and disconnection monitoring.
    /// </summary>
    public Features.Observability.Diagnostics.DiagnosticsSlice DiagnosticsSlice { get; }

    /// <summary>
    /// Gets the Resilience slice for retry and circuit breaker patterns.
    /// </summary>
    public Features.Observability.Resilience.ResilienceSlice ResilienceSlice { get; }

    /// <inheritdoc/>
    public TimestampManager TimestampManager { get; }

    #endregion

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

        _ = CachingSlice.Remove(queryName);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task InvalidateQueriesAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        _ = CachingSlice.RemovePattern(pattern);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Invalidates queries based on mutation dependencies.
    /// Called automatically after successful mutation execution.
    /// </summary>
    internal async Task InvalidateDependentQueriesAsync(string mutationName)
    {
        var queriesToInvalidate = _dependencyRegistry.GetQueriesToInvalidate(mutationName);

        foreach (var queryPattern in queriesToInvalidate)
        {
            // Support both exact matches and patterns
            if (queryPattern.Contains('*') || queryPattern.Contains('?'))
            {
                _ = CachingSlice.RemovePattern(queryPattern);
            }
            else
            {
                _ = CachingSlice.Remove(queryPattern);
            }
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Health Checks

    /// <summary>
    /// Gets the current health status of the Convex client connection.
    /// </summary>
    /// <returns>A health check result with connection metrics and status.</returns>
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
        var healthCheck = HealthSlice.CreateHealthCheck(connectionState, activeSubscriptions);

        return Task.FromResult(healthCheck);
    }

    #endregion

    #region Connection Quality

    /// <summary>
    /// Gets the current connection quality assessment.
    /// Quality is determined by latency, packet loss, reconnections, and stability.
    /// </summary>
    /// <returns>Detailed connection quality information.</returns>
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

        // Clear cached values
        _cachedValues.Clear();

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
