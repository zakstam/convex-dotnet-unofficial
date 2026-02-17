namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Context information for a Convex request, available to interceptors.
/// </summary>
public sealed class ConvexRequestContext
{
    /// <summary>
    /// Gets or sets the request type.
    /// </summary>
    public string RequestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments.
    /// </summary>
    public object? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the unique ID.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the string.
    /// to pass data between BeforeRequest and AfterResponse hooks.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}
