namespace Convex.BetterAuth;

/// <summary>
/// Interface for storing and retrieving session tokens.
/// Implement this interface to provide custom storage (e.g., localStorage, secure storage, etc.).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Security Considerations:</strong>
/// </para>
/// <para>
/// Session tokens stored in browser-accessible storage (localStorage, sessionStorage) are
/// vulnerable to Cross-Site Scripting (XSS) attacks. If an attacker can execute JavaScript
/// on your page, they can steal tokens from browser storage.
/// </para>
/// <para>
/// <strong>Recommended mitigations:</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Implement a strong Content Security Policy (CSP) to prevent XSS attacks.
/// </description>
/// </item>
/// <item>
/// <description>
/// Use sessionStorage instead of localStorage for shorter token exposure (tokens cleared on tab close).
/// </description>
/// </item>
/// <item>
/// <description>
/// Consider HTTP-only cookies for server-rendered applications (not applicable for Blazor WASM).
/// </description>
/// </item>
/// <item>
/// <description>
/// Implement token rotation and short expiry times to limit exposure window.
/// </description>
/// </item>
/// <item>
/// <description>
/// Sanitize all user input and use trusted libraries for rendering dynamic content.
/// </description>
/// </item>
/// </list>
/// <para>
/// For Blazor WebAssembly applications, browser storage is the only option for client-side
/// token persistence. Focus on XSS prevention through CSP and input sanitization.
/// </para>
/// </remarks>
public interface ISessionStorage
{
    /// <summary>
    /// Stores a session token securely.
    /// </summary>
    /// <param name="token">The token to store.</param>
    /// <remarks>
    /// Implementations should ensure the token is stored in a way that minimizes
    /// exposure to XSS attacks. Never log or expose the token value.
    /// </remarks>
    Task StoreTokenAsync(string token);

    /// <summary>
    /// Retrieves the stored session token.
    /// </summary>
    /// <returns>The stored token, or null if not found.</returns>
    /// <remarks>
    /// The returned token should be treated as sensitive and never logged or
    /// exposed to untrusted code.
    /// </remarks>
    Task<string?> GetTokenAsync();

    /// <summary>
    /// Removes the stored session token.
    /// </summary>
    /// <remarks>
    /// This should be called during sign-out to ensure tokens are not left
    /// in storage after the user has logged out.
    /// </remarks>
    Task RemoveTokenAsync();
}
