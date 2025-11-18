using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Godot implementation of IClerkTokenService using OAuth 2.0 Authorization Code Flow with PKCE.
/// </summary>
public class GodotClerkTokenService : IClerkTokenService
{
    private readonly ClerkOptions _options;
    private readonly ClerkAuthorizationCodeFlow _authFlow;
    private readonly HttpClient _httpClient;
    private ClerkOAuthCallbackServer? _callbackServer;
    private ClerkAuthorizationCodeFlow.PkceParameters? _currentPkceParams;

    private string? _cachedToken;
    private DateTime _tokenExpiry;
    private bool _isAuthenticated;
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of GodotClerkTokenService.
    /// </summary>
    /// <param name="options">Clerk configuration options.</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public GodotClerkTokenService(ClerkOptions options, HttpClient? httpClient = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? new HttpClient();

        if (string.IsNullOrWhiteSpace(_options.OAuthClientId))
        {
            throw new InvalidOperationException(
                "Clerk OAuthClientId is required for Godot OAuth flow. " +
                "Create an OAuth Application in Clerk Dashboard and set the Client ID in ClerkOptions or configuration.");
        }

        if (string.IsNullOrWhiteSpace(_options.ClerkDomain))
        {
            throw new InvalidOperationException(
                "Clerk ClerkDomain is required for Godot OAuth flow. " +
                "Set it in ClerkOptions or configuration.");
        }

