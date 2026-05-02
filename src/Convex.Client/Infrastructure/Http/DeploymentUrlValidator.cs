namespace Convex.Client.Infrastructure.Http;

/// <summary>
/// Validates Convex deployment URLs and derives related transport endpoints.
/// </summary>
public static class DeploymentUrlValidator
{
    private const string WebSocketSecureScheme = "wss";
    private const string WebSocketInsecureScheme = "ws";

    /// <summary>
    /// Validates a deployment URL and throws if the scheme is not allowed.
    /// </summary>
    public static void Validate(string deploymentUrl, bool allowInsecureDevelopmentTransport = false)
    {
        if (string.IsNullOrWhiteSpace(deploymentUrl))
        {
            throw new ArgumentException("Deployment URL cannot be null or whitespace.", nameof(deploymentUrl));
        }

        if (!Uri.TryCreate(deploymentUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Deployment URL must be a valid absolute URI.", nameof(deploymentUrl));
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            if (allowInsecureDevelopmentTransport || IsLoopbackHost(uri.Host))
            {
                return;
            }

            throw new ArgumentException(
                "Deployment URL must use HTTPS unless `AllowInsecureDevelopmentTransport` is enabled or the host is loopback.",
                nameof(deploymentUrl));
        }

        throw new ArgumentException("Deployment URL must use the HTTP or HTTPS scheme.", nameof(deploymentUrl));
    }

    /// <summary>
    /// Builds the corresponding WebSocket URI for a validated deployment URL.
    /// </summary>
    public static Uri ToWebSocketUri(string deploymentUrl, string relativePath, bool allowInsecureDevelopmentTransport = false)
    {
        Validate(deploymentUrl, allowInsecureDevelopmentTransport);

        var baseUri = new Uri(deploymentUrl, UriKind.Absolute);
        var builder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? WebSocketSecureScheme : WebSocketInsecureScheme,
            Path = $"{baseUri.AbsolutePath.TrimEnd('/')}/{relativePath.TrimStart('/')}"
        };

        if (builder.Scheme == WebSocketSecureScheme)
        {
            builder.Port = -1;
        }
        else if (baseUri.IsDefaultPort)
        {
            builder.Port = 80;
        }

        return builder.Uri;
    }

    private static bool IsLoopbackHost(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("[::1]", StringComparison.OrdinalIgnoreCase);
}
