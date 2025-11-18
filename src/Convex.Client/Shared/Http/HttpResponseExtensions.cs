namespace Convex.Client.Shared.Http;

/// <summary>
/// Extension methods for HttpResponseMessage to provide safe content reading.
/// </summary>
public static class HttpResponseExtensions
{
    /// <summary>
    /// Safely reads the response content as a string, handling null content gracefully.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response content as a string, or empty string if content is null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
    public static async Task<string> ReadContentAsStringAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        return response == null
            ? throw new ArgumentNullException(nameof(response))
            : response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Safely reads the response content as a stream, handling null content gracefully.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response content as a stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when response content is null.</exception>
    public static async Task<Stream> ReadContentAsStreamAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        return response == null
            ? throw new ArgumentNullException(nameof(response))
            : response.Content == null
            ? throw new InvalidOperationException("Response content is null. Cannot read as stream.")
            : await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets response headers including content headers, handling null content gracefully.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>A dictionary of all response headers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
    public static Dictionary<string, string> GetAllHeaders(this HttpResponseMessage response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var headers = response.Headers
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headers;
    }

    /// <summary>
    /// Gets the content type from the response, handling null content gracefully.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>The content type media type, or null if content is null or content type is not set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
    public static string? GetContentType(this HttpResponseMessage response) => response == null ? throw new ArgumentNullException(nameof(response)) : (response.Content?.Headers.ContentType?.MediaType);
}

