namespace Convex.Client.Extensions.Clerk;

/// <summary>
/// Interface for retrieving authentication tokens from Clerk.
/// This abstraction allows the integration to work with different Clerk SDK implementations.
/// </summary>
public interface IClerkTokenService
{
    /// <summary>
    /// Gets the current authentication token from Clerk.
    /// </summary>
    /// <param name="tokenTemplate">The JWT template name (default: "convex").</param>
    /// <param name="skipCache">Whether to skip cache and force a fresh token.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The authentication token, or null if not available.</returns>
    Task<string?> GetTokenAsync(string tokenTemplate = "convex", bool skipCache = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets whether the authentication state is still loading.
    /// </summary>
    /// <returns>True if loading, false otherwise.</returns>
    bool IsLoading { get; }
}

