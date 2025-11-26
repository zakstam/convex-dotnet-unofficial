using Convex.Client.Infrastructure.Common;

namespace Convex.Client.Extensions.Clerk;

/// <summary>
/// Extension methods for configuring Clerk authentication on Convex clients.
/// </summary>
public static class ClerkExtensions
{
    /// <summary>
    /// Configures the Convex client to use Clerk authentication with the specified token provider.
    /// </summary>
    /// <param name="client">The Convex client to configure.</param>
    /// <param name="tokenProvider">The Clerk authentication token provider.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// var clerkProvider = new ClerkAuthTokenProvider(clerkTokenService, clerkOptions);
    /// await client.UseClerkAuthAsync(clerkProvider);
    /// </code>
    /// </example>
    public static async Task UseClerkAuthAsync(
        this IConvexClient client,
        IAuthTokenProvider tokenProvider,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (tokenProvider == null)
        {
            throw new ArgumentNullException(nameof(tokenProvider));
        }

        if (client is ConvexClient convexClient)
        {
            await convexClient.AuthenticationSlice.SetAuthTokenProviderAsync(tokenProvider, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                "Clerk authentication can only be configured on ConvexClient instances. " +
                "The provided client does not support AuthenticationSlice.");
        }
    }

    /// <summary>
    /// Configures the Convex client to use Clerk authentication with the specified Clerk token service and options.
    /// </summary>
    /// <param name="client">The Convex client to configure.</param>
    /// <param name="clerkTokenService">The Clerk token service.</param>
    /// <param name="options">The Clerk configuration options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <example>
    /// <code>
    /// await client.UseClerkAuthAsync(clerkTokenService, clerkOptions);
    /// </code>
    /// </example>
    public static async Task UseClerkAuthAsync(
        this IConvexClient client,
        IClerkTokenService clerkTokenService,
        ClerkOptions options,
        CancellationToken cancellationToken = default)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (clerkTokenService == null)
        {
            throw new ArgumentNullException(nameof(clerkTokenService));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var tokenProvider = new ClerkAuthTokenProvider(clerkTokenService, options);

        if (client is ConvexClient convexClient)
        {
            await convexClient.AuthenticationSlice.SetAuthTokenProviderAsync(tokenProvider, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                "Clerk authentication can only be configured on ConvexClient instances. " +
                "The provided client does not support AuthenticationSlice.");
        }
    }
}

