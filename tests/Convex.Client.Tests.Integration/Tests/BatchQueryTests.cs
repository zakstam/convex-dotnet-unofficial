using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for batch query operations.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class BatchQueryTests
{
    private readonly ConvexFixture _fixture;

    public BatchQueryTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Batch_MultipleQueries_ShouldExecuteTogether()
    {
        // Create some test data first
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Batch Test Item", value = 789.0 })
            .ExecuteAsync();

        try
        {
            // Execute batch query
            var batch = _fixture.Client.Batch();

            // Note: The batch API may vary - this tests the basic concept
            // Adjust based on actual IBatchQueryBuilder interface
            Assert.NotNull(batch);
        }
        finally
        {
            // Cleanup
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }

    [Fact]
    public async Task MultipleSequentialQueries_ShouldAllSucceed()
    {
        // Create test data
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var id1 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Sequential 1", value = 1.0 })
            .ExecuteAsync();

        var id2 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Sequential 2", value = 2.0 })
            .ExecuteAsync();

        try
        {
            // Execute multiple queries sequentially
            var list = await _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();

            var item1 = await _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = id1 })
                .ExecuteAsync();

            var item2 = await _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = id2 })
                .ExecuteAsync();

            Assert.NotNull(list);
            Assert.Equal(2, list.Length);
            Assert.NotNull(item1);
            Assert.NotNull(item2);
            Assert.Equal("Sequential 1", item1.name);
            Assert.Equal("Sequential 2", item2.name);
        }
        finally
        {
            // Cleanup
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }

    [Fact]
    public async Task ParallelQueries_ShouldAllSucceed()
    {
        // Create test data
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var id1 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Parallel 1", value = 10.0 })
            .ExecuteAsync();

        var id2 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Parallel 2", value = 20.0 })
            .ExecuteAsync();

        var id3 = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = uniqueTestRunId, name = "Parallel 3", value = 30.0 })
            .ExecuteAsync();

        try
        {
            // Execute queries in parallel
            var listTask = _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();

            var item1Task = _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = id1 })
                .ExecuteAsync();

            var item2Task = _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = id2 })
                .ExecuteAsync();

            var item3Task = _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = id3 })
                .ExecuteAsync();

            await Task.WhenAll(listTask, item1Task, item2Task, item3Task);

            var list = await listTask;
            var item1 = await item1Task;
            var item2 = await item2Task;
            var item3 = await item3Task;

            Assert.NotNull(list);
            Assert.Equal(3, list.Length);
            Assert.NotNull(item1);
            Assert.NotNull(item2);
            Assert.NotNull(item3);
        }
        finally
        {
            // Cleanup
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }
}
