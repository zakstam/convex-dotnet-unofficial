using Convex.Client.Tests.Integration.Fixtures;
using Xunit;

namespace Convex.Client.Tests.Integration.Tests;

/// <summary>
/// Integration tests for the fluent builder API (WithTimeout, OnError, Cached, WithRetry).
/// </summary>
[Collection("Convex")]
[Trait("Category", "Integration")]
public class FluentApiTests
{
    private readonly ConvexFixture _fixture;

    public FluentApiTests(ConvexFixture fixture)
    {
        _fixture = fixture;
    }

    #region Query Builder Tests

    [Fact]
    public async Task Query_WithTimeout_ShouldUseCustomTimeout()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        // Use a generous timeout - should succeed
        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .WithTimeout(TimeSpan.FromSeconds(30))
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Query_WithOnError_ShouldCallCallbackOnFailure()
    {
        var errorCallbackCalled = false;
        Exception? capturedException = null;

        try
        {
            await _fixture.Client
                .Query<object>("nonExistent:function")
                .OnError(ex =>
                {
                    errorCallbackCalled = true;
                    capturedException = ex;
                })
                .ExecuteAsync();
        }
        catch
        {
            // Expected to throw
        }

        Assert.True(errorCallbackCalled, "OnError callback was not called");
        Assert.NotNull(capturedException);
    }

    [Fact]
    public async Task Query_WithCached_NotImplemented_ShouldThrow()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        // Cached() is not yet implemented - should throw NotImplementedException
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await _fixture.Client
                .Query<TestItem[]>("testQueries:list")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .Cached(TimeSpan.FromMinutes(5))
                .ExecuteAsync();
        });
    }

    [Fact]
    public async Task Query_ChainedFluentMethods_ShouldWork()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .WithTimeout(TimeSpan.FromSeconds(30))
            .OnError(_ => { })
            .ExecuteAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Query_ExecuteWithResultAsync_Success_ShouldReturnResult()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        var result = await _fixture.Client
            .Query<TestItem[]>("testQueries:list")
            .WithArgs(new { testRunId = uniqueTestRunId })
            .ExecuteWithResultAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Query_ExecuteWithResultAsync_Failure_ShouldReturnError()
    {
        var result = await _fixture.Client
            .Query<object>("nonExistent:function")
            .ExecuteWithResultAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    #endregion

    #region Mutation Builder Tests

    [Fact]
    public async Task Mutation_WithTimeout_ShouldUseCustomTimeout()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        try
        {
            var result = await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = uniqueTestRunId, name = "Timeout Test", value = 1.0 })
                .WithTimeout(TimeSpan.FromSeconds(30))
                .ExecuteAsync();

            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
        finally
        {
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }

    [Fact]
    public async Task Mutation_WithOnError_ShouldCallCallbackOnFailure()
    {
        var errorCallbackCalled = false;
        Exception? capturedException = null;

        try
        {
            await _fixture.Client
                .Mutate<object>("nonExistent:mutation")
                .WithArgs(new { foo = "bar" })
                .OnError(ex =>
                {
                    errorCallbackCalled = true;
                    capturedException = ex;
                })
                .ExecuteAsync();
        }
        catch
        {
            // Expected to throw
        }

        Assert.True(errorCallbackCalled, "OnError callback was not called");
        Assert.NotNull(capturedException);
    }

    [Fact]
    public async Task Mutation_ExecuteWithResultAsync_Success_ShouldReturnResult()
    {
        var uniqueTestRunId = Guid.NewGuid().ToString();

        try
        {
            var result = await _fixture.Client
                .Mutate<string>("testMutations:create")
                .WithArgs(new { testRunId = uniqueTestRunId, name = "Result Test", value = 1.0 })
                .ExecuteWithResultAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
        }
        finally
        {
            await _fixture.Client
                .Mutate<double>("testMutations:cleanup")
                .WithArgs(new { testRunId = uniqueTestRunId })
                .ExecuteAsync();
        }
    }

    #endregion

    #region Action Builder Tests

    [Fact]
    public async Task Action_WithTimeout_ShouldUseCustomTimeout()
    {
        var result = await _fixture.Client
            .Action<EchoResult>("testActions:echo")
            .WithArgs(new { message = "Timeout Test" })
            .WithTimeout(TimeSpan.FromSeconds(30))
            .ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal("Timeout Test", result.echo);
    }

    [Fact]
    public async Task Action_WithOnError_ShouldCallCallbackOnFailure()
    {
        var errorCallbackCalled = false;
        Exception? capturedException = null;

        try
        {
            await _fixture.Client
                .Action<object>("testActions:throwError")
                .WithArgs(new { errorMessage = "Test error" })
                .OnError(ex =>
                {
                    errorCallbackCalled = true;
                    capturedException = ex;
                })
                .ExecuteAsync();
        }
        catch
        {
            // Expected to throw
        }

        Assert.True(errorCallbackCalled, "OnError callback was not called");
        Assert.NotNull(capturedException);
    }

    [Fact]
    public async Task Action_ExecuteWithResultAsync_Success_ShouldReturnResult()
    {
        var result = await _fixture.Client
            .Action<EchoResult>("testActions:echo")
            .WithArgs(new { message = "Result Test" })
            .ExecuteWithResultAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Result Test", result.Value.echo);
    }

    [Fact]
    public async Task Action_ExecuteWithResultAsync_Failure_ShouldReturnError()
    {
        var result = await _fixture.Client
            .Action<object>("testActions:throwError")
            .WithArgs(new { errorMessage = "Intentional error" })
            .ExecuteWithResultAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    #endregion
}
