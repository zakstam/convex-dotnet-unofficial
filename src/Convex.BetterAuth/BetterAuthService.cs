using System.Net.Http.Json;
using System.Text.Json;
using Convex.BetterAuth.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Convex.BetterAuth;

/// <summary>
/// Service for handling authentication with Better Auth via Convex.
/// </summary>
public class BetterAuthService : IBetterAuthService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ISessionStorage _sessionStorage;
    private readonly ILogger<BetterAuthService> _logger;
    private readonly BetterAuthOptions _options;

    private AuthSession? _currentSession;
    private AuthUser? _currentUser;

    // Rate limiting: minimum time between auth attempts
    private DateTime _lastAuthAttempt = DateTime.MinValue;
    private static readonly TimeSpan MinAuthInterval = TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public event Action? OnAuthStateChanged;

    /// <inheritdoc />
    public AuthUser? CurrentUser => _currentUser;

    /// <inheritdoc />
    public AuthSession? CurrentSession => _currentSession;

    /// <inheritdoc />
    public bool IsAuthenticated => _currentSession != null && _currentUser != null;

    /// <summary>
    /// Creates a new instance of <see cref="BetterAuthService"/>.
    /// </summary>
    public BetterAuthService(
        HttpClient httpClient,
        ISessionStorage sessionStorage,
        IOptions<BetterAuthOptions> options,
        ILogger<BetterAuthService> logger)
    {
        _httpClient = httpClient;
        _sessionStorage = sessionStorage;
        _options = options.Value;
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
    public async Task<AuthResult> SignUpAsync(string email, string password, string? name = null)
    {
        // Input validation
        var validationError = ValidateCredentials(email, password);
        if (validationError != null)
        {
            return AuthResult.Failure(validationError);
        }

        // Rate limiting
        var timeSinceLastAttempt = DateTime.UtcNow - _lastAuthAttempt;
        if (timeSinceLastAttempt < MinAuthInterval)
        {
            return AuthResult.Failure("Please wait a moment before trying again.");
        }
        _lastAuthAttempt = DateTime.UtcNow;

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
                request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                // Better Auth returns token directly, not wrapped in session
                var token = result?.Token ?? result?.Session?.Token;
                if (result?.User != null && !string.IsNullOrEmpty(token))
                {
                    _currentUser = result.User;
                    _currentSession = result.Session ?? new AuthSession { Token = token };

                    await _sessionStorage.StoreTokenAsync(token);

                    OnAuthStateChanged?.Invoke();
                    _logger.LogInformation("User signed up successfully: {Email}", email);
                    return AuthResult.Success();
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Sign up failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return AuthResult.Failure(ParseErrorMessage(errorContent) ?? "Sign up failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign up error for {Email}", email);
            return AuthResult.Failure("An unexpected error occurred. Please try again.");
        }
    }

    /// <inheritdoc />
    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        // Input validation
        var validationError = ValidateCredentials(email, password);
        if (validationError != null)
        {
            return AuthResult.Failure(validationError);
        }

        // Rate limiting
        var timeSinceLastAttempt = DateTime.UtcNow - _lastAuthAttempt;
        if (timeSinceLastAttempt < MinAuthInterval)
        {
            return AuthResult.Failure("Please wait a moment before trying again.");
        }
        _lastAuthAttempt = DateTime.UtcNow;

        try
        {
            var request = new SignInRequest
            {
                Email = email,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.SiteUrl}/api/auth/sign-in/email",
                request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                // Better Auth returns token directly, not wrapped in session
                var token = result?.Token ?? result?.Session?.Token;
                if (result?.User != null && !string.IsNullOrEmpty(token))
                {
                    _currentUser = result.User;
                    _currentSession = result.Session ?? new AuthSession { Token = token };

                    await _sessionStorage.StoreTokenAsync(token);

                    OnAuthStateChanged?.Invoke();
                    _logger.LogInformation("User signed in successfully: {Email}", email);
                    return AuthResult.Success();
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Sign in failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return AuthResult.Failure(ParseErrorMessage(errorContent) ?? "Invalid email or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign in error for {Email}", email);
            return AuthResult.Failure("An unexpected error occurred. Please try again.");
        }
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        try
        {
            if (_currentSession != null)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.SiteUrl}/api/auth/sign-out");
                request.Headers.Add("Authorization", $"Bearer {_currentSession.Token}");

                await _httpClient.SendAsync(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sign out request failed (ignoring)");
        }
        finally
        {
            _currentUser = null;
            _currentSession = null;

            await _sessionStorage.RemoveTokenAsync();

            OnAuthStateChanged?.Invoke();
            _logger.LogInformation("User signed out");
        }
    }

    /// <inheritdoc />
    public async Task TryRestoreSessionAsync()
    {
        try
        {
            var token = await _sessionStorage.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.SiteUrl}/api/auth/get-session");
            request.Headers.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                // Better Auth returns token directly, not wrapped in session
                var sessionToken = result?.Token ?? result?.Session?.Token ?? token;
                if (result?.User != null)
                {
                    _currentUser = result.User;
                    _currentSession = result.Session ?? new AuthSession { Token = sessionToken };

                    OnAuthStateChanged?.Invoke();
                    _logger.LogInformation("Session restored for user: {Email}", _currentUser.Email);
                    return;
                }
            }

            await _sessionStorage.RemoveTokenAsync();
            _logger.LogInformation("Stored session was invalid, removed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore session");
            await _sessionStorage.RemoveTokenAsync();
        }
    }

    /// <inheritdoc />
    public string? GetSessionToken()
    {
        return _currentSession?.Token;
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
            _logger.LogDebug(ex, "Failed to parse error message from response: {Content}", content);
        }
        return null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
