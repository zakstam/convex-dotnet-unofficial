using Convex.Client.Infrastructure.Common;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Security.Authentication;

/// <summary>
/// Internal implementation of authentication management.
/// Handles token storage, provider integration, and authentication state.
/// </summary>
internal sealed class AuthenticationManager(ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private string? _authToken;
    private string? _adminAuth;
    private IAuthTokenProvider? _authTokenProvider;
    private AuthenticationState _authenticationState = AuthenticationState.Unauthenticated;

    public AuthenticationState AuthenticationState => _authenticationState;
    public string? CurrentAuthToken => _authToken;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    public async Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token));

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            _authToken = token;
            _adminAuth = null;
            _authTokenProvider = null;

            UpdateAuthenticationState(AuthenticationState.Authenticated);
        }
        finally
        {
            _ = _authLock.Release();
        }
    }

    public async Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminKey))
            throw new ArgumentNullException(nameof(adminKey));

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            _adminAuth = adminKey;
            _authToken = null;
            _authTokenProvider = null;

            UpdateAuthenticationState(AuthenticationState.Authenticated);
        }
        finally
        {
            _ = _authLock.Release();
        }
    }

    public async Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("SetAuthTokenProviderAsync called with provider type: {ProviderType}", provider.GetType().Name);
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            _authTokenProvider = provider;
            _authToken = null;
            _adminAuth = null;

            UpdateAuthenticationState(AuthenticationState.Authenticated);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("Token provider registered successfully, auth state: {AuthState}", _authenticationState);
            }
        }
        finally
        {
            _ = _authLock.Release();
        }
    }

    public async Task ClearAuthAsync(CancellationToken cancellationToken = default)
    {
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            _authToken = null;
            _adminAuth = null;
            _authTokenProvider = null;

            UpdateAuthenticationState(AuthenticationState.Unauthenticated);
        }
        finally
        {
            _ = _authLock.Release();
        }
    }

    public async Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
    {
        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("GetAuthTokenAsync called - HasToken: {HasToken}, HasAdmin: {HasAdmin}, HasProvider: {HasProvider}",
                _authToken != null, _adminAuth != null, _authTokenProvider != null);
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            // Return existing token if available
            if (_authToken != null)
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("Returning cached token (length: {TokenLength})", _authToken.Length);
                }
                return _authToken;
            }

            // Return admin auth if available
            if (_adminAuth != null)
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("Returning admin auth");
                }
                return _adminAuth;
            }

            // Fetch from provider if available
            if (_authTokenProvider != null)
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("Calling token provider GetTokenAsync");
                }

                try
                {
                    var token = await _authTokenProvider.GetTokenAsync(cancellationToken);

                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogDebug("Token provider returned: {TokenResult}",
                            token != null ? $"token (length: {token.Length})" : "null");
                    }

                    if (token != null)
                    {
                        _authToken = token;
                        UpdateAuthenticationState(AuthenticationState.Authenticated);
                        return token;
                    }
                }
                catch (Exception ex)
                {
                    // Update state before rethrowing - the exception will propagate to caller
                    UpdateAuthenticationState(AuthenticationState.AuthenticationFailed, ex.Message);
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogDebug(ex, "Failed to get auth token from provider: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
                    }
                    throw;
                }
            }

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("No auth token available, returning null");
            }
            return null;
        }
        finally
        {
            _ = _authLock.Release();
        }
    }

    public async Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string>();
        var token = await GetAuthTokenAsync(cancellationToken);

        if (token != null)
        {
            headers["Authorization"] = $"Bearer {token}";
        }

        return headers;
    }

    private void UpdateAuthenticationState(AuthenticationState newState, string? errorMessage = null)
    {
        if (_authenticationState != newState)
        {
            _authenticationState = newState;
            AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(newState, errorMessage));
        }
    }
}
