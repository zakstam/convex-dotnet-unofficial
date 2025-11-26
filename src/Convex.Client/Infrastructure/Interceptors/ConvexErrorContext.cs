namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Context information for a Convex request error, available to interceptors.
/// </summary>
public sealed class ConvexErrorContext
{
    /// <summary>
    /// Gets or sets the original request context.
    /// </summary>
    public ConvexRequestContext Request { get; set; } = null!;

    /// <summary>
    /// Gets or sets the exception that occurred during request execution.
    /// </summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset ErrorTimestamp { get; set; }

    /// <summary>
    /// Gets the duration from request creation to error occurrence.
    /// </summary>
    public TimeSpan Duration => ErrorTimestamp - Request.Timestamp;

    /// <summary>
    /// Gets or sets additional metadata for the error.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
