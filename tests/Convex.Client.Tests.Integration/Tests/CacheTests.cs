using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for query caching and cache invalidation.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class CacheTests
{
    private readonly ConvexFixture _fixture;

    public CacheTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Cache_ShouldBeAccessible()
    {
        // Verify the cache interface is accessible
        var cache = _fixture.Client.Cache;
        Assert.NotNull(cache);
    }

    [Fact]
    public void GetCachedValue_BeforeQuery_ShouldReturnDefault()
    {
        // Before any query, cached value should be default/null
        var cached = _fixture.Client.GetCachedValue<TestItem[]>("testQueries:nonCachedQuery");
        Assert.Null(cached);
    }

    [Fact]
    public void TryGetCachedValue_BeforeQuery_ShouldReturnFalse()
    {
        var found = _fixture.Client.TryGetCachedValue<TestItem[]>("testQueries:anotherNonCachedQuery", out var value);
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public async Task InvalidateQueryAsync_ShouldNotThrow()
    {
        // Invalidating a query should not throw, even if it doesn't exist in cache
        await _fixture.Client.InvalidateQueryAsync("testQueries:list");
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public async Task InvalidateQueriesAsync_WithPattern_ShouldNotThrow()
    {
        // Invalidating with a pattern should not throw
        await _fixture.Client.InvalidateQueriesAsync("testQueries:*");
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void DefineQueryDependency_ShouldNotThrow()
    {
        // Defining dependencies should not throw
        _fixture.Client.DefineQueryDependency("testMutations:create", "testQueries:list", "testQueries:count");
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public async Task InvalidateQueriesAsync_MultiplePatterns_ShouldWork()
    {
        // Test various invalidation patterns
        await _fixture.Client.InvalidateQueriesAsync("testQueries:list");
        await _fixture.Client.InvalidateQueriesAsync("test*:*");
        await _fixture.Client.InvalidateQueriesAsync("*");
        Assert.True(true);
    }

    [Fact]
    public async Task QueryAfterInvalidation_ShouldFetchFreshData()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        try
        {
            // Execute initial query
            var initial = await _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();

            Assert.Empty(initial);

            // Create an item
            await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = uniqueTestRunId, name = "Cache Test", value = 100.0 })
                .ExecuteAsync();

            // Invalidate the query
            await _fixture.Client.InvalidateQueryAsync("testQueries:list");

            // Query again - should get fresh data
            var afterInvalidation = await _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();

            Assert.NotEmpty(afterInvalidation);
            Assert.Contains(afterInvalidation, item => item.name == "Cache Test");
        }
        finally
        {
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }
}
