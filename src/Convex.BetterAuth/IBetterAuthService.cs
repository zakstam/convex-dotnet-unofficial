using Convex.BetterAuth.Models;

namespace Convex.BetterAuth;

/// <summary>
/// Service interface for Better Auth authentication operations.
/// </summary>
public interface IBetterAuthService
{
    /// <summary>
    /// Gets the currently authenticated user, or null if not authenticated.
    /// </summary>
    AuthUser? CurrentUser { get; }

    /// <summary>
    /// Gets the current session, or null if not authenticated.
    /// </summary>
    AuthSession? CurrentSession { get; }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Event raised when the authentication state changes.
    /// </summary>
    event Action? OnAuthStateChanged;

    /// <summary>
    /// Signs up a new user with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="name">Optional display name. Defaults to the email username.</param>
    /// <returns>The result of the sign-up operation.</returns>
    Task<AuthResult> SignUpAsync(string email, string password, string? name = null);

    /// <summary>
    /// Signs up a new user with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="name">Optional display name. Defaults to the email username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the sign-up operation.</returns>
    Task<AuthResult> SignUpAsync(string email, string password, string? name, CancellationToken cancellationToken);

    /// <summary>
    /// Signs in a user with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>The result of the sign-in operation.</returns>
    Task<AuthResult> SignInAsync(string email, string password);

    /// <summary>
    /// Signs in a user with email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the sign-in operation.</returns>
    Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SignOutAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to restore a session from storage.
    /// Call this on application startup to restore existing sessions.
    /// </summary>
    Task TryRestoreSessionAsync();

    /// <summary>
    /// Attempts to restore a session from storage.
    /// Call this on application startup to restore existing sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task TryRestoreSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current session token for use with authenticated requests.
    /// </summary>
    /// <returns>The session token, or null if not authenticated.</returns>
    string? GetSessionToken();
}
