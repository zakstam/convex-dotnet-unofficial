using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Convex.Client.Infrastructure.Telemetry;

namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Default implementation of <see cref="IHttpClientProvider"/> using System.Net.Http.HttpClient.
/// </summary>
public class DefaultHttpClientProvider : IHttpClientProvider
{
    private readonly HttpClient _httpClient;
    private Func<CancellationToken, Task<Dictionary<string, string>>>? _authHeadersProvider;
    private readonly ILogger? _logger;
    private readonly bool _enableDebugLogging;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpClientProvider"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="deploymentUrl">The Convex deployment URL.</param>
    /// <param name="logger">Optional logger for logging HTTP operations.</param>
    /// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or deploymentUrl is null.</exception>
    /// <exception cref="ArgumentException">Thrown when deploymentUrl is empty or whitespace.</exception>
    public DefaultHttpClientProvider(HttpClient httpClient, string deploymentUrl, ILogger? logger = null, bool enableDebugLogging = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or whitespace.", nameof(deploymentUrl));
        }

        DeploymentUrl = deploymentUrl;
        _logger = logger;
        _enableDebugLogging = enableDebugLogging;
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

        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[HTTP] Request starting: Method={Method}, Uri={Uri}",
                request.Method, SanitizeUri(request.RequestUri));
        }

        // Add authentication headers if provider is configured
        if (_authHeadersProvider != null)
        {
            var authHeaders = await _authHeadersProvider(cancellationToken).ConfigureAwait(false);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[HTTP] Adding authentication headers: Count={HeaderCount}", authHeaders.Count);
            }

            foreach (var header in authHeaders)
            {
                _ = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[HTTP] Request completed: Method={Method}, Uri={Uri}, StatusCode={StatusCode}, Duration={DurationMs}ms",
                    request.Method, SanitizeUri(request.RequestUri), (int)response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger?.LogError(ex, "[HTTP] Request failed: Method={Method}, Uri={Uri}, Duration={DurationMs}ms, ErrorType={ErrorType}",
                request.Method, SanitizeUri(request.RequestUri), stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name);
            throw;
        }
    }

    private static string? SanitizeUri(Uri? uri)
    {
        if (uri == null) return null;

        // Remove query parameters that might contain sensitive data
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
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
