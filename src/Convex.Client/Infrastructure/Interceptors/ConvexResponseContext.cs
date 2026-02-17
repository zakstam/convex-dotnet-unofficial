namespace Convex.Client.Infrastructure.Interceptors;

/// <summary>
/// Context information for a Convex response, available to interceptors.
/// </summary>
public sealed class ConvexResponseContext
{
    /// <summary>
    /// Gets or sets the request.
    /// </summary>
    public ConvexRequestContext Request { get; set; } = null!;

    /// <summary>
    /// Gets or sets the result.
    /// This is the raw JSON element from the Convex response.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response timestamp.
    /// </summary>
    public DateTimeOffset ResponseTimestamp { get; set; }

    /// <summary>
    /// Gets the duration.
    /// </summary>
    public TimeSpan Duration => ResponseTimestamp - Request.Timestamp;

    /// <summary>
    /// Gets or sets the string.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}
