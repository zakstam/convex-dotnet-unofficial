using Microsoft.Extensions.Logging;
using Convex.Client.Infrastructure.Telemetry;

namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Executes interceptors in a pipeline for Convex requests.
/// </summary>
internal sealed class InterceptorPipeline(IReadOnlyList<IConvexInterceptor> interceptors, ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly IReadOnlyList<IConvexInterceptor> _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    /// <summary>
    /// Executes BeforeRequest hooks for all interceptors in order.
    /// </summary>
    public async Task<ConvexRequestContext> ExecuteBeforeRequestAsync(
        ConvexRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var currentContext = context;

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Interceptor] Executing BeforeRequest: InterceptorCount={Count}, RequestType={RequestType}, Function={FunctionName}",
                _interceptors.Count, context.RequestType, context.FunctionName);
        }

        foreach (var interceptor in _interceptors)
        {
            try
            {
                currentContext = await interceptor.BeforeRequestAsync(currentContext, cancellationToken);

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Interceptor] BeforeRequest executed: Interceptor={InterceptorType}", interceptor.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - interceptors should not break the request pipeline
                _logger?.LogWarning(ex,
                    "Interceptor {InterceptorType} failed in BeforeRequest for {RequestType} {FunctionName}",
                    interceptor.GetType().Name,
                    context.RequestType,
                    context.FunctionName);
            }
        }

        return currentContext;
    }

    /// <summary>
    /// Executes AfterResponse hooks for all interceptors in order.
    /// </summary>
    public async Task<ConvexResponseContext> ExecuteAfterResponseAsync(
        ConvexResponseContext context,
        CancellationToken cancellationToken = default)
    {
        var currentContext = context;

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Interceptor] Executing AfterResponse: InterceptorCount={Count}, RequestType={RequestType}, Function={FunctionName}",
                _interceptors.Count, context.Request.RequestType, context.Request.FunctionName);
        }

        foreach (var interceptor in _interceptors)
        {
            try
            {
                currentContext = await interceptor.AfterResponseAsync(currentContext, cancellationToken);

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Interceptor] AfterResponse executed: Interceptor={InterceptorType}", interceptor.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - interceptors should not break the request pipeline
                _logger?.LogWarning(ex,
                    "Interceptor {InterceptorType} failed in AfterResponse for {RequestType} {FunctionName}",
                    interceptor.GetType().Name,
                    context.Request.RequestType,
                    context.Request.FunctionName);
            }
        }

        return currentContext;
    }

    /// <summary>
    /// Executes OnError hooks for all interceptors in order.
    /// </summary>
    public async Task ExecuteOnErrorAsync(
        ConvexErrorContext context,
        CancellationToken cancellationToken = default)
    {
        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Interceptor] Executing OnError: InterceptorCount={Count}, RequestType={RequestType}, Function={FunctionName}, ErrorType={ErrorType}",
                _interceptors.Count, context.Request.RequestType, context.Request.FunctionName, context.Exception.GetType().Name);
        }

        foreach (var interceptor in _interceptors)
        {
            try
            {
                await interceptor.OnErrorAsync(context, cancellationToken);

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Interceptor] OnError executed: Interceptor={InterceptorType}", interceptor.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - error interceptors should never break the pipeline
                _logger?.LogWarning(ex,
                    "Interceptor {InterceptorType} failed in OnError for {RequestType} {FunctionName}",
                    interceptor.GetType().Name,
                    context.Request.RequestType,
                    context.Request.FunctionName);
            }
        }
    }

    /// <summary>
    /// Gets whether there are any interceptors registered.
    /// </summary>
    public bool HasInterceptors => _interceptors.Count > 0;
}
