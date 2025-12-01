using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for connection management and state.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class ConnectionTests
{
    private readonly ConvexFixture _fixture;

    public ConnectionTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Client_ShouldBeCreated()
    {
        // The fixture creates the client on initialization
        Assert.NotNull(_fixture.Client);
    }

    [Fact]
    public void DeploymentUrl_ShouldBeConfigured()
    {
        Assert.False(string.IsNullOrEmpty(_fixture.DeploymentUrl));
        Assert.StartsWith("https://", _fixture.DeploymentUrl);
        Assert.Contains(".convex.cloud", _fixture.DeploymentUrl);
    }

    [Fact]
    public void TestRunId_ShouldBeUnique()
    {
        Assert.False(string.IsNullOrEmpty(_fixture.TestRunId));
        Assert.True(Guid.TryParse(_fixture.TestRunId, out _));
    }

    [Fact]
    public async Task Client_ShouldExecuteSimpleQuery()
    {
        // This tests basic connectivity by executing a query
        // Even if no items exist, the query should succeed
        var result = await _fixture.Client
            .Query<object[]>("testQueries:list")
            .WithArgs(new { testRunId = _fixture.TestRunId })
            .ExecuteAsync();

        Assert.NotNull(result);
    }
}
