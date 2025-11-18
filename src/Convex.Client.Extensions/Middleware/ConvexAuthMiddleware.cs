using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Extensions.Middleware;

/// <summary>
/// Middleware for automatically setting Convex authentication from HTTP context.
/// Extracts JWT tokens from Authorization header and configures the Convex client.
/// </summary>
public class ConvexAuthMiddleware(
    RequestDelegate next,
    ILogger<ConvexAuthMiddleware> logger,
    ConvexAuthMiddlewareOptions? options = null)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<ConvexAuthMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConvexAuthMiddlewareOptions _options = options ?? new ConvexAuthMiddlewareOptions();

    public async Task InvokeAsync(HttpContext context, IConvexClient convexClient)
    {
        if (convexClient == null)
            throw new ArgumentNullException(nameof(convexClient));

        // Extract token from Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader))
        {
            // Parse Bearer token
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        if (convexClient is ConvexClient client)
                        {
                            await client.AuthenticationSlice.SetAuthTokenAsync(token);
                        }

                        if (_options.LogAuthenticationChanges)
                        {
                            _logger.LogDebug("Convex auth token set from request Authorization header");
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Invalid JWT token format in Authorization header");

                        if (_options.RejectInvalidTokens)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Invalid authentication token");
                            return;
                        }
                    }
                }
            }
            // Support custom authentication schemes
            else if (_options.CustomAuthScheme != null &&
                     authHeader.StartsWith(_options.CustomAuthScheme + " ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring((_options.CustomAuthScheme + " ").Length).Trim();

                if (!string.IsNullOrEmpty(token) && _options.CustomTokenExtractor != null)
                {
                    try
                    {
                        var extractedToken = _options.CustomTokenExtractor(token);
                        if (!string.IsNullOrEmpty(extractedToken) && convexClient is ConvexClient client)
                        {
                            await client.AuthenticationSlice.SetAuthTokenAsync(extractedToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract token using custom extractor");
                    }
                }
            }
        }
        // Extract from custom header if configured
        else if (!string.IsNullOrEmpty(_options.CustomTokenHeader))
        {
            var customToken = context.Request.Headers[_options.CustomTokenHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(customToken))
            {
                try
                {
                    if (convexClient is ConvexClient client)
                    {
                        await client.AuthenticationSlice.SetAuthTokenAsync(customToken);
                    }

                    if (_options.LogAuthenticationChanges)
                    {
                        _logger.LogDebug("Convex auth token set from custom header '{Header}'", _options.CustomTokenHeader);
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Invalid token in custom header '{Header}'", _options.CustomTokenHeader);
                }
            }
        }
        // Clear auth if no token found and configured to do so
        else if (_options.ClearAuthIfNoToken)
        {
            if (convexClient is ConvexClient client)
            {
                await client.AuthenticationSlice.ClearAuthAsync();
            }

            if (_options.LogAuthenticationChanges)
            {
                _logger.LogDebug("Convex auth cleared - no token in request");
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Options for configuring Convex authentication middleware.
/// </summary>
public class ConvexAuthMiddlewareOptions
{
    /// <summary>
    /// Gets or sets whether to log authentication changes (default: false).
    /// </summary>
    public bool LogAuthenticationChanges { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to reject requests with invalid tokens (default: false).
    /// If false, invalid tokens are ignored and the request continues.
    /// </summary>
    public bool RejectInvalidTokens { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to clear authentication if no token is present (default: false).
    /// </summary>
    public bool ClearAuthIfNoToken { get; set; } = false;

    /// <summary>
    /// Gets or sets a custom header name to extract tokens from (optional).
    /// </summary>
    public string? CustomTokenHeader { get; set; }

    /// <summary>
    /// Gets or sets a custom authentication scheme name (optional).
    /// </summary>
    public string? CustomAuthScheme { get; set; }

    /// <summary>
    /// Gets or sets a custom function to extract tokens from custom auth schemes.
    /// </summary>
    public Func<string, string?>? CustomTokenExtractor { get; set; }
}

/// <summary>
/// Extension methods for adding Convex authentication middleware.
/// </summary>
public static class ConvexAuthMiddlewareExtensions
{
    /// <summary>
    /// Adds Convex authentication middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="options">Optional middleware configuration.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseConvexAuth(
        this IApplicationBuilder builder,
        ConvexAuthMiddlewareOptions? options = null) => builder.UseMiddleware<ConvexAuthMiddleware>(options ?? new ConvexAuthMiddlewareOptions());

    /// <summary>
    /// Adds Convex authentication middleware with configuration callback.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configureOptions">Action to configure the middleware options.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseConvexAuth(
        this IApplicationBuilder builder,
        Action<ConvexAuthMiddlewareOptions> configureOptions)
    {
        var options = new ConvexAuthMiddlewareOptions();
        configureOptions(options);
        return builder.UseConvexAuth(options);
    }
}
