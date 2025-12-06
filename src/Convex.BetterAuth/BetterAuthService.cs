using System.Net.Http.Json;
using System.Text.Json;
using Convex.BetterAuth.Models;
using Microsoft.Extensions.Logging;

namespace Convex.BetterAuth;

/// <summary>
/// Service for handling authentication with Better Auth via Convex.
/// This class is thread-safe.
/// </summary>
public class BetterAuthService : IBetterAuthService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ISessionStorage _sessionStorage;
    private readonly ILogger<BetterAuthService>? _logger;
    private readonly BetterAuthOptions _options;
    private readonly object _stateLock = new();

    private AuthSession? _currentSession;
    private AuthUser? _currentUser;
    private bool _disposed;

    // Rate limiting: minimum time between auth attempts
    private DateTime _lastAuthAttempt = DateTime.MinValue;
    private static readonly TimeSpan MinAuthInterval = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public event Action? OnAuthStateChanged;

    /// <inheritdoc />
    public AuthUser? CurrentUser
    {
        get
        {
            lock (_stateLock)
            {
                return _currentUser;
            }
        }
    }

    /// <inheritdoc />
    public AuthSession? CurrentSession
    {
        get
        {
            lock (_stateLock)
            {
                return _currentSession;
            }
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            lock (_stateLock)
            {
                return _currentSession != null && _currentUser != null;
            }
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="BetterAuthService"/>.
    /// </summary>
    public BetterAuthService(
        HttpClient httpClient,
        ISessionStorage sessionStorage,
        BetterAuthOptions options,
        ILogger<BetterAuthService>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _sessionStorage = sessionStorage ?? throw new ArgumentNullException(nameof(sessionStorage));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (string.IsNullOrEmpty(_options.SiteUrl))
        {
            throw new InvalidOperationException(
                "BetterAuth:SiteUrl must be configured. Set it in appsettings.json or via configuration.");
        }

        // Enforce HTTPS for security - credentials must not be sent over unencrypted connections
        if (!_options.SiteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "BetterAuth:SiteUrl must use HTTPS to ensure credentials are transmitted securely.");
        }

        // Configure HttpClient for cross-origin requests
        if (!_httpClient.DefaultRequestHeaders.Contains("Accept"))
        {
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }
    }

    /// <inheritdoc />
    public Task<AuthResult> SignUpAsync(string email, string password, string? name = null)
    {
        return SignUpAsync(email, password, name, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<AuthResult> SignUpAsync(string email, string password, string? name, CancellationToken cancellationToken)
    {
        // Input validation
        var validationError = ValidateCredentials(email, password);
        if (validationError != null)
        {
            return AuthResult.Failure(validationError);
        }

        // Rate limiting check
        if (!TryCheckRateLimit(out var rateLimitError))
        {
            return AuthResult.Failure(rateLimitError!);
        }

        try
        {
            var request = new SignUpRequest
            {
                Email = email,
                Password = password,
                Name = name ?? email.Split('@')[0]
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.SiteUrl}/api/auth/sign-up/email",
                request,
                cancellationToken).ConfigureAwait(false);

            return await ProcessAuthResponseAsync(response, email, "Sign up", "Sign up failed", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Sign up error for {Email}", email);
            return AuthResult.Failure("An unexpected error occurred. Please try again.");
        }
    }

    /// <inheritdoc />
    public Task<AuthResult> SignInAsync(string email, string password)
    {
        return SignInAsync(email, password, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        // Input validation
        var validationError = ValidateCredentials(email, password);
        if (validationError != null)
        {
            return AuthResult.Failure(validationError);
        }

        // Rate limiting check
        if (!TryCheckRateLimit(out var rateLimitError))
        {
            return AuthResult.Failure(rateLimitError!);
        }

        try
        {
            var request = new SignInRequest
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.SiteUrl}/api/auth/sign-in/email",
                request,
                cancellationToken).ConfigureAwait(false);

            return await ProcessAuthResponseAsync(response, email, "Sign in", "Invalid email or password", cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Sign in error for {Email}", email);
            return AuthResult.Failure("An unexpected error occurred. Please try again.");
        }
    }

    /// <inheritdoc />
    public Task SignOutAsync()
    {
        return SignOutAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        string? sessionToken;
        lock (_stateLock)
        {
            sessionToken = _currentSession?.Token;
        }

        try
        {
            if (sessionToken != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.SiteUrl}/api/auth/sign-out");
                // Cross-domain plugin expects session cookies via better-auth-cookie header
                // The stored token contains the full cookie string (token + signature)
                request.Headers.Add("better-auth-cookie", sessionToken);

                await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Sign out request failed (ignoring)");
        }
        finally
        {
            lock (_stateLock)
            {
                _currentUser = null;
                _currentSession = null;
            }

            await _sessionStorage.RemoveTokenAsync().ConfigureAwait(false);

            OnAuthStateChanged?.Invoke();
            _logger?.LogInformation("User signed out");
        }
    }

    /// <inheritdoc />
    public Task TryRestoreSessionAsync()
    {
        return TryRestoreSessionAsync(CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task TryRestoreSessionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _sessionStorage.GetTokenAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.SiteUrl}/api/auth/get-session");
            // Cross-domain plugin expects session cookies via better-auth-cookie header
            // The stored token contains the full cookie string (token + signature)
            request.Headers.Add("better-auth-cookie", token);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (result?.User != null)
                {
                    lock (_stateLock)
                    {
                        _currentUser = result.User;
                        // IMPORTANT: Keep the original stored token (full cookie string) for session.
                        // The JSON body token is just the token ID, but cross-domain requires
                        // the full cookie string: "__Secure-better-auth.session_token=ID.SIGNATURE"
                        _currentSession = new AuthSession { Token = token };
                        if (result.Session != null)
                        {
                            _currentSession.Id = result.Session.Id;
                            _currentSession.ExpiresAt = result.Session.ExpiresAt;
                        }
                    }

                    OnAuthStateChanged?.Invoke();
                    _logger?.LogInformation("Session restored for user: {Email}", result.User.Email);
                    return;
                }
            }

            await _sessionStorage.RemoveTokenAsync().ConfigureAwait(false);
            _logger?.LogInformation("Stored session was invalid, removed");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to restore session");
            await _sessionStorage.RemoveTokenAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public string? GetSessionToken()
    {
        lock (_stateLock)
        {
            return _currentSession?.Token;
        }
    }

    /// <summary>
    /// Checks rate limiting and updates the last attempt time if allowed.
    /// </summary>
    /// <param name="errorMessage">The error message if rate limited.</param>
    /// <returns>True if the request is allowed, false if rate limited.</returns>
    private bool TryCheckRateLimit(out string? errorMessage)
    {
        lock (_stateLock)
        {
            var timeSinceLastAttempt = DateTime.UtcNow - _lastAuthAttempt;
            if (timeSinceLastAttempt < MinAuthInterval)
            {
                errorMessage = "Please wait a moment before trying again.";
                return false;
            }
            _lastAuthAttempt = DateTime.UtcNow;
            errorMessage = null;
            return true;
        }
    }

    /// <summary>
    /// Processes the authentication response from sign-in or sign-up.
    /// </summary>
    private async Task<AuthResult> ProcessAuthResponseAsync(
        HttpResponseMessage response,
        string email,
        string operationName,
        string defaultErrorMessage,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // Cross-domain plugin uses Set-Better-Auth-Cookie header instead of set-cookie
            var token = ExtractSessionToken(response);
            // Fallback to JSON body if header not present
            token ??= result?.Token ?? result?.Session?.Token;

            if (result?.User != null && !string.IsNullOrEmpty(token))
            {
                lock (_stateLock)
                {
                    _currentUser = result.User;
                    _currentSession = result.Session ?? new AuthSession { Token = token };
                }

                await _sessionStorage.StoreTokenAsync(token).ConfigureAwait(false);

                OnAuthStateChanged?.Invoke();
                _logger?.LogInformation("{Operation} successful: {Email}", operationName, email);
                return AuthResult.Success();
            }
        }

#if NET5_0_OR_GREATER
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
        _logger?.LogWarning("{Operation} failed: {StatusCode} - {Error}", operationName, response.StatusCode, errorContent);
        return AuthResult.Failure(ParseErrorMessage(errorContent) ?? defaultErrorMessage);
    }

    /// <summary>
    /// Validates email and password inputs.
    /// </summary>
    /// <returns>An error message if validation fails, null if valid.</returns>
    private static string? ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        // Basic email format validation
        if (!email.Contains('@') || !email.Contains('.') || email.Length < 5)
        {
            return "Please enter a valid email address.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 6)
        {
            return "Password must be at least 6 characters.";
        }

        return null;
    }

    private string? ParseErrorMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString();
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogDebug(ex, "Failed to parse error message from response: {Content}", content);
        }
        return null;
    }

    /// <summary>
    /// Extracts the session cookies from the Set-Better-Auth-Cookie header.
    /// The cross-domain plugin uses this header instead of set-cookie for CORS compatibility.
    /// Returns the full cookie string that can be sent back via better-auth-cookie header.
    /// </summary>
    private string? ExtractSessionToken(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Better-Auth-Cookie", out var values))
        {
            return null;
        }

        var cookieHeader = string.Join(", ", values);
        _logger?.LogDebug("Set-Better-Auth-Cookie header: {Header}", cookieHeader);

        // Parse the cookie header to extract all better-auth session cookies
        // Cookie names can have different prefixes depending on environment:
        // - "__Secure-better-auth.session_token" (HTTPS with Secure prefix)
        // - "__Host-better-auth.session_token" (HTTPS with Host prefix)
        // - "better-auth.session_token" or "better_auth.session_token" (HTTP/dev)
        // The signature may be embedded in the token value (token.signature format)
        // or as a separate cookie (session_token.sig)
        var cookieParts = new List<string>();

        foreach (var cookie in cookieHeader.Split(','))
        {
            var trimmedCookie = cookie.Trim();

            // Check for session token cookie (various naming patterns)
            // Match: *better-auth.session_token= or *better_auth.session_token=
            if (IsSessionTokenCookie(trimmedCookie) || IsSessionTokenSigCookie(trimmedCookie))
            {
                var endOfValue = trimmedCookie.IndexOf(';');
                var cookieValue = endOfValue > 0
                    ? trimmedCookie.Substring(0, endOfValue)
                    : trimmedCookie;
                cookieParts.Add(cookieValue);
                _logger?.LogDebug("Found session cookie: {Cookie}", cookieValue);
            }
        }

        if (cookieParts.Count > 0)
        {
            var result = string.Join("; ", cookieParts);
            _logger?.LogDebug("Extracted session cookies: {Cookies}", result);
            return result;
        }

        return null;
    }

    /// <summary>
    /// Checks if a cookie string is a Better Auth session token cookie.
    /// Handles various naming patterns: __Secure-better-auth.session_token, better_auth.session_token, etc.
    /// </summary>
    private static bool IsSessionTokenCookie(string cookie)
    {
        // Look for the session_token pattern (not the .sig variant)
        var lowerCookie = cookie.ToLowerInvariant();
        return (lowerCookie.Contains("better-auth.session_token=") ||
                lowerCookie.Contains("better_auth.session_token=")) &&
               !lowerCookie.Contains(".sig=");
    }

    /// <summary>
    /// Checks if a cookie string is a Better Auth session token signature cookie.
    /// </summary>
    private static bool IsSessionTokenSigCookie(string cookie)
    {
        var lowerCookie = cookie.ToLowerInvariant();
        return lowerCookie.Contains("session_token.sig=");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the service.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_stateLock)
            {
                _currentSession = null;
                _currentUser = null;
            }
        }

        _disposed = true;
    }
}
