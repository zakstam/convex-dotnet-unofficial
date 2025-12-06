using System.Text.Json.Serialization;
using Convex.Client.Infrastructure.Common;
using Microsoft.Extensions.Logging;

namespace Convex.BetterAuth;

/// <summary>
/// Token provider that supplies Convex JWTs from Better Auth to the Convex client.
/// This exchanges Better Auth session tokens for JWTs that Convex can validate.
/// This class is thread-safe.
/// </summary>
public class BetterAuthTokenProvider : IAuthTokenProvider, IDisposable
{
    private readonly IBetterAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly BetterAuthOptions _options;
    private readonly ILogger<BetterAuthTokenProvider>? _logger;
    private readonly object _cacheLock = new();

    private string? _cachedJwt;
    private DateTime _jwtExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private bool _disposed;

    // Refresh JWT 1 minute before expiry
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(1);
    // Default JWT expiration (15 minutes as per Better Auth default)
    private static readonly TimeSpan DefaultJwtExpiration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Creates a new instance of <see cref="BetterAuthTokenProvider"/>.
    /// </summary>
    /// <param name="authService">The Better Auth service to get session tokens from.</param>
    /// <param name="httpClient">HTTP client for making requests to Better Auth.</param>
    /// <param name="options">Better Auth configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public BetterAuthTokenProvider(
        IBetterAuthService authService,
        HttpClient httpClient,
        BetterAuthOptions options,
        ILogger<BetterAuthTokenProvider>? logger = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        // Subscribe to auth state changes to invalidate cache on sign-out
        _authService.OnAuthStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged()
    {
        // Clear cache when auth state changes (sign-in, sign-out, session restore)
        ClearCache();
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var sessionToken = _authService.GetSessionToken();
        if (string.IsNullOrEmpty(sessionToken))
        {
            _logger?.LogDebug("No session token available from auth service");
            return null;
        }

        _logger?.LogDebug("Session token available, length: {Length}", sessionToken.Length);

        // If configured to use session token directly (default for @convex-dev/better-auth),
        // return the session token without exchanging for a JWT
        if (_options.UseSessionTokenDirectly)
        {
            _logger?.LogDebug("Using session token directly (UseSessionTokenDirectly=true)");
            return sessionToken;
        }

        // Check if we have a valid cached JWT (thread-safe read)
        lock (_cacheLock)
        {
            if (_cachedJwt != null && DateTime.UtcNow < _jwtExpiry - RefreshBuffer)
            {
                _logger?.LogDebug("Returning cached JWT");
                return _cachedJwt;
            }
        }

        // Need to refresh the JWT
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            lock (_cacheLock)
            {
                if (_cachedJwt != null && DateTime.UtcNow < _jwtExpiry - RefreshBuffer)
                {
                    return _cachedJwt;
                }
            }

            var tokenEndpoint = $"{_options.SiteUrl}/api/auth/convex/token";
            _logger?.LogDebug("Exchanging session token for JWT at: {Endpoint}", tokenEndpoint);

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                tokenEndpoint);
            // Cross-domain plugin expects session cookies via better-auth-cookie header
            // The session token contains the full cookie string (token + signature)
            request.Headers.Add("better-auth-cookie", sessionToken);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            _logger?.LogDebug("JWT endpoint response: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
#if NET5_0_OR_GREATER
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
                var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

                var result = System.Text.Json.JsonSerializer.Deserialize<ConvexTokenResponse>(responseContent);

                if (!string.IsNullOrEmpty(result?.Token))
                {
                    lock (_cacheLock)
                    {
                        _cachedJwt = result.Token;
                        _jwtExpiry = ParseJwtExpiry(result.Token) ?? DateTime.UtcNow + DefaultJwtExpiration;
                    }
                    _logger?.LogDebug("Successfully obtained Convex JWT, expires at {Expiry}", _jwtExpiry);
                    return result.Token;
                }
                else
                {
                    _logger?.LogWarning("JWT response was success but no token in body");
                }
            }

#if NET5_0_OR_GREATER
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
            _logger?.LogWarning(
                "Failed to get Convex JWT: {StatusCode} - {Error}",
                response.StatusCode,
                errorContent);

            // Return null if JWT exchange fails
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error exchanging session token for JWT");
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Parses the expiry time from a JWT token's 'exp' claim.
    /// </summary>
    /// <param name="jwt">The JWT token string.</param>
    /// <returns>The expiry time, or null if parsing fails.</returns>
    private static DateTime? ParseJwtExpiry(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            // Decode the payload (second part)
            var payload = parts[1];
            // Add padding if needed for base64 decoding
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("exp", out var expElement))
            {
                var expSeconds = expElement.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
            }
        }
        catch
        {
            // Fall back to default expiry if parsing fails
        }

        return null;
    }

    /// <summary>
    /// Clears the cached JWT, forcing a refresh on the next request.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cachedJwt = null;
            _jwtExpiry = DateTime.MinValue;
        }
        _logger?.LogDebug("Cleared JWT cache");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and optionally managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Unsubscribe from auth state changes
            _authService.OnAuthStateChanged -= OnAuthStateChanged;

            _tokenLock.Dispose();
            lock (_cacheLock)
            {
                _cachedJwt = null;
            }
        }

        _disposed = true;
    }

    private sealed class ConvexTokenResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
