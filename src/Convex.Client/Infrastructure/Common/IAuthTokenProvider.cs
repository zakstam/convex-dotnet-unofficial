namespace Convex.Client.Infrastructure.Common;

/// <summary>
/// Interface for providing authentication tokens.
/// This is defined in Shared to allow configuration infrastructure to use it.
/// </summary>
public interface IAuthTokenProvider
{
    /// <summary>
    /// Gets the current authentication token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The current authentication token, or null if not available.</returns>
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