        _authFlow = new ClerkAuthorizationCodeFlow(_options.OAuthClientId, _options.ClerkDomain, _httpClient);
        _isAuthenticated = false;
        _isLoading = false;
    }

    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Gets whether the authentication state is still loading.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Gets the current authentication token from Clerk.
    /// </summary>
    /// <param name="tokenTemplate">The JWT template name (default: "convex").</param>
    /// <param name="skipCache">Whether to skip cache and force a fresh token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The authentication token, or null if not available.</returns>
    public Task<string?> GetTokenAsync(
        string tokenTemplate = "convex",
        bool skipCache = false,
        CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] GetTokenAsync called - IsAuthenticated: {_isAuthenticated}, HasCachedToken: {_cachedToken != null}, TokenExpired: {DateTime.UtcNow >= _tokenExpiry}");

        // Use configured template if not specified
        if (tokenTemplate == "convex" && !string.IsNullOrEmpty(_options.TokenTemplate))
        {
            tokenTemplate = _options.TokenTemplate;
        }

        // Return cached token if available and not expired
        if (!skipCache && _options.EnableTokenCaching && _cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] Returning cached token (length: {_cachedToken.Length})");
            return Task.FromResult<string?>(_cachedToken);
        }

        // If not authenticated, return null (caller should initiate auth flow)
        if (!_isAuthenticated)
        {
            System.Diagnostics.Debug.WriteLine("[GodotClerkTokenService] Not authenticated, returning null");
            return Task.FromResult<string?>(null);
        }

        // Return cached token if available
        System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] Returning cached token (length: {_cachedToken?.Length ?? 0})");
        return Task.FromResult<string?>(_cachedToken);
    }

    /// <summary>
    /// Starts the OAuth 2.0 Authorization Code Flow with PKCE.
    /// Opens the system browser and waits for callback.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing success status and optional error message.</returns>
    public async Task<AuthFlowResult> StartAuthorizationFlowAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoading)
        {
            return new AuthFlowResult
            {
                Success = false,
                ErrorMessage = "Authentication is already in progress."
            };
        }

        _isLoading = true;

        try
        {
            // 1. Start local HTTP server for callback
            _callbackServer = new ClerkOAuthCallbackServer(_options.CallbackPort, _options.CallbackPath);
            await _callbackServer.StartAsync(cancellationToken);

            // 2. Generate PKCE parameters
            _currentPkceParams = _authFlow.GeneratePkceParameters();

            // 3. Build authorization URL
            var authUrl = _authFlow.BuildAuthorizationUrl(
                _callbackServer.CallbackUrl,
                _currentPkceParams);

            // 4. Open browser
            if (!OpenBrowser(authUrl))
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = "Failed to open browser. Please visit this URL manually: " + authUrl,
                    AuthorizationUrl = authUrl
                };
            }

            // 5. Wait for callback
            var callbackResult = await _callbackServer.WaitForCallbackAsync(
                timeoutSeconds: 300,
                cancellationToken: cancellationToken);

            // 6. Validate callback
            if (!callbackResult.Success)
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = callbackResult.ErrorDescription ?? "Authentication failed."
                };
            }

            // Validate state parameter (CSRF protection)
            if (callbackResult.State != _currentPkceParams.State)
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = "State mismatch. Possible CSRF attack. Please try again."
                };
            }

            // 7. Exchange authorization code for token
            var tokenResponse = await _authFlow.ExchangeCodeForTokenAsync(
                callbackResult.AuthorizationCode!,
                _callbackServer.CallbackUrl,
                _currentPkceParams.CodeVerifier,
                cancellationToken);

            if (!string.IsNullOrEmpty(tokenResponse.Error))
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = tokenResponse.ErrorDescription ?? $"Token exchange failed: {tokenResponse.Error}"
                };
            }

            if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = $"Token response missing access_token. Received: TokenType={tokenResponse.TokenType}, ExpiresIn={tokenResponse.ExpiresIn}, SessionId={tokenResponse.SessionId ?? "null"}, IdToken={!string.IsNullOrEmpty(tokenResponse.IdToken)}"
                };
            }

            // 8. Use OAuth id_token directly for Convex authentication
            // For desktop OAuth applications, we use the id_token (OIDC JWT) directly
            // because fetching JWT template tokens requires browser cookies or Clerk Secret Key,
            // neither of which are available in desktop applications.
            //
            // The Convex backend must be configured with:
            // - domain: Clerk Frontend API domain (matches "iss" claim)
            // - applicationID: OAuth Client ID (matches "aud" claim)
            var jwtToken = tokenResponse.IdToken;

            if (string.IsNullOrEmpty(jwtToken))
            {
                return new AuthFlowResult
                {
                    Success = false,
                    ErrorMessage = "OAuth response missing id_token. This should not happen with a properly configured OAuth application."
                };
            }

            System.Diagnostics.Debug.WriteLine("[GodotClerkTokenService] Using OAuth id_token directly for Convex authentication");
            System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] Token length: {jwtToken.Length} characters");

            // 9. Cache the token
            _cachedToken = jwtToken;
            _tokenExpiry = DateTime.UtcNow.Add(_options.TokenCacheExpiration);
            _isAuthenticated = true;

            return new AuthFlowResult
            {
                Success = true
            };
        }
        catch (OperationCanceledException)
        {
            return new AuthFlowResult
            {
                Success = false,
                ErrorMessage = "Authentication was cancelled."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] Auth flow failed: {ex.Message}");
            return new AuthFlowResult
            {
                Success = false,
                ErrorMessage = $"Authentication error: {ex.Message}"
            };
        }
        finally
        {
            // Cleanup
            _callbackServer?.Dispose();
            _callbackServer = null;
            _currentPkceParams = null;
            _isLoading = false;
        }
    }

    /// <summary>
    /// Sets a token manually (for manual token entry fallback).
    /// </summary>
    /// <param name="token">The JWT token to use.</param>
    public void SetTokenManually(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentNullException(nameof(token));
        }

        _cachedToken = token;
        _tokenExpiry = DateTime.UtcNow.Add(_options.TokenCacheExpiration);
        _isAuthenticated = true;
    }

    /// <summary>
    /// Signs out the current user by clearing the cached token.
    /// </summary>
    public void SignOut()
    {
        _cachedToken = null;
        _isAuthenticated = false;
        _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// Clears the cached token, forcing a fresh token on next request.
    /// </summary>
    public void ClearCache()
    {
        _cachedToken = null;
        _tokenExpiry = DateTime.MinValue;
    }

    /// <summary>
    /// Opens the default system browser with the given URL.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    /// <returns>True if browser was opened successfully, false otherwise.</returns>
    private bool OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows
                _ = Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS
                _ = Process.Start("open", url);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux
                _ = Process.Start("xdg-open", url);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GodotClerkTokenService] Failed to open browser: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Result of an authorization flow attempt.
    /// </summary>
    public class AuthFlowResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AuthorizationUrl { get; set; }
    }
}
