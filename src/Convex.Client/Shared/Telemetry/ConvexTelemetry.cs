using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Convex.Client.Shared.Connection;
using Convex.Client.Shared.ErrorHandling;

namespace Convex.Client.Shared.Telemetry;

/// <summary>
/// Comprehensive telemetry system for Convex client operations.
/// Provides structured logging, metrics collection, and distributed tracing.
/// </summary>
public static class ConvexTelemetry
{
    /// <summary>
    /// Activity source for distributed tracing.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new("Convex.Client");

    /// <summary>
    /// Meter for collecting metrics.
    /// </summary>
    public static readonly Meter Meter = new("Convex.Client");

    #region Metrics

    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "convex_requests_total",
        description: "Total number of Convex requests");

    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "convex_request_duration_ms",
        description: "Duration of Convex requests in milliseconds");

    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
        "convex_errors_total",
        description: "Total number of Convex errors");

    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>(
        "convex_retries_total",
        description: "Total number of retry attempts");

    private static int _activeConnectionsCount = 0;

    private static readonly ObservableGauge<int> ActiveConnections = Meter.CreateObservableGauge<int>(
        name: "convex_active_connections",
        unit: null,
        description: "Number of active WebSocket connections",
        observeValue: () => _activeConnectionsCount);

    private static readonly Counter<long> ConnectionEvents = Meter.CreateCounter<long>(
        "convex_connection_events_total",
        description: "Total number of connection state changes");

    private static readonly Histogram<double> AuthTokenRefresh = Meter.CreateHistogram<double>(
        "convex_auth_refresh_duration_ms",
        description: "Duration of authentication token refresh operations");

    #endregion

    #region Activity Creation

    /// <summary>
    /// Starts a new activity for a Convex query operation.
    /// </summary>
    /// <param name="functionName">The name of the function being called.</param>
    /// <param name="hasArgs">Whether arguments were provided.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartQuery(string functionName, bool hasArgs = false)
    {
        var activity = ActivitySource.StartActivity("convex.query");
        _ = (activity?.SetTag("convex.function.name", functionName));
        _ = (activity?.SetTag("convex.function.type", "query"));
        _ = (activity?.SetTag("convex.has_args", hasArgs));
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a Convex mutation operation.
    /// </summary>
    /// <param name="functionName">The name of the function being called.</param>
    /// <param name="hasArgs">Whether arguments were provided.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartMutation(string functionName, bool hasArgs = false)
    {
        var activity = ActivitySource.StartActivity("convex.mutation");
        _ = (activity?.SetTag("convex.function.name", functionName));
        _ = (activity?.SetTag("convex.function.type", "mutation"));
        _ = (activity?.SetTag("convex.has_args", hasArgs));
        return activity;
    }

    /// <summary>
    /// Starts a new activity for a Convex action operation.
    /// </summary>
    /// <param name="functionName">The name of the function being called.</param>
    /// <param name="hasArgs">Whether arguments were provided.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartAction(string functionName, bool hasArgs = false)
    {
        var activity = ActivitySource.StartActivity("convex.action");
        _ = (activity?.SetTag("convex.function.name", functionName));
        _ = (activity?.SetTag("convex.function.type", "action"));
        _ = (activity?.SetTag("convex.has_args", hasArgs));
        return activity;
    }

    /// <summary>
    /// Starts a new activity for WebSocket connection operations.
    /// </summary>
    /// <param name="url">The WebSocket URL.</param>
    /// <returns>The started activity, or null if tracing is disabled.</returns>
    public static Activity? StartWebSocketConnection(string url)
    {
        var activity = ActivitySource.StartActivity("convex.websocket.connect");
        _ = (activity?.SetTag("convex.websocket.url", SanitizeUrl(url)));
        return activity;
    }

    #endregion

    #region Metrics Recording

    /// <summary>
    /// Records a completed request operation.
    /// </summary>
    /// <param name="functionType">The type of function (query, mutation, action).</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="errorCode">The error code if the operation failed.</param>
    public static void RecordRequest(string functionType, string functionName, double durationMs,
        bool success, string? errorCode = null)
    {
        var tags = new TagList
        {
            { "function_type", functionType },
            { "function_name", functionName },
            { "success", success.ToString().ToLowerInvariant() }
        };

        if (errorCode != null)
        {
            tags.Add("error_code", errorCode);
        }

        RequestCounter.Add(1, tags);
        RequestDuration.Record(durationMs, tags);

        if (!success)
        {
            ErrorCounter.Add(1, tags);
        }
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    /// <param name="functionType">The type of function being retried.</param>
    /// <param name="attempt">The retry attempt number.</param>
    /// <param name="reason">The reason for the retry.</param>
    public static void RecordRetry(string functionType, int attempt, string reason)
    {
        var retryTags = new TagList
        {
            { "function_type", functionType },
            { "attempt", attempt.ToString() },
            { "reason", reason }
        };
        RetryCounter.Add(1, retryTags);
    }

    /// <summary>
    /// Records a connection state change.
    /// </summary>
    /// <param name="from">The previous connection state.</param>
    /// <param name="to">The new connection state.</param>
    /// <param name="reason">The reason for the state change.</param>
    public static void RecordConnectionStateChange(ConnectionState from, ConnectionState to, string? reason = null)
    {
        var connectionTags = new TagList
        {
            { "from_state", from.ToString().ToLowerInvariant() },
            { "to_state", to.ToString().ToLowerInvariant() },
            { "reason", reason ?? "unknown" }
        };
        ConnectionEvents.Add(1, connectionTags);
    }

    /// <summary>
    /// Records authentication token refresh operation.
    /// </summary>
    /// <param name="durationMs">The duration of the refresh operation in milliseconds.</param>
    /// <param name="success">Whether the refresh was successful.</param>
    public static void RecordAuthTokenRefresh(double durationMs, bool success)
    {
        var authTags = new TagList
        {
            { "success", success.ToString().ToLowerInvariant() }
        };
        AuthTokenRefresh.Record(durationMs, authTags);
    }

    /// <summary>
    /// Updates the active connection count.
    /// </summary>
    /// <param name="count">The current number of active connections.</param>
    public static void UpdateActiveConnections(int count) => _activeConnectionsCount = count;

    #endregion

    #region Structured Logging

    /// <summary>
    /// Log event IDs for Convex operations.
    /// </summary>
    public static class EventIds
    {
        public static readonly EventId RequestStarted = new(1000, "RequestStarted");
        public static readonly EventId RequestCompleted = new(1001, "RequestCompleted");
        public static readonly EventId RequestFailed = new(1002, "RequestFailed");
        public static readonly EventId RequestRetrying = new(1003, "RequestRetrying");

        public static readonly EventId ConnectionEstablished = new(2000, "ConnectionEstablished");
        public static readonly EventId ConnectionLost = new(2001, "ConnectionLost");
        public static readonly EventId ConnectionRetrying = new(2002, "ConnectionRetrying");

        public static readonly EventId AuthTokenRefreshed = new(3000, "AuthTokenRefreshed");
        public static readonly EventId AuthTokenExpired = new(3001, "AuthTokenExpired");

        public static readonly EventId CircuitBreakerOpened = new(4000, "CircuitBreakerOpened");
        public static readonly EventId CircuitBreakerClosed = new(4001, "CircuitBreakerClosed");

        public static readonly EventId OptimisticUpdateApplied = new(5000, "OptimisticUpdateApplied");
        public static readonly EventId OptimisticUpdateRolledBack = new(5001, "OptimisticUpdateRolledBack");
    }


    #endregion

    #region Performance Tracking

    /// <summary>
    /// Measures the duration of an operation and records telemetry.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to measure.</param>
    /// <param name="operationName">The name of the operation for telemetry.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <param name="tags">Additional tags for metrics.</param>
    /// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> MeasureAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        ILogger? logger = null,
        TagList? tags = null,
        bool enableDebugLogging = false)
    {
        using var activity = ActivitySource.StartActivity(operationName);
        var stopwatch = Stopwatch.StartNew();
        var operationTags = tags ?? new TagList();
        operationTags.Add("operation", operationName);

        try
        {
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(logger, enableDebugLogging))
            {
                logger!.LogDebug("Starting operation: {OperationName}", operationName);
            }

            var result = await operation();

            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            // Record success metrics
            operationTags.Add("success", "true");
            RequestDuration.Record(durationMs, operationTags);

            _ = (activity?.SetStatus(ActivityStatusCode.Ok));
            _ = (activity?.SetTag("duration_ms", durationMs));

            logger?.LogInformation("Completed operation: {OperationName} in {DurationMs}ms",
                operationName, durationMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = stopwatch.Elapsed.TotalMilliseconds;

            // Record failure metrics
            operationTags.Add("success", "false");
            operationTags.Add("error_type", ex.GetType().Name);
            RequestDuration.Record(durationMs, operationTags);
            ErrorCounter.Add(1, operationTags);

            _ = (activity?.SetStatus(ActivityStatusCode.Error, ex.Message));
            _ = (activity?.SetTag("duration_ms", durationMs));
            _ = (activity?.SetTag("exception.type", ex.GetType().FullName));
            _ = (activity?.SetTag("exception.message", ex.Message));

            logger?.LogError(ex, "Failed operation: {OperationName} after {DurationMs}ms",
                operationName, durationMs);

            throw;
        }
    }

    #endregion

    #region Helper Methods

    private static string SanitizeUrl(string url)
    {
        // Remove sensitive information from URLs for logging
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}{uri.AbsolutePath}";
    }

    /// <summary>
    /// Safely extracts error code from exceptions for telemetry.
    /// </summary>
    /// <param name="exception">The exception to extract error code from.</param>
    /// <returns>The error code, or the exception type name if no specific code is available.</returns>
    public static string ExtractErrorCode(Exception exception)
    {
        return exception switch
        {
            ConvexException convexEx when !string.IsNullOrEmpty(convexEx.ErrorCode) => convexEx.ErrorCode,
            ConvexFunctionException => "FUNCTION_ERROR",
            ConvexArgumentException => "ARGUMENT_ERROR",
            ConvexNetworkException networkEx => $"NETWORK_{networkEx.ErrorType}",
            ConvexAuthenticationException => "AUTH_ERROR",
            ConvexRateLimitException => "RATE_LIMIT",
            ConvexCircuitBreakerException => "CIRCUIT_BREAKER",
            TaskCanceledException => "TIMEOUT",
            HttpRequestException => "HTTP_ERROR",
            _ => exception.GetType().Name.ToUpperInvariant()
        };
    }

    /// <summary>
    /// Creates a scope for request telemetry that automatically tracks duration and outcome.
    /// </summary>
    /// <param name="functionType">The type of function being called.</param>
    /// <param name="functionName">The name of the function being called.</param>
    /// <param name="requestId">The unique request identifier.</param>
    /// <param name="logger">Optional logger for structured logging.</param>
    /// <returns>A disposable telemetry scope.</returns>
    public static ConvexTelemetryScope CreateRequestScope(string functionType, string functionName,
        string requestId, ILogger? logger = null) => new ConvexTelemetryScope(functionType, functionName, requestId, logger);

    #endregion
}

