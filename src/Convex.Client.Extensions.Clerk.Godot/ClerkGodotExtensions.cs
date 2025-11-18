using System;
using System.Diagnostics;
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
        // Use both Debug.WriteLine and Console.WriteLine for maximum visibility
        var startMsg = "[ClerkGodotExtensions] AddClerkAuthToConvexClientAsync START";
        Debug.WriteLine(startMsg);
        Console.WriteLine(startMsg);

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

        var createMsg = $"[ClerkGodotExtensions] Creating ClerkAuthTokenProvider...";
        Debug.WriteLine(createMsg);
        Console.WriteLine(createMsg);

        // Create a token provider that wraps the Godot token service
        var tokenProvider = new ClerkAuthTokenProvider(tokenService, options);

        var checkMsg = $"[ClerkGodotExtensions] Checking if client is ConvexClient: {client is ConvexClient}";
        Debug.WriteLine(checkMsg);
        Console.WriteLine(checkMsg);

        var typeMsg = $"[ClerkGodotExtensions] Client type: {client.GetType().Name}";
        Debug.WriteLine(typeMsg);
        Console.WriteLine(typeMsg);

        if (client is ConvexClient convexClient)
        {
            var callMsg = $"[ClerkGodotExtensions] Calling SetAuthTokenProviderAsync...";
            Debug.WriteLine(callMsg);
            Console.WriteLine(callMsg);

            await convexClient.AuthenticationSlice.SetAuthTokenProviderAsync(tokenProvider, cancellationToken);

            var completeMsg = $"[ClerkGodotExtensions] SetAuthTokenProviderAsync completed!";
            Debug.WriteLine(completeMsg);
            Console.WriteLine(completeMsg);
        }
        else
        {
            var errMsg = $"[ClerkGodotExtensions] ERROR: Client is not ConvexClient!";
            Debug.WriteLine(errMsg);
            Console.WriteLine(errMsg);
            throw new InvalidOperationException(
                "Clerk authentication can only be configured on ConvexClient instances. " +
                "The provided client does not support AuthenticationSlice.");
        }

        var endMsg = "[ClerkGodotExtensions] AddClerkAuthToConvexClientAsync COMPLETE";
        Debug.WriteLine(endMsg);
        Console.WriteLine(endMsg);
    }

    /// <summary>
    /// Creates a Clerk token provider from a Godot token service and options.
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

