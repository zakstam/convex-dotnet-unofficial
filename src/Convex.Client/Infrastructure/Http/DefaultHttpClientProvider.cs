namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Default implementation of <see cref="IHttpClientProvider"/> using System.Net.Http.HttpClient.
/// </summary>
public class DefaultHttpClientProvider : IHttpClientProvider
{
    private readonly HttpClient _httpClient;
    private Func<CancellationToken, Task<Dictionary<string, string>>>? _authHeadersProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpClientProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or deploymentUrl is null.</exception>
    /// <exception cref="ArgumentException">Thrown when deploymentUrl is empty or whitespace.</exception>
    public DefaultHttpClientProvider(HttpClient httpClient, string deploymentUrl)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or whitespace.", nameof(deploymentUrl));
        }

        DeploymentUrl = deploymentUrl;
    }

    /// <summary>
    /// Sets the authentication headers provider function.
    /// This is called by ConvexClient to wire up the Authentication slice.
    /// </summary>
    /// <param name="authHeadersProvider">Function that retrieves authentication headers.</param>
    public void SetAuthHeadersProvider(Func<CancellationToken, Task<Dictionary<string, string>>> authHeadersProvider) => _authHeadersProvider = authHeadersProvider;

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Add authentication headers if provider is configured
        if (_authHeadersProvider != null)
        {
            var authHeaders = await _authHeadersProvider(cancellationToken).ConfigureAwait(false);

            foreach (var header in authHeaders)
            {
                _ = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public TimeSpan Timeout
    {
        get => _httpClient.Timeout;
        set => _httpClient.Timeout = value;
    }

    /// <inheritdoc/>
    public string DeploymentUrl { get; }
}
