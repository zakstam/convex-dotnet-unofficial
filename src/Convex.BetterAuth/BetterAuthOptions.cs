namespace Convex.BetterAuth;

/// <summary>
/// Configuration options for Better Auth.
/// </summary>
public class BetterAuthOptions
{
    /// <summary>
    /// The URL of your Convex site (e.g., "https://your-deployment.convex.site").
    /// This is where the Better Auth HTTP endpoints are hosted.
    /// </summary>
    public string SiteUrl { get; set; } = "";
}
