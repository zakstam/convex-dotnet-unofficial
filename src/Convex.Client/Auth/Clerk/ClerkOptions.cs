namespace Convex.Client.Extensions.Clerk;

/// <summary>
/// Configuration options for Clerk authentication integration.
/// </summary>
public class ClerkOptions
{
    /// <summary>
    /// Gets or sets the publishable key.
    /// This is optional and only needed for client-side token retrieval.
    /// </summary>
    public string? PublishableKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key.
    /// This is required for server-side token operations.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the token template.
    /// Default is "convex" to match Convex backend expectations.
    /// </summary>
    public string TokenTemplate { get; set; } = "convex";

    /// <summary>
    /// Gets or sets a value indicating whether enable token caching.
    /// When enabled, tokens are cached to avoid excessive Clerk API calls.
    /// Default is true.
    /// </summary>
    public bool EnableTokenCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the from minutes.
    /// Tokens are considered expired after this duration and will be refreshed.
    /// Default is 5 minutes.
    /// </summary>
    public TimeSpan TokenCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the cancellation token.
    /// If provided, this will be used instead of the default Clerk SDK token retrieval.
    /// This allows for custom integration with different Clerk SDK implementations.
    /// </summary>
    public Func<CancellationToken, Task<string?>>? CustomTokenRetriever { get; set; }

    /// <summary>
    /// Gets or sets the clerk domain.
    /// For development: {instance-name}.clerk.accounts.dev
    /// For production: clerk.{yourdomain}.com
    /// If not set, will attempt to use default.
    /// </summary>
    public string? ClerkDomain { get; set; }

    /// <summary>
    /// Gets or sets the callback port.
    /// Default is 8080.
    /// </summary>
    public int CallbackPort { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the callback path.
    /// Default is "/callback".
    /// </summary>
    public string CallbackPath { get; set; } = "/callback";

    /// <summary>
    /// Gets or sets the o auth client ID.
    /// This is the Client ID from your OAuth Application in Clerk Dashboard.
    /// Different from PublishableKey - this is specifically for OAuth flows.
    /// </summary>
    public string? OAuthClientId { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(PublishableKey) && string.IsNullOrWhiteSpace(SecretKey) && CustomTokenRetriever == null)
        {
            throw new InvalidOperationException(
                "Either PublishableKey, SecretKey, or CustomTokenRetriever must be configured. " +
                "Set at least one via options or in configuration.");
        }

        if (string.IsNullOrWhiteSpace(TokenTemplate))
        {
            throw new InvalidOperationException("TokenTemplate cannot be null or empty.");
        }

        if (TokenCacheExpiration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"TokenCacheExpiration must be greater than zero, got {TokenCacheExpiration}.");
        }
    }
}

