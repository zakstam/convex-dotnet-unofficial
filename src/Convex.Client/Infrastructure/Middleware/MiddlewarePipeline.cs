namespace Convex.Client.Infrastructure.Middleware;

/// <summary>
/// Manages the execution of middleware in a pipeline.
/// </summary>
internal sealed class MiddlewarePipeline
{
    private readonly List<IConvexMiddleware> _middleware = [];
    private ConvexRequestDelegate? _compiledPipeline;

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
            return finalHandler;
        }

        // Build the pipeline from the end backwards
        var next = finalHandler;

        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var middleware = _middleware[i];
            var currentNext = next;
            next = request => middleware.InvokeAsync(request, currentNext);
        }

        return next;
    }

    /// <summary>
    /// Executes the pipeline with the given request and final handler.
    /// </summary>
    public async Task<ConvexResponse> ExecuteAsync(ConvexRequest request, ConvexRequestDelegate finalHandler)
    {
        // Build or use cached pipeline
        if (_compiledPipeline == null)
        {
            _compiledPipeline = Build(finalHandler);
        }

        return await _compiledPipeline(request);
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
