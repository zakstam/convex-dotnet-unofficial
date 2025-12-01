using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for mutation operations.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class MutationTests
{
    private readonly ConvexFixture _fixture;

    public MutationTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_ShouldReturnNewItemId()
    {
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Create Test", value = 1.0 })
            .ExecuteAsync();

        Assert.NotNull(itemId);
        Assert.NotEmpty(itemId);
    }

    [Fact]
    public async Task Create_MultipleTimes_ShouldCreateMultipleItems()
    {
        // Create first item
        var id1 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Multi Item 1", value = 10.0 })
            .ExecuteAsync();

        // Create second item
        var id2 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Multi Item 2", value = 20.0 })
            .ExecuteAsync();

        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEqual(id1, id2);

        // Verify both items exist
        var items = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = _fixture.TestRunId })
            .ExecuteAsync();

        Assert.NotNull(items);
        Assert.Contains(items, i => i.name == "Multi Item 1");
        Assert.Contains(items, i => i.name == "Multi Item 2");
    }

    [Fact]
    public async Task Update_ShouldModifyItem()
    {
        // Create an item
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Before Update", value = 50.0 })
            .ExecuteAsync();

        Assert.NotNull(itemId);

        // Update the item
        await _fixture.Client
            .Mutate<object?>("testMutations:update")
            .WithArgs(new { id = itemId, name = "After Update", value = 999.0 })
            .ExecuteAsync();

        // Verify the update
        var updated = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.NotNull(updated);
        Assert.Equal("After Update", updated.name);
        Assert.Equal(999.0, updated.value);
    }

    [Fact]
    public async Task Update_PartialUpdate_ShouldOnlyModifySpecifiedFields()
    {
        // Create an item
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Partial Update Test", value = 100.0 })
            .ExecuteAsync();

        Assert.NotNull(itemId);

        // Update only the name
        await _fixture.Client
            .Mutate<object?>("testMutations:update")
            .WithArgs(new { id = itemId, name = "Updated Name Only" })
            .ExecuteAsync();

        // Verify only name changed, value remains
        var updated = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.NotNull(updated);
        Assert.Equal("Updated Name Only", updated.name);
        Assert.Equal(100.0, updated.value);
    }

    [Fact]
    public async Task Delete_ShouldRemoveItem()
    {
        // Create an item
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "To Be Deleted", value = 0.0 })
            .ExecuteAsync();

        Assert.NotNull(itemId);

        // Verify it exists
        var existing = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.NotNull(existing);

        // Delete the item
        await _fixture.Client
            .Mutate<object?>("testMutations:deleteItem")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        // Verify it no longer exists
        var deleted = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.Null(deleted);
    }

    [Fact]
    public async Task Cleanup_ShouldRemoveAllTestRunItems()
    {
        // Create multiple items for this test's unique test run
        var uniqueTestRunId = Guid.NewGuid().ToString();

        for (int i = 0; i < 3; i++)
        {
            await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = uniqueTestRunId, name = $"Cleanup Item {i}", value = (double)i })
                .ExecuteAsync();
        }

        // Verify items exist
        var before = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteAsync();

        Assert.NotNull(before);
        Assert.Equal(3, before.Length);

        // Run cleanup
        var deletedCount = await _fixture.Client
            .Mutate<double>("testMutations:cleanup")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteAsync();

        Assert.Equal(3.0, deletedCount);

        // Verify all items are gone
        var after = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteAsync();

        Assert.NotNull(after);
        Assert.Empty(after);
    }
}
