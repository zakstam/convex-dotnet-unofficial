namespace Convex.Client.Shared.Middleware;

/// <summary>
/// Represents a request to a Convex function.
/// Used by middleware to inspect and transform requests.
/// </summary>
/// <remarks>
/// Creates a new ConvexRequest.
/// </remarks>
/// <param name="functionName">The function name.</param>
/// <param name="method">The request method (query, mutation, or action).</param>
/// <param name="args">Optional arguments.</param>
/// <param name="cancellationToken">Optional cancellation token.</param>
public sealed class ConvexRequest(
    string functionName,
    string method,
    object? args = null,
    CancellationToken cancellationToken = default)
{
    /// <summary>
    /// Gets or sets the function name (e.g., "todos:list").
    /// </summary>
    public string FunctionName { get; set; } = functionName ?? throw new ArgumentNullException(nameof(functionName));

    /// <summary>
    /// Gets or sets the request method type (query, mutation, or action).
    /// </summary>
    public string Method { get; set; } = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>
    /// Gets or sets the arguments to pass to the function.
    /// Null if no arguments are provided.
    /// </summary>
    public object? Args { get; set; } = args;

    /// <summary>
    /// Gets the request metadata that can be used by middleware.
    /// Middleware can store custom data here for passing information between middleware layers.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; } = [];

    /// <summary>
    /// Gets or sets the timeout for this request.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token for this request.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = cancellationToken;

    /// <summary>
    /// Creates a shallow copy of this request.
    /// Useful for middleware that needs to modify the request without affecting the original.
    /// </summary>
    public ConvexRequest Clone()
    {
        var clone = new ConvexRequest(FunctionName, Method, Args, CancellationToken)
        {
            Timeout = Timeout
        };

        foreach (var kvp in Metadata)
        {
            clone.Metadata[kvp.Key] = kvp.Value;
        }

        return clone;
    }
}
