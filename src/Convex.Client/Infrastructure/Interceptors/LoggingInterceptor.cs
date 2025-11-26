using Microsoft.Extensions.Logging;

namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Example interceptor that logs all Convex requests and responses.
/// Useful for debugging and monitoring Convex operations.
/// </summary>
/// <remarks>
/// Creates a new logging interceptor with the specified logger.
/// </remarks>
/// <param name="logger">The logger to use for logging.</param>
/// <param name="requestLogLevel">Log level for requests (default: Information).</param>
/// <param name="responseLogLevel">Log level for responses (default: Information).</param>
/// <param name="errorLogLevel">Log level for errors (default: Error).</param>
public sealed class LoggingInterceptor(
    ILogger logger,
    LogLevel requestLogLevel = LogLevel.Information,
    LogLevel responseLogLevel = LogLevel.Information,
    LogLevel errorLogLevel = LogLevel.Error) : IConvexInterceptor
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly LogLevel _requestLogLevel = requestLogLevel;
    private readonly LogLevel _responseLogLevel = responseLogLevel;
    private readonly LogLevel _errorLogLevel = errorLogLevel;

    /// <inheritdoc/>
    public Task<ConvexRequestContext> BeforeRequestAsync(
        ConvexRequestContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.Log(
            _requestLogLevel,
            "Convex {RequestType} {FunctionName} - Starting (RequestId: {RequestId})",
            context.RequestType,
            context.FunctionName,
            context.RequestId);

        // Store start time in metadata for duration calculation
        context.Metadata["StartTime"] = DateTimeOffset.UtcNow;

        return Task.FromResult(context);
    }

    /// <inheritdoc/>
    public Task<ConvexResponseContext> AfterResponseAsync(
        ConvexResponseContext context,
        CancellationToken cancellationToken = default)
    {
        var durationMs = context.Duration.TotalMilliseconds;

        _logger.Log(
            _responseLogLevel,
            "Convex {RequestType} {FunctionName} - Completed in {DurationMs:F2}ms (RequestId: {RequestId}, StatusCode: {StatusCode})",
            context.Request.RequestType,
            context.Request.FunctionName,
            durationMs,
            context.Request.RequestId,
            context.StatusCode);

        return Task.FromResult(context);
    }

    /// <inheritdoc/>
    public Task OnErrorAsync(
        ConvexErrorContext context,
        CancellationToken cancellationToken = default)
    {
        var durationMs = context.Duration.TotalMilliseconds;

        _logger.Log(
            _errorLogLevel,
            context.Exception,
            "Convex {RequestType} {FunctionName} - Failed after {DurationMs:F2}ms (RequestId: {RequestId}): {ErrorMessage}",
            context.Request.RequestType,
            context.Request.FunctionName,
            durationMs,
            context.Request.RequestId,
            context.Exception.Message);

        return Task.CompletedTask;
    }
}
