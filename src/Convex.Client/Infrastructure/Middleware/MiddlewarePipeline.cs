using Microsoft.Extensions.Logging;
using Convex.Client.Infrastructure.Telemetry;
using System.Diagnostics;

namespace Convex.Client.Infrastructure.Middleware;

/// <summary>
/// Manages the execution of middleware in a pipeline.
/// </summary>
internal sealed class MiddlewarePipeline(ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly List<IConvexMiddleware> _middleware = [];
    private ConvexRequestDelegate? _compiledPipeline;
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    /// <summary>
    /// Adds middleware to the end of the pipeline.
    /// </summary>
    public void Add(IConvexMiddleware middleware)
    {
        if (middleware == null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        _middleware.Add(middleware);
        _compiledPipeline = null; // Invalidate compiled pipeline

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Middleware] Added middleware: Type={MiddlewareType}, TotalCount={Count}",
                middleware.GetType().Name, _middleware.Count);
        }
    }

    /// <summary>
    /// Builds the middleware pipeline with the given final handler.
    /// </summary>
    /// <param name="finalHandler">The final handler to execute after all middleware.</param>
    /// <returns>A delegate that executes the entire pipeline.</returns>
    public ConvexRequestDelegate Build(ConvexRequestDelegate finalHandler)
    {
        if (finalHandler == null)
        {
            throw new ArgumentNullException(nameof(finalHandler));
        }

        // If no middleware, just return the final handler
        if (_middleware.Count == 0)
        {
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[Middleware] Pipeline built with no middleware");
            }
            return finalHandler;
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Middleware] Building pipeline: MiddlewareCount={Count}",
                _middleware.Count);
        }

        // Build the pipeline from the end backwards
        var next = finalHandler;

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var middleware = _middleware[i];
            var currentNext = next;
            next = request => middleware.InvokeAsync(request, currentNext);
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Middleware] Pipeline built successfully: MiddlewareCount={Count}",
                _middleware.Count);
        }

        return next;
    }

    /// <summary>
    /// Executes the pipeline with the given request and final handler.
    /// </summary>
    public async Task<ConvexResponse> ExecuteAsync(ConvexRequest request, ConvexRequestDelegate finalHandler)
    {
        var stopwatch = Stopwatch.StartNew();
        var usingCache = _compiledPipeline != null;

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Middleware] Executing pipeline: Method={Method}, Function={FunctionName}, UsingCachedPipeline={UsingCache}",
                request.Method, request.FunctionName, usingCache);
        }

        // Build or use cached pipeline
        if (_compiledPipeline == null)
        {
            _compiledPipeline = Build(finalHandler);
        }

        var response = await _compiledPipeline(request);
        stopwatch.Stop();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Middleware] Pipeline executed: Function={FunctionName}, Duration={DurationMs}ms",
                request.FunctionName, stopwatch.Elapsed.TotalMilliseconds);
        }

        return response;
    }

    /// <summary>
    /// Gets the number of middleware in the pipeline.
    /// </summary>
    public int Count => _middleware.Count;

    /// <summary>
    /// Clears all middleware from the pipeline.
    /// </summary>
    public void Clear()
    {
        _middleware.Clear();
        _compiledPipeline = null;
    }
}