/// <summary>
/// Disposable telemetry scope that automatically records request metrics and logging.
/// </summary>
public sealed class ConvexTelemetryScope : IDisposable
{
    private readonly string _functionType;
    private readonly string _functionName;
    private readonly string _requestId;
    private readonly ILogger? _logger;
    private readonly Stopwatch _stopwatch;
    private readonly Activity? _activity;

    private bool _disposed;
    private bool _success;
    private Exception? _exception;

    internal ConvexTelemetryScope(string functionType, string functionName, string requestId, ILogger? logger)
    {
        _functionType = functionType;
        _functionName = functionName;
        _requestId = requestId;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();

        _activity = functionType switch
        {
            "query" => ConvexTelemetry.StartQuery(functionName),
            "mutation" => ConvexTelemetry.StartMutation(functionName),
            "action" => ConvexTelemetry.StartAction(functionName),
            _ => ConvexTelemetry.ActivitySource.StartActivity($"convex.{functionType}")
        };

        _logger?.ConvexRequestStarted(_functionType, _functionName, _requestId);
    }

    /// <summary>
    /// Marks the operation as successful.
    /// </summary>
    public void RecordSuccess() => _success = true;

    /// <summary>
    /// Marks the operation as failed with the given exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public void RecordFailure(Exception exception) => _exception = exception;

    public void Dispose()
    {
        if (_disposed) return;

        _stopwatch.Stop();
        var durationMs = _stopwatch.Elapsed.TotalMilliseconds;

        if (_success)
        {
            _logger?.ConvexRequestCompleted(_functionType, _functionName, _requestId, durationMs);
            _ = (_activity?.SetStatus(ActivityStatusCode.Ok));

            ConvexTelemetry.RecordRequest(_functionType, _functionName, durationMs, true);
        }
        else
        {
            var errorCode = _exception != null ? ConvexTelemetry.ExtractErrorCode(_exception) : "UNKNOWN";
            _logger?.ConvexRequestFailed(_functionType, _functionName, _requestId, durationMs, errorCode, _exception);
            _ = (_activity?.SetStatus(ActivityStatusCode.Error, _exception?.Message ?? "Unknown error"));

            ConvexTelemetry.RecordRequest(_functionType, _functionName, durationMs, false, errorCode);
        }

        _activity?.Dispose();
        _disposed = true;
    }
}
