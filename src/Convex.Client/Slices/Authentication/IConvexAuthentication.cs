using Convex.Client.Shared.Common;

namespace Convex.Client.Slices.Authentication;

/// <summary>
/// Manages authentication state and token handling for Convex clients.
/// Thread-safe for concurrent authentication operations.
/// </summary>
public interface IConvexAuthentication
{
    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    AuthenticationState AuthenticationState { get; }

    /// <summary>
    /// Gets the current authentication token if set.
    /// </summary>
    string? CurrentAuthToken { get; }

    /// <summary>
    /// Event fired when authentication state changes.
    /// </summary>
    event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

    /// <summary>
    /// Sets the authentication token for this client.
    /// </summary>
    /// <param name="token">The JWT authentication token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the admin authentication key for this client.
    /// </summary>
    /// <param name="adminKey">The admin authentication key.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an authentication token provider for automatic token management.
    /// </summary>
    /// <param name="provider">The authentication token provider.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all authentication information.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    Task ClearAuthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authentication token, fetching from provider if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The authentication token or null if not authenticated.</returns>
    Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authentication headers to include in HTTP requests.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Dictionary of authentication headers.</returns>
    Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Enumeration of authentication states.
/// </summary>
public enum AuthenticationState
{
    /// <summary>
    /// No authentication is configured.
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// Authentication is configured and valid.
    /// </summary>
    Authenticated,

    /// <summary>
    /// Authentication failed or token is invalid.
    /// </summary>
    AuthenticationFailed,

    /// <summary>
    /// Authentication token has expired.
    /// </summary>
    TokenExpired
}

/// <summary>
/// Event arguments for authentication state changes.
/// </summary>
public class AuthenticationStateChangedEventArgs(AuthenticationState state, string? errorMessage = null) : EventArgs
{
    /// <summary>
    /// Gets the new authentication state.
    /// </summary>
    public AuthenticationState State { get; } = state;

    /// <summary>
    /// Gets the optional error message if authentication failed.
    /// </summary>
    public string? ErrorMessage { get; } = errorMessage;
}
