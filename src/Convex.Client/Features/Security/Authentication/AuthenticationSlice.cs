using System.Diagnostics;
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

    public AuthenticationState AuthenticationState => _implementation.AuthenticationState;

    public string? CurrentAuthToken => _implementation.CurrentAuthToken;

    public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged
    {
        add => _implementation.AuthenticationStateChanged += value;
        remove => _implementation.AuthenticationStateChanged -= value;
    }

    public Task SetAuthTokenAsync(string token, CancellationToken cancellationToken = default)
        => _implementation.SetAuthTokenAsync(token, cancellationToken);

    public Task SetAdminAuthAsync(string adminKey, CancellationToken cancellationToken = default)
        => _implementation.SetAdminAuthAsync(adminKey, cancellationToken);

    public Task SetAuthTokenProviderAsync(IAuthTokenProvider provider, CancellationToken cancellationToken = default)
    {
        var providerType = provider?.GetType().Name ?? "null";
        var msg = $"[AuthenticationSlice] SetAuthTokenProviderAsync called with {providerType}";
        Debug.WriteLine(msg);
        Console.WriteLine(msg);

        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        return _implementation.SetAuthTokenProviderAsync(provider, cancellationToken);
    }

    public Task ClearAuthAsync(CancellationToken cancellationToken = default)
        => _implementation.ClearAuthAsync(cancellationToken);

    public Task<string?> GetAuthTokenAsync(CancellationToken cancellationToken = default)
        => _implementation.GetAuthTokenAsync(cancellationToken);

    public Task<Dictionary<string, string>> GetAuthHeadersAsync(CancellationToken cancellationToken = default)
        => _implementation.GetAuthHeadersAsync(cancellationToken);
}
