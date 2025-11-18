namespace Convex.Client.Shared.Middleware;

/// <summary>
/// Interface for Convex request/response middleware.
/// Middleware can inspect, modify, or short-circuit requests and responses.
/// </summary>
public interface IConvexMiddleware
{
    /// <summary>
    /// Invokes the middleware with the given request.
    /// The middleware should call the next delegate to continue the pipeline,
    /// or return a response directly to short-circuit the pipeline.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next middleware in the pipeline or the final handler.</param>
    /// <returns>A task that completes with the response.</returns>
    Task<ConvexResponse> InvokeAsync(ConvexRequest request, ConvexRequestDelegate next);
}
