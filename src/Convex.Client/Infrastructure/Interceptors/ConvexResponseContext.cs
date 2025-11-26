namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Context information for a Convex response, available to interceptors.
/// </summary>
public sealed class ConvexResponseContext
{
    /// <summary>
    /// Gets or sets the original request context.
    /// </summary>
    public ConvexRequestContext Request { get; set; } = null!;

    /// <summary>
    /// Gets or sets the response result (before deserialization to the target type).
    /// This is the raw JSON element from the Convex response.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was received.
    /// </summary>
    public DateTimeOffset ResponseTimestamp { get; set; }

    /// <summary>
    /// Gets the duration of the request (from request creation to response received).
    /// </summary>
    public TimeSpan Duration => ResponseTimestamp - Request.Timestamp;

    /// <summary>
    /// Gets or sets additional metadata for the response.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
