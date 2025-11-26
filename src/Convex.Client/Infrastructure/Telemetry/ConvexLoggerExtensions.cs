using Microsoft.Extensions.Logging;

namespace Convex.Client.Infrastructure.Telemetry;

/// <summary>
/// Extension methods for structured logging of Convex operations.
/// </summary>
public static class ConvexLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogRequestStarted =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            ConvexTelemetry.EventIds.RequestStarted,
            "Starting {FunctionType} request to {FunctionName} with requestId {RequestId}");

    private static readonly Action<ILogger, string, string, string, double, Exception?> LogRequestCompleted =
        LoggerMessage.Define<string, string, string, double>(
            LogLevel.Information,
            ConvexTelemetry.EventIds.RequestCompleted,
            "Completed {FunctionType} request to {FunctionName} with requestId {RequestId} in {DurationMs}ms");

    private static readonly Action<ILogger, string, string, string, double, string, Exception?> LogRequestFailed =
        LoggerMessage.Define<string, string, string, double, string>(
            LogLevel.Warning,
            ConvexTelemetry.EventIds.RequestFailed,
            "Failed {FunctionType} request to {FunctionName} with requestId {RequestId} after {DurationMs}ms: {ErrorCode}");

    private static readonly Action<ILogger, string, string, int, double, string, Exception?> LogRequestRetrying =
        LoggerMessage.Define<string, string, int, double, string>(
            LogLevel.Warning,
            ConvexTelemetry.EventIds.RequestRetrying,
            "Retrying {FunctionType} request to {FunctionName}, attempt {Attempt} after {DelayMs}ms: {Reason}");

    private static readonly Action<ILogger, string, string, Exception?> LogConnectionEstablished =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            ConvexTelemetry.EventIds.ConnectionEstablished,
            "WebSocket connection established to {Url} with sessionId {SessionId}");

    private static readonly Action<ILogger, string, string, Exception?> LogConnectionLost =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            ConvexTelemetry.EventIds.ConnectionLost,
            "WebSocket connection lost to {Url}: {Reason}");

    private static readonly Action<ILogger, double, bool, Exception?> LogAuthTokenRefreshed =
        LoggerMessage.Define<double, bool>(
            LogLevel.Information,
            ConvexTelemetry.EventIds.AuthTokenRefreshed,
            "Authentication token refresh completed in {DurationMs}ms, success: {Success}");

    private static readonly Action<ILogger, string, int, Exception?> LogCircuitBreakerOpened =
        LoggerMessage.Define<string, int>(
            LogLevel.Error,
            ConvexTelemetry.EventIds.CircuitBreakerOpened,
            "Circuit breaker opened for {ServiceName} after {FailureCount} failures");

    private static readonly Action<ILogger, string, string, Exception?> LogOptimisticUpdateApplied =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            ConvexTelemetry.EventIds.OptimisticUpdateApplied,
            "Applied optimistic update for {FunctionName} with queryId {QueryId}");

    public static void ConvexRequestStarted(this ILogger logger, string functionType, string functionName, string requestId)
        => LogRequestStarted(logger, functionType, functionName, requestId, null);

    public static void ConvexRequestCompleted(this ILogger logger, string functionType, string functionName, string requestId, double durationMs)
        => LogRequestCompleted(logger, functionType, functionName, requestId, durationMs, null);

    public static void ConvexRequestFailed(this ILogger logger, string functionType, string functionName, string requestId, double durationMs, string errorCode, Exception? exception = null)
        => LogRequestFailed(logger, functionType, functionName, requestId, durationMs, errorCode, exception);

    public static void ConvexRequestRetrying(this ILogger logger, string functionType, string functionName, int attempt, double delayMs, string reason, Exception? exception = null)
        => LogRequestRetrying(logger, functionType, functionName, attempt, delayMs, reason, exception);

    public static void ConvexConnectionEstablished(this ILogger logger, string url, string sessionId)
        => LogConnectionEstablished(logger, url, sessionId, null);

    public static void ConvexConnectionLost(this ILogger logger, string url, string reason, Exception? exception = null)
        => LogConnectionLost(logger, url, reason, exception);

    public static void ConvexAuthTokenRefreshed(this ILogger logger, double durationMs, bool success, Exception? exception = null)
        => LogAuthTokenRefreshed(logger, durationMs, success, exception);

    public static void ConvexCircuitBreakerOpened(this ILogger logger, string serviceName, int failureCount, Exception? exception = null)
        => LogCircuitBreakerOpened(logger, serviceName, failureCount, exception);

    public static void ConvexOptimisticUpdateApplied(this ILogger logger, string functionName, string queryId)
        => LogOptimisticUpdateApplied(logger, functionName, queryId, null);

    /// <summary>
    /// Checks if debug logging should be enabled.
    /// Debug logging is enabled when both a logger is configured and the EnableDebugLogging flag is true.
    /// </summary>
    /// <param name="logger">The logger instance (may be null).</param>
    /// <param name="enableDebugLogging">Whether debug logging is enabled in options.</param>
    /// <returns>True if debug logging should be enabled, false otherwise.</returns>
    public static bool IsDebugLoggingEnabled(ILogger? logger, bool enableDebugLogging) => enableDebugLogging && logger != null && logger.IsEnabled(LogLevel.Debug);
}
