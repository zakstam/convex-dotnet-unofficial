using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for query operations.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class QueryTests
{
    private readonly ConvexFixture _fixture;

    public QueryTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task List_WithNoItems_ShouldReturnEmptyArray()
    {
        // Use a unique testRunId that no other test has used
        var uniqueTestRunId = Guid.NewGuid().ToString();

        // Query for items with this unique ID (should be empty)
        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task List_AfterCreatingItem_ShouldReturnItem()
    {
        // Create an item first
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "QueryTest Item", value = 42.5 })
            .ExecuteAsync();

        Assert.NotNull(itemId);

        // Now query and verify the item exists
        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = _fixture.TestRunId })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Contains(result, item => item.name == "QueryTest Item" && item.value == 42.5);
    }

    [Fact]
    public async Task Get_WithValidId_ShouldReturnItem()
    {
        // Create an item first
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "GetTest Item", value = 100.0 })
            .ExecuteAsync();

        Assert.NotNull(itemId);

        // Get the specific item by ID
        var result = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal("GetTest Item", result.name);
        Assert.Equal(100.0, result.value);
    }

    [Fact]
    public async Task Get_WithNonExistentId_ShouldReturnNull()
    {
        // Create and then delete an item to get a valid but non-existent ID
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Temp Item", value = 0.0 })
            .ExecuteAsync();

        await _fixture.Client
            .Mutate<object?>("testMutations:deleteItem")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        // Now query for the deleted item - should return null
        var result = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.Null(result);
    }
}

/// <summary>
/// Represents a test item from the Convex backend.
/// </summary>
public class TestItem
{
    public string _id { get; set; } = string.Empty;
    public double _creationTime { get; set; }
    public string testRunId { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public double value { get; set; }
}
