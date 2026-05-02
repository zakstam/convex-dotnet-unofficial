using System;
using Convex.Client.Infrastructure.Http;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class DeploymentUrlValidatorTests
{
    [Fact]
    public void Validate_RejectsInsecureNonLoopbackHttp()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            DeploymentUrlValidator.Validate("http://example.convex.cloud"));

        Assert.Contains("HTTPS", ex.Message);
    }

    [Theory]
    [InlineData("http://localhost:3210")]
    [InlineData("http://127.0.0.1:3210")]
    [InlineData("http://[::1]:3210")]
    public void Validate_AllowsLoopbackHttp(string url)
    {
        DeploymentUrlValidator.Validate(url);
    }

    [Fact]
    public void Validate_AllowsExplicitInsecureDevelopmentTransport()
    {
        DeploymentUrlValidator.Validate("http://example.convex.cloud", allowInsecureDevelopmentTransport: true);
    }

    [Fact]
    public void ToWebSocketUri_ConvertsHttpsToWss()
    {
        var uri = DeploymentUrlValidator.ToWebSocketUri("https://demo.convex.cloud", "api/1.27.3/sync");

        Assert.Equal("wss", uri.Scheme);
        Assert.Equal("demo.convex.cloud", uri.Host);
        Assert.Equal("/api/1.27.3/sync", uri.AbsolutePath);
    }

    [Fact]
    public void ToWebSocketUri_ConvertsLoopbackHttpToWs()
    {
        var uri = DeploymentUrlValidator.ToWebSocketUri("http://localhost:3210", "api/1.27.3/sync");

        Assert.Equal("ws", uri.Scheme);
        Assert.Equal("localhost", uri.Host);
        Assert.Equal(3210, uri.Port);
        Assert.Equal("/api/1.27.3/sync", uri.AbsolutePath);
    }

    [Fact]
    public void ConvexClientBuilder_RejectsInsecureNonLoopbackHttpByDefault()
    {
        var builder = new ConvexClientBuilder().UseDeployment("http://example.convex.cloud");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void ConvexClientBuilder_AllowsExplicitInsecureDevelopmentTransport()
    {
        var builder = new ConvexClientBuilder()
            .UseDeployment("http://example.convex.cloud")
            .AllowInsecureDevelopmentTransport();

        using var client = builder.Build();
        Assert.Equal("http://example.convex.cloud", client.DeploymentUrl);
    }
}
