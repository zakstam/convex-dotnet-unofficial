namespace Convex.Client.Extensions.DependencyInjection;

/// <summary>
/// Factory for creating named Convex client instances.
/// Useful when your application needs to connect to multiple Convex deployments.
/// </summary>
public interface IConvexClientFactory
{
    /// <summary>
    /// Creates a Convex client instance for the specified name.
    /// </summary>
    /// <param name="name">
    /// The logical name of the client to create.
    /// If not specified, the default (unnamed) client is returned.
    /// </param>
    /// <returns>A configured IConvexClient instance.</returns>
    IConvexClient CreateClient(string? name = null);
}
