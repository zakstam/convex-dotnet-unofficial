using System.Threading.Tasks;
using Convex.Client.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class DependencyInjectionAuthIsolationTests
{
    [Fact]
    public async Task DefaultClientResolution_ReturnsFreshClients_WithIsolatedAuthState()
    {
        var services = new ServiceCollection();
        services.AddConvex(options =>
        {
            options.DeploymentUrl = "https://example.convex.cloud";
            options.EnableAutoReconnect = false;
        });

        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IConvexClient>();
        var second = provider.GetRequiredService<IConvexClient>();

        Assert.NotSame(first, second);

        await first.Auth.SetAuthTokenAsync("first-user-token");

        Assert.Equal("first-user-token", await first.Auth.GetAuthTokenAsync());
        Assert.Null(await second.Auth.GetAuthTokenAsync());
    }

    [Fact]
    public void NamedFactoryResolution_PreservesExplicitSharedClientCaching()
    {
        var services = new ServiceCollection();
        services.AddConvex("shared", options =>
        {
            options.DeploymentUrl = "https://example.convex.cloud";
            options.EnableAutoReconnect = false;
        });

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IConvexClientFactory>();

        var first = factory.CreateClient("shared");
        var second = factory.CreateClient("shared");

        Assert.Same(first, second);
    }
}
