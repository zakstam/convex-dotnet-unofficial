using System.Collections.Concurrent;
using Convex.Client.Shared.Common;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Extensions.Clerk;

/// <summary>
/// Authentication token provider that retrieves tokens from Clerk.
/// Implements <see cref="IAuthTokenProvider"/> to integrate with Convex client authentication.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClerkAuthTokenProvider"/> class.
/// </remarks>
/// <param name="clerkTokenService">The Clerk token service to use for token retrieval.</param>
/// <param name="options">The Clerk configuration options.</param>
/// <param name="logger">Optional logger for diagnostic information.</param>
public class ClerkAuthTokenProvider(
    IClerkTokenService clerkTokenService,
    ClerkOptions options,
    ILogger<ClerkAuthTokenProvider>? logger = null) : IAuthTokenProvider
{
    private readonly IClerkTokenService _clerkTokenService = clerkTokenService ?? throw new ArgumentNullException(nameof(clerkTokenService));
    private readonly ClerkOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger? _logger = logger;
    private readonly ConcurrentDictionary<string, CachedToken> _tokenCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("ClerkAuthTokenProvider.GetTokenAsync called");

            // Don't check IsAuthenticated/IsLoading here - let the token service handle it
            // The BlazorClerkTokenService.GetTokenAsync() will check and update auth state if needed
            // This allows the token service to refresh the auth state dynamically

            // Check cache if enabled
            if (_options.EnableTokenCaching)
            {
                var cachedToken = await GetCachedTokenAsync(cancellationToken);
                if (cachedToken != null)
                {
                    _logger?.LogDebug("Returning cached Clerk token.");
                    return cachedToken;
                }
            }

            // Fetch fresh token from Clerk
            // The token service will handle authentication checks and state updates
            var token = await _clerkTokenService.GetTokenAsync(_options.TokenTemplate, skipCache: false, cancellationToken);

            if (token != null && _options.EnableTokenCaching)
            {
                await CacheTokenAsync(token, cancellationToken);
            }

            if (token == null)
            {
                _logger?.LogDebug("Token service returned null - user may not be authenticated or Clerk may still be loading");
            }
            else
            {
                _logger?.LogDebug("Successfully retrieved token from Clerk (length: {TokenLength})", token.Length);
            }

            return token;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve token from Clerk: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<string?> GetCachedTokenAsync(CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_tokenCache.TryGetValue(_options.TokenTemplate, out var cached) &&
                cached.ExpiresAt > DateTime.UtcNow)
            {
                return cached.Token;
            }

            // Remove expired token
            _ = _tokenCache.TryRemove(_options.TokenTemplate, out _);
            return null;
        }
        finally
        {
            _ = _cacheLock.Release();
        }
    }

    private async Task CacheTokenAsync(string token, CancellationToken cancellationToken)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            _tokenCache[_options.TokenTemplate] = new CachedToken
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.Add(_options.TokenCacheExpiration)
            };
        }
        finally
        {
            _ = _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clears the token cache, forcing the next request to fetch a fresh token.
    /// </summary>
    public void ClearCache()
    {
        _tokenCache.Clear();
        _logger?.LogDebug("Cleared Clerk token cache.");
    }

    private sealed class CachedToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

