namespace Convex.Client.Shared.Interceptors;

/// <summary>
/// Interceptor interface for observing and transforming Convex requests and responses.
/// Interceptors can be used for logging, metrics collection, request transformation,
/// custom error handling, and other cross-cutting concerns.
/// </summary>
public interface IConvexInterceptor
{
    /// <summary>
    /// Called before a request is sent to the Convex backend.
    /// Can modify the request or add metadata.
    /// </summary>
    /// <param name="context">The request context containing request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The potentially modified request context.
    /// Return the same context if no modifications are needed.
    /// </returns>
    Task<ConvexRequestContext> BeforeRequestAsync(
        ConvexRequestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a successful response is received from the Convex backend.
    /// Can inspect or transform the response.
    /// </summary>
    /// <param name="context">The response context containing request and response details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The potentially modified response context.
    /// Return the same context if no modifications are needed.
    /// </returns>
    Task<ConvexResponseContext> AfterResponseAsync(
        ConvexResponseContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when an error occurs during request execution.
    /// Can log errors, transform exceptions, or implement custom error handling.
    /// </summary>
    /// <param name="context">The error context containing request details and the exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method should not throw exceptions. Any exceptions thrown will be logged
    /// and ignored to prevent interceptors from breaking the request pipeline.
    /// </remarks>
    Task OnErrorAsync(
        ConvexErrorContext context,
        CancellationToken cancellationToken = default);
}
