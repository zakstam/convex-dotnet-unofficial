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

    /// <summary>
    /// When true, the session token is passed directly to Convex instead of
    /// exchanging it for a JWT.
    /// When false (default), the token provider exchanges the session token for
    /// a JWT via the /api/auth/convex/token endpoint provided by the convex() plugin.
    /// Default: false
    /// </summary>
    public bool UseSessionTokenDirectly { get; set; } = false;
}
