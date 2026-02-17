using Convex.Client.Infrastructure.Common;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Security.Authentication;

/// <summary>
/// Authentication slice - provides authentication state management and token handling.
/// This is a self-contained vertical slice that handles all authentication functionality.
/// </summary>
public class AuthenticationSlice(ILogger? logger = null, bool enableDebugLogging = false) : IConvexAuthentication
{
    private readonly AuthenticationManager _implementation = new AuthenticationManager(logger, enableDebugLogging);

    /// <summary>
    /// Gets the authentication state.
    /// </summary>
    public AuthenticationState AuthenticationState => _implementation.AuthenticationState;

    /// <summary>
    /// Gets the current auth token.
    /// </summary>
    public string? CurrentAuthToken => _implementation.CurrentAuthToken;

    /// <summary>
    /// Occurs when the authentication state changes.
    /// </summary>
    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged
    {
        add => _implementation.AuthenticationStateChanged += value;
        remove => _implementation.AuthenticationStateChanged -= value;
    }

    /// <summary>
    /// Sets auth token.
    /// </summary>
    public Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default)
        => _implementation.SetAuthTokenAsync(token, cancellationToken);

    /// <summary>
    /// Sets admin auth.
    /// </summary>
    public Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default)
        => _implementation.SetAdminAuthAsync(adminKey, cancellationToken);

    /// <summary>
    /// Sets auth token provider.
    /// </summary>
    public Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));
        return _implementation.SetAuthTokenProviderAsync(provider, cancellationToken);
    }

    /// <summary>
    /// Clears any currently configured authentication state.
    /// </summary>
    public Task ClearAuthAsync(CancellationToken cancellationToken = default)
        => _implementation.ClearAuthAsync(cancellationToken);

    /// <summary>
    /// Gets auth token.
    /// </summary>
    public Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        => _implementation.GetAuthTokenAsync(cancellationToken);

    /// <summary>
    /// Gets auth headers.
    /// </summary>
    public Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default)
        => _implementation.GetAuthHeadersAsync(cancellationToken);
}
