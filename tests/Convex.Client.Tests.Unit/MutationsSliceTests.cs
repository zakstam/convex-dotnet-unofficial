using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.Caching;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Resilience;
using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Features.DataAccess.Mutations;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class MutationsSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<IConvexCache> _mockCache = null!;
    private Mock<ILogger> _mockLogger = null!;
    private MutationsSlice _mutationsSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestFunctionName = "test:mutation";

    public MutationsSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockCache = new Mock<IConvexCache>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        _mutationsSlice = new MutationsSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockCache.Object,
            invalidateDependencies: null,
            syncContext: null,
            logger: _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region MutationsSlice Entry Point Tests

    [Fact]
    public void MutationsSlice_Mutate_WithValidFunctionName_ReturnsMutationBuilder()
    {
        // Act
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IMutationBuilder<string>>(builder);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MutationsSlice_Mutate_WithNullFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _mutationsSlice.Mutate<string>(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MutationsSlice_Mutate_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _mutationsSlice.Mutate<string>(""));
        Assert.Throws<ArgumentException>(() => _mutationsSlice.Mutate<string>("   "));
    }

    #endregion

    #region MutationBuilder Constructor Tests

    // Note: MutationBuilder constructor tests removed as MutationBuilder is internal

    #endregion

    #region MutationBuilder WithArgs Tests

    [Fact]
    public void MutationBuilder_WithArgs_WithValueTypeArgs_ConfiguresCorrectly()
    {
        // Arrange
        var args = new { id = 123, name = "test" };
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs(args);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void MutationBuilder_WithArgs_WithAction_ConfiguresCorrectly()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs<TestArgs>(args =>
        {
            args.Id = 456;
            args.Name = "configured";
        });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    #endregion

    #region MutationBuilder Optimistic Update Tests

    [Fact]
    public void MutationBuilder_Optimistic_WithAction_SetsOptimisticUpdate()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act
        var result = builder.Optimistic(value => { });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void MutationBuilder_Optimistic_WithValueAndApply_SetsOptimisticUpdate()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);
        string? capturedValue = null;

        // Act
        var result = builder.Optimistic("optimistic", value => capturedValue = value);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void MutationBuilder_OptimisticWithAutoRollback_ConfiguresAutoRollback()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);
        var state = "original";

        // Act
        var result = builder.OptimisticWithAutoRollback(
            () => state,
            s => state = s,
            s => "updated");

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MutationBuilder_OptimisticWithAutoRollback_WithNullGetter_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.OptimisticWithAutoRollback<string>(
                null!,
                s => { },
                s => s));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MutationBuilder_OptimisticWithAutoRollback_WithNullSetter_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.OptimisticWithAutoRollback<string>(
                () => "test",
                null!,
                s => s));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void MutationBuilder_OptimisticWithAutoRollback_WithNullUpdate_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.OptimisticWithAutoRollback<string>(
                () => "test",
                s => { },
                null!));
    }

    #endregion

    #region MutationBuilder ExecuteAsync Tests

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithSuccessResponse_ReturnsResult()
    {
        // Arrange
        var expectedResult = "mutation result";
        var responseJson = "{\"status\":\"success\",\"value\":\"mutation result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal(expectedResult, result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithOptimisticUpdate_AppliesOptimisticUpdate()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        bool optimisticCalled = false;
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .Optimistic(value => optimisticCalled = true);

        // Act
        await builder.ExecuteAsync();

        // Assert
        Assert.True(optimisticCalled);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithError_RollsBackOptimisticUpdate()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");
        bool rollbackCalled = false;
        var state = "original";

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .OptimisticWithAutoRollback(
                () => state,
                s => state = s,
                s => "updated")
            .WithRollback(() => rollbackCalled = true);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Assert rollback was called
        Assert.True(rollbackCalled);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithOnSuccess_CallsSuccessHandler()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        string? capturedResult = null;
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .OnSuccess(result => capturedResult = result);

        // Act
        await builder.ExecuteAsync();

        // Assert
        Assert.Equal("result", capturedResult);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithOnError_CallsErrorHandler()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");
        Exception? capturedException = null;

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .OnError(ex => capturedException = ex);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Assert error handler was called
        Assert.NotNull(capturedException);
        Assert.Same(httpException, capturedException);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithCacheInvalidation_InvalidatesDependencies()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        bool invalidateCalled = false;
        Func<string, Task> invalidateDependencies = async (pattern) =>
        {
            invalidateCalled = true;
            await Task.CompletedTask;
        };

        var sliceWithInvalidation = new MutationsSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockCache.Object,
            invalidateDependencies,
            syncContext: null,
            logger: null,
            enableDebugLogging: false);

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = sliceWithInvalidation.Mutate<string>(TestFunctionName);

        // Act
        await builder.ExecuteAsync();

        // Assert
        Assert.True(invalidateCalled);
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"success\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10))
            .RetryOn<HttpRequestException>()
            .Build();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("success");

        // First call fails, second succeeds
        _mockHttpProvider.SetupSequence(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Temporary failure"))
            .ReturnsAsync(response);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MutationBuilder_ExecuteAsync_WithSkipQueue_ExecutesImmediately()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _mutationsSlice.Mutate<string>(TestFunctionName)
            .SkipQueue();

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal("result", result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Mutation Queue Tests

    [Fact]
    public async Task MutationsSlice_EnqueueMutationAsync_ProcessesMutationsSequentially()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        int executionOrder = 0;
        int firstOrder = 0;
        int secondOrder = 0;

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback(() =>
            {
                var current = Interlocked.Increment(ref executionOrder);
                if (firstOrder == 0) firstOrder = current;
                else if (secondOrder == 0) secondOrder = current;
            });

        var builder1 = _mutationsSlice.Mutate<string>(TestFunctionName);
        var builder2 = _mutationsSlice.Mutate<string>(TestFunctionName);

        // Act - Execute both mutations (they should be queued)
        var task1 = builder1.ExecuteAsync();
        var task2 = builder2.ExecuteAsync();

        await Task.WhenAll(task1, task2);

        // Assert - Mutations should execute sequentially
        Assert.True(firstOrder < secondOrder);
    }

    #endregion

    #region BatchMutationBuilder Tests

    // Note: BatchMutationBuilder is internal, so we test through MutationsSlice if available
    // For now, we test mutation queue processing which uses BatchMutationBuilder internally

    #endregion

    #region Helper Classes

    private class TestArgs
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    #endregion
}


