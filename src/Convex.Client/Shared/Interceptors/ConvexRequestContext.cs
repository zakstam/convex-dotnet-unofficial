namespace Convex.Client.Shared.Interceptors;

/// <summary>
/// Context information for a Convex request, available to interceptors.
/// </summary>
public sealed class ConvexRequestContext
{
    /// <summary>
    /// Gets or sets the type of request (query, mutation, action).
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the function being called.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request arguments (if any).
    /// </summary>
    public object? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the unique request identifier.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata that can be used by interceptors
    /// to pass data between BeforeRequest and AfterResponse hooks.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// Gets or sets the cancellation token for the request.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
