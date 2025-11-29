using Convex.Client.Infrastructure.Common;

namespace Convex.Client.Features.Security.Authentication;

/// <summary>
/// Manages authentication state and token handling for Convex clients.
/// Provides support for static tokens, admin keys, and token providers with automatic refresh.
/// Thread-safe for concurrent authentication operations.
/// </summary>
/// <remarks>
/// <para>
/// The authentication system supports three modes:
/// <list type="bullet">
/// <item><strong>Static tokens</strong> - Set a JWT token directly using <see cref="SetAuthTokenAsync(string, CancellationToken)"/></item>
/// <item><strong>Admin keys</strong> - Use admin authentication for server-side operations via <see cref="SetAdminAuthAsync(string, CancellationToken)"/></item>
/// <item><strong>Token providers</strong> - Use a provider for automatic token refresh via <see cref="SetAuthTokenProviderAsync(IAuthTokenProvider, CancellationToken)"/></item>
/// </list>
/// </para>
/// <para>
/// Authentication state changes are exposed via the <see cref="AuthenticationStateChanged"/> event,
/// allowing you to react to authentication state transitions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Set static token
/// await client.Auth.SetAuthTokenAsync("your-jwt-token");
///
/// // Use token provider for automatic refresh
/// var provider = new MyTokenProvider();
/// await client.Auth.SetAuthTokenProviderAsync(provider);
///
/// // Monitor authentication state
/// client.Auth.AuthenticationStateChanged += (sender, e) => {
///     Console.WriteLine($"Auth state: {e.State}");
///     if (e.State == AuthenticationState.TokenExpired)
///     {
///         // Handle token expiration
///     }
/// };
/// </code>
/// </example>
/// <seealso cref="AuthenticationSlice"/>
/// <seealso cref="IAuthTokenProvider"/>
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
    /// Use this for simple authentication scenarios where you have a static JWT token.
    /// For automatic token refresh, use <see cref="SetAuthTokenProviderAsync(IAuthTokenProvider, CancellationToken)"/> instead.
    /// </summary>
    /// <param name="token">The JWT authentication token. Must not be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that completes when the token is set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null or empty.</exception>
    /// <remarks>
    /// Setting a token will update the <see cref="AuthenticationState"/> to <see cref="AuthenticationState.Authenticated"/>
    /// and raise the <see cref="AuthenticationStateChanged"/> event.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set authentication token
    /// await client.Auth.SetAuthTokenAsync("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    ///
    /// // Check authentication state
    /// if (client.Auth.AuthenticationState == AuthenticationState.Authenticated)
    /// {
    ///     Console.WriteLine("Successfully authenticated");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="SetAuthTokenProviderAsync(IAuthTokenProvider, CancellationToken)"/>
    /// <seealso cref="ClearAuthAsync(CancellationToken)"/>
    Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the admin authentication key for this client.
    /// Admin keys provide privileged access and should only be used in server-side applications.
    /// Never expose admin keys in client applications or browser code.
    /// </summary>
    /// <param name="adminKey">The admin authentication key from your Convex dashboard. Must not be null or empty.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that completes when the admin key is set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="adminKey"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// Admin authentication bypasses user-level permissions and should only be used for:
    /// <list type="bullet">
    /// <item>Server-side operations</item>
    /// <item>Background jobs</item>
    /// <item>Administrative tasks</item>
    /// </list>
    /// </para>
    /// <para>
    /// Setting an admin key will update the <see cref="AuthenticationState"/> to <see cref="AuthenticationState.Authenticated"/>
    /// and raise the <see cref="AuthenticationStateChanged"/> event.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Set admin key (server-side only!)
    /// await client.Auth.SetAdminAuthAsync("your-admin-key");
    ///
    /// // Now you can perform privileged operations
    /// var allUsers = await client.Query&lt;List&lt;User&gt;&gt;("admin:getAllUsers").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="SetAuthTokenAsync(string, CancellationToken)"/>
    Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an authentication token provider for automatic token management.
    /// The provider will be called whenever a token is needed, allowing for automatic token refresh.
    /// This is the recommended approach for applications that need to handle token expiration.
    /// </summary>
    /// <param name="provider">The authentication token provider that supplies tokens on demand. Must not be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that completes when the provider is set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Token providers are useful when:
    /// <list type="bullet">
    /// <item>Tokens expire and need to be refreshed</item>
    /// <item>Tokens are stored securely and retrieved on demand</item>
    /// <item>You want to implement custom token refresh logic</item>
    /// </list>
    /// </para>
    /// <para>
    /// The provider's <see cref="IAuthTokenProvider.GetTokenAsync(CancellationToken)"/> method will be called
    /// whenever authentication is needed for a request.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a token provider
    /// public class MyTokenProvider : IAuthTokenProvider
    /// {
    ///     public async Task&lt;string?&gt; GetTokenAsync(CancellationToken cancellationToken = default)
    ///     {
    ///         // Get token from secure storage, refresh if needed, etc.
    ///         return await GetValidTokenAsync();
    ///     }
    /// }
    ///
    /// // Set the provider
    /// await client.Auth.SetAuthTokenProviderAsync(new MyTokenProvider());
    ///
    /// // Token will be automatically retrieved when needed
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos").ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="IAuthTokenProvider"/>
    /// <seealso cref="SetAuthTokenAsync(string, CancellationToken)"/>
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
