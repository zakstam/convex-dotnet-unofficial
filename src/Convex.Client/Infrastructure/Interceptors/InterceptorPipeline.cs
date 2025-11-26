using Microsoft.Extensions.Logging;

namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Executes interceptors in a pipeline for Convex requests.
/// </summary>
internal sealed class InterceptorPipeline(IReadOnlyList<IConvexInterceptor> interceptors, ILogger? logger = null)
{
    private readonly IReadOnlyList<IConvexInterceptor> _interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));
    private readonly ILogger? _logger = logger;

    /// <summary>
    /// Executes BeforeRequest hooks for all interceptors in order.
    /// </summary>
    public async Task<ConvexRequestContext> ExecuteBeforeRequestAsync(
        ConvexRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var currentContext = context;

        foreach (var interceptor in _interceptors)
        {
            try
            {
                currentContext = await interceptor.BeforeRequestAsync(currentContext, cancellationToken);
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

        foreach (var interceptor in _interceptors)
        {
            try
            {
                currentContext = await interceptor.AfterResponseAsync(currentContext, cancellationToken);
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
        foreach (var interceptor in _interceptors)
        {
            try
            {
                await interceptor.OnErrorAsync(context, cancellationToken);
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
