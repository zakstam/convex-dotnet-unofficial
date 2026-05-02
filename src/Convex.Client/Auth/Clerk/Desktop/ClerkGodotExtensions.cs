using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Extension methods for integrating Clerk authentication with Convex client in Godot applications.
/// </summary>
public static class ClerkGodotExtensions
{
    /// <summary>
    /// Configures the Convex client to use Clerk authentication with the specified token service and options.
    /// </summary>
    /// <param name="client">The Convex client to configure.</param>
    /// <param name="tokenService">The Godot Clerk token service.</param>
    /// <param name="options">The Clerk configuration options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task AddClerkAuthToConvexClientAsync(
        this IConvexClient client,
        GodotClerkTokenService tokenService,
        ClerkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (tokenService == null)
        {
            throw new ArgumentNullException(nameof(tokenService));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // Create a token provider that wraps the Godot token service
        var tokenProvider = new ClerkAuthTokenProvider(tokenService, options);

        await client.Auth.SetAuthTokenProviderAsync(tokenProvider, cancellationToken);
    }

    /// <summary>
    /// Creates token provider.
    /// </summary>
    /// <param name="tokenService">The Godot Clerk token service.</param>
    /// <param name="options">The Clerk options.</param>
    /// <returns>A Clerk authentication token provider.</returns>
    public static ClerkAuthTokenProvider CreateTokenProvider(
        this GodotClerkTokenService tokenService,
        ClerkOptions options)
    {
        if (tokenService == null)
        {
            throw new ArgumentNullException(nameof(tokenService));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new ClerkAuthTokenProvider(tokenService, options);
    }
}

