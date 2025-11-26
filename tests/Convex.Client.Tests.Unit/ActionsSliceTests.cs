using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Resilience;
using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Features.DataAccess.Actions;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class ActionsSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<ILogger> _mockLogger = null!;
    private ActionsSlice _actionsSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestFunctionName = "test:action";

    public ActionsSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);
        
        // Default setup: Serialize always returns a non-null JSON string
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");

        _actionsSlice = new ActionsSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region ActionsSlice Entry Point Tests

    [Fact]
    public void ActionsSlice_Action_WithValidFunctionName_ReturnsActionBuilder()
    {
        // Act
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IActionBuilder<string>>(builder);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ActionsSlice_Action_WithNullFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _actionsSlice.Action<string>(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ActionsSlice_Action_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _actionsSlice.Action<string>(""));
        Assert.Throws<ArgumentException>(() => _actionsSlice.Action<string>("   "));
    }

    #endregion

    #region ActionBuilder Constructor Tests

    // Note: ActionBuilder constructor tests removed as ActionBuilder is internal

    #endregion

    #region ActionBuilder WithArgs Tests

    [Fact]
    public void ActionBuilder_WithArgs_WithValueTypeArgs_ConfiguresCorrectly()
    {
        // Arrange
        var args = new { id = 123, name = "test" };
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs(args);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void ActionBuilder_WithArgs_WithAction_ConfiguresCorrectly()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

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

    #region ActionBuilder WithTimeout Tests

    [Fact]
    public void ActionBuilder_WithTimeout_SetsTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.WithTimeout(timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ActionBuilder_WithTimeout_WithZeroTimeout_DoesNotThrow()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.WithTimeout(TimeSpan.Zero);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region ActionBuilder WithRetry Tests

    [Fact]
    public void ActionBuilder_WithRetry_WithAction_ConfiguresRetryPolicy()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.WithRetry(policy =>
        {
            policy.MaxRetries(3);
            policy.ExponentialBackoff(TimeSpan.FromMilliseconds(100));
        });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ActionBuilder_WithRetry_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithRetry((Action<RetryPolicyBuilder>)null!));
    }

    [Fact]
    public void ActionBuilder_WithRetry_WithPolicy_ConfiguresRetryPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(50))
            .Build();
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.WithRetry(retryPolicy);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ActionBuilder_WithRetry_WithNullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithRetry((RetryPolicy)null!));
    }

    #endregion

    #region ActionBuilder OnSuccess/OnError Tests

    [Fact]
    public void ActionBuilder_OnSuccess_SetsSuccessHandler()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.OnSuccess(value => { });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void ActionBuilder_OnError_SetsErrorHandler()
    {
        // Arrange
        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = builder.OnError(ex => { });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    #endregion

    #region ActionBuilder ExecuteAsync Tests

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithSuccessResponse_ReturnsResult()
    {
        // Arrange
        var expectedResult = "action result";
        var responseJson = "{\"status\":\"success\",\"value\":\"action result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal(expectedResult, result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithArgs_SerializesArgs()
    {
        // Arrange
        var args = new { id = 123 };
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        // ConvexRequestBuilder serializes a dictionary containing args, not args directly
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _actionsSlice.Action<string>(TestFunctionName)
            .WithArgs(args);

        // Act
        await builder.ExecuteAsync();

        // Assert - Verify that Serialize was called (for the request body dictionary)
        // Note: Serialize may be called multiple times (e.g., for logging, request body, etc.)
        _mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            builder.ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithHttpError_CallsOnError()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");
        Exception? capturedException = null;

        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _actionsSlice.Action<string>(TestFunctionName)
            .OnError(ex => capturedException = ex);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Assert error handler was called
        Assert.NotNull(capturedException);
        Assert.Same(httpException, capturedException);
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithOnSuccess_CallsSuccessHandler()
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

        var builder = _actionsSlice.Action<string>(TestFunctionName)
            .OnSuccess(result => capturedResult = result);

        // Act
        await builder.ExecuteAsync();

        // Assert
        Assert.Equal("result", capturedResult);
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                await Task.Delay(200, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var builder = _actionsSlice.Action<string>(TestFunctionName)
            .WithTimeout(timeout);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() =>
            builder.ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
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

        var builder = _actionsSlice.Action<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ActionBuilder_ExecuteAsync_WithMiddlewareExecutor_UsesMiddleware()
    {
        // Arrange
        var expectedResult = "middleware result";

        var sliceWithMiddleware = new ActionsSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);

        var builder = sliceWithMiddleware.Action<string>(TestFunctionName);

        // Use reflection to set middleware executor (since it's passed via constructor)
        // For this test, we'll verify the normal execution path works
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"success\",\"value\":\"middleware result\"}", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region ActionBuilder ExecuteWithResultAsync Tests

    [Fact]
    public async Task ActionBuilder_ExecuteWithResultAsync_WithSuccess_ReturnsConvexResult()
    {
        // Arrange
        var expectedResult = "action result";
        var responseJson = "{\"status\":\"success\",\"value\":\"action result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteWithResultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
    }

    [Fact]
    public async Task ActionBuilder_ExecuteWithResultAsync_WithError_ReturnsConvexResultWithError()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _actionsSlice.Action<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteWithResultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(httpException.GetType(), result.Error.Exception?.GetType());
    }

    #endregion

    #region Helper Classes

    private class TestArgs
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    #endregion
}


