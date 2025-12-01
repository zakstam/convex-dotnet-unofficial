using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for error handling scenarios.
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class ErrorHandlingTests
{
    private readonly ConvexFixture _fixture;

    public ErrorHandlingTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Query_NonExistentFunction_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Query<object>("nonExistentFunction:doesNotExist")
                .ExecuteAsync();
        });

        Assert.Contains("nonExistentFunction:doesNotExist", exception.Message);
    }

    [Fact]
    public async Task Mutation_NonExistentFunction_ShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Mutate<object>("nonExistentMutation:doesNotExist")
                .WithArgs(new { someArg = "value" })
                .ExecuteAsync();
        });

        Assert.Contains("nonExistentMutation:doesNotExist", exception.Message);
    }

    [Fact]
    public async Task Query_InvalidIdFormat_ShouldThrow()
    {
        // Convex validates ID format - an invalid format should cause an error
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Query<TestItem?>("testQueries:get")
                .WithArgs(new { id = "not_a_valid_convex_id" })
                .ExecuteAsync();
        });

        // The error should mention validation
        Assert.Contains("Validator", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mutation_MissingRequiredArgs_ShouldThrow()
    {
        // Try to create without required fields
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = "test" }) // Missing name and value
                .ExecuteAsync();
        });

        // Should fail validation
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task Mutation_WrongArgType_ShouldThrow()
    {
        // Try to pass wrong type for 'value' (string instead of number)
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = "test", name = "test", value = "not_a_number" })
                .ExecuteAsync();
        });

        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task Query_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = Guid.NewGuid().ToString() })
                .ExecuteAsync(cts.Token);
        });
    }

    [Fact]
    public async Task Mutation_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = "test", name = "test", value = 1.0 })
                .ExecuteAsync(cts.Token);
        });
    }

    [Fact]
    public async Task Query_EmptyResult_ShouldReturnEmptyNotNull()
    {
        // Query with a testRunId that doesn't exist
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Query_NullResult_ShouldReturnNull()
    {
        // Create and delete an item to get a valid but non-existent ID
        var itemId = await _fixture.Client
            .Mutate<string>("testMutations:create")
            .WithArgs(new { testRunId = _fixture.TestRunId, name = "Temp", value = 0.0 })
            .ExecuteAsync();

        await _fixture.Client
            .Mutate<object?>("testMutations:deleteItem")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        var result = await _fixture.Client
            .Query<TestItem?>("testQueries:get")
            .WithArgs(new { id = itemId })
            .ExecuteAsync();

        Assert.Null(result);
    }
}
