namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Provides an abstraction over HTTP client operations for Convex API communication.
/// This interface enables testing, mocking, and custom HTTP transport implementations.
/// </summary>
public interface IHttpClientProvider
{
    /// <summary>
    /// Sends an HTTP request asynchronously.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    /// <exception cref="HttpRequestException">Thrown when the request fails.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the request times out or is cancelled.</exception>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets the base deployment URL for the Convex backend.
    /// </summary>
    string DeploymentUrl { get; }
}
