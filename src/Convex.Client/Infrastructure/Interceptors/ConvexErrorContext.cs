namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Context information for a Convex request error, available to interceptors.
/// </summary>
public sealed class ConvexErrorContext
{
    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    public ConvexRequestContext Request { get; set; } = null!;

    /// <summary>
    /// Gets or sets the exception.
    /// </summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>
    /// Gets or sets the error timestamp.
    /// </summary>
    public DateTimeOffset ErrorTimestamp { get; set; }

    /// <summary>
    /// Gets the duration.
    /// </summary>
    public TimeSpan Duration => ErrorTimestamp - Request.Timestamp;

    /// <summary>
    /// Gets or sets the string.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
