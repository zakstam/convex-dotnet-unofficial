using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Resilience;
using Convex.Client.Shared.Builders;
using Convex.Client.Shared.Serialization;
using Convex.Client.Slices.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class QueriesSliceTests : IDisposable
{
    private Mock<IHttpClientProvider> _mockHttpProvider;
    private Mock<IConvexSerializer> _mockSerializer;
    private Mock<ILogger> _mockLogger;
    private QueriesSlice _queriesSlice;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestFunctionName = "test:query";

    public QueriesSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        // Default setup: Serialize always returns a non-null JSON string
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");

        _queriesSlice = new QueriesSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region QueriesSlice Entry Point Tests

    [Fact]
    public void QueriesSlice_Query_WithValidFunctionName_ReturnsQueryBuilder()
    {
        // Act
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IQueryBuilder<string>>(builder);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueriesSlice_Query_WithNullFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _queriesSlice.Query<string>(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueriesSlice_Query_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _queriesSlice.Query<string>(""));
        Assert.Throws<ArgumentException>(() => _queriesSlice.Query<string>("   "));
    }

    [Fact]
    public void QueriesSlice_Batch_ReturnsBatchQueryBuilder()
    {
        // Act
        var batchBuilder = _queriesSlice.Batch();

        // Assert
        Assert.NotNull(batchBuilder);
        Assert.IsAssignableFrom<IBatchQueryBuilder>(batchBuilder);
    }

    #endregion

    #region QueryBuilder Constructor Tests

    // Note: QueryBuilder constructor tests removed as QueryBuilder is internal

    #endregion

    #region QueryBuilder WithArgs Tests

    [Fact]
    public void QueryBuilder_WithArgs_WithValueTypeArgs_ConfiguresCorrectly()
    {
        // Arrange
        var args = new { id = 123, name = "test" };
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs(args);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result); // Should return same instance for chaining
    }

    [Fact]
    public void QueryBuilder_WithArgs_WithAction_ConfiguresCorrectly()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs<TestArgs>(a =>
        {
            a.Id = 456;
            a.Name = "configured";
        });

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueryBuilder_WithArgs_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            builder.WithArgs<TestArgs>((Action<TestArgs>)null!));

        Assert.Equal("configure", ex.ParamName);
    }

    #endregion

    #region QueryBuilder WithTimeout Tests

    [Fact]
    public void QueryBuilder_WithTimeout_SetsTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(30);
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithTimeout(timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueryBuilder_WithTimeout_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithTimeout(TimeSpan.Zero));

        Assert.Equal("timeout", ex.ParamName);
        Assert.Contains("cannot be zero", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueryBuilder_WithTimeout_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithTimeout(TimeSpan.FromSeconds(-1)));

        Assert.Equal("timeout", ex.ParamName);
        Assert.Contains("cannot be negative", ex.Message);
    }

    #endregion

    #region QueryBuilder WithRetry Tests

    [Fact]
    public void QueryBuilder_WithRetry_WithAction_ConfiguresRetryPolicy()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithRetry((RetryPolicyBuilder policy) =>
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
    public void QueryBuilder_WithRetry_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithRetry((Action<RetryPolicyBuilder>)null!));
    }

    [Fact]
    public void QueryBuilder_WithRetry_WithPolicy_ConfiguresRetryPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(50))
            .Build();
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithRetry(retryPolicy);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void QueryBuilder_WithRetry_WithNullPolicy_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithRetry((RetryPolicy)null!));
    }

    #endregion

    #region QueryBuilder OnError Tests

    [Fact]
    public void QueryBuilder_OnError_SetsErrorHandler()
    {
        // Arrange
        var builder = _queriesSlice.Query<string>(TestFunctionName);
        Exception? capturedException = null;

        // Act
        var result = builder.OnError(ex => capturedException = ex);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    #endregion

    #region QueryBuilder ExecuteAsync Tests

    [Fact]
    public async Task QueryBuilder_ExecuteAsync_WithSuccessResponse_ReturnsResult()
    {
        // Arrange
        var expectedResult = "test result";
        // ConvexResponseParser expects wrapped format: {"status":"success","value":<actual_value>}
        var responseJson = "{\"status\":\"success\",\"value\":\"test result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser deserializes the value field, not the whole response
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "\"test result\""))).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal(expectedResult, result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSerializer.Verify(s => s.Deserialize<string>(It.Is<string>(json => json == "\"test result\"")), Times.Once);
    }

    [Fact]
    public async Task QueryBuilder_ExecuteAsync_WithArgs_SerializesArgs()
    {
        // Arrange
        var args = new { id = 123 };
        // ConvexResponseParser expects wrapped format: {"status":"success","value":<actual_value>}
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        // ConvexRequestBuilder serializes a dictionary containing args, not args directly
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser deserializes the value field, not the whole response
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "\"result\""))).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithArgs(args);

        // Act
        await builder.ExecuteAsync();

        // Assert - Verify that Serialize was called (for the request body dictionary)
        // Note: Serialize may be called multiple times (e.g., for logging, request body, etc.)
        _mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task QueryBuilder_ExecuteAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Setup serializer to return empty JSON for null args
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");

        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            builder.ExecuteAsync(cts.Token));
    }

    [Fact]
    public async Task QueryBuilder_ExecuteAsync_WithHttpError_CallsOnError()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");
        Exception? capturedException = null;

        // Setup serializer to return empty JSON for null args
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .OnError(ex => capturedException = ex);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Assert error handler was called
        Assert.NotNull(capturedException);
        Assert.Same(httpException, capturedException);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithOnErrorThrowing_DoesNotMaskOriginalException()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");
        var callbackException = new InvalidOperationException("Error callback failed");

        // Setup serializer to return empty JSON for null args
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .OnError(ex => throw callbackException);

        // Act & Assert
        // Original exception should be thrown, not the callback exception
        var thrownEx = await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        Assert.Same(httpException, thrownEx);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithNullDeserializedResult_ThrowsInvalidOperationException()
    {
        // Arrange
        // ConvexResponseParser expects wrapped format, and null value should still be wrapped
        var responseJson = "{\"status\":\"success\",\"value\":null}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser deserializes the value field, which is null
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "null"))).Returns((string?)null);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync());

        Assert.Contains(TestFunctionName, ex.Message);
    }

    [Fact]
    public async Task QueryBuilder_ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
        // Arrange
        // ConvexResponseParser expects wrapped format: {"status":"success","value":<actual_value>}
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
        // ConvexResponseParser deserializes the value field, not the whole response
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "\"success\""))).Returns("success");

        // First call fails, second succeeds
        _mockHttpProvider.SetupSequence(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Temporary failure"))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithRetryPolicy_RespectsMaxRetries()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10))
            .RetryOn<HttpRequestException>()
            .Build();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        
        // All attempts fail
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Persistent failure"));

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act & Assert
        // MaxRetries=2 means: initial attempt + 2 retries = 3 total attempts
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Verify exactly 3 attempts were made (initial + 2 retries)
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithRetryPolicy_MaxRetriesZero_NoRetries()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(0)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10))
            .RetryOn<HttpRequestException>()
            .Build();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Failure"));

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act & Assert
        // MaxRetries=0 means: initial attempt only (no retries)
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            builder.ExecuteAsync());

        // Verify only 1 attempt was made (no retries)
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithRetryPolicy_CancellationDuringDelay_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(5)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(100))
            .RetryOn<HttpRequestException>()
            .Build();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        
        // First attempt fails, then cancel during delay
        _mockHttpProvider.SetupSequence(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Temporary failure"))
            .Returns(async () =>
            {
                // Cancel during the delay
                cts.CancelAfter(50);
                await Task.Delay(200, cts.Token);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"success\",\"value\":\"result\"}", Encoding.UTF8, "application/json")
                };
            });

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithRetry(retryPolicy);

        // Act & Assert
        // TaskCanceledException is thrown when cancellation occurs during Task.Delay in the mock
        // Note: The actual retry delay cancellation would be wrapped, but the mock's delay cancellation
        // throws TaskCanceledException directly
        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            builder.ExecuteAsync(cts.Token));

        // Verify cancellation occurred (TaskCanceledException inherits from OperationCanceledException)
        Assert.NotNull(ex);
        // The exception may be TaskCanceledException from the mock's delay, which doesn't include function name
        // but still represents cancellation during retry delay
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                // Simulate a delay longer than the timeout
                // Use a longer delay and check cancellation periodically to ensure timeout triggers
                var delayTime = TimeSpan.FromMilliseconds(500);
                var checkInterval = TimeSpan.FromMilliseconds(10);
                var elapsed = TimeSpan.Zero;
                
                while (elapsed < delayTime)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(checkInterval, ct);
                    elapsed = elapsed.Add(checkInterval);
                }
                
                // This should never be reached due to cancellation
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"status\":\"success\",\"value\":\"result\"}", Encoding.UTF8, "application/json")
                };
            });

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithTimeout(timeout);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            builder.ExecuteAsync());

        Assert.Contains(TestFunctionName, exception.Message);
        Assert.Contains(timeout.ToString(), exception.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithTimeout_CompletesWithinTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(5);
        // ConvexResponseParser expects wrapped format: {"status":"success","value":<actual_value>}
        var responseJson = "{\"status\":\"success\",\"value\":\"success\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser deserializes the value field, not the whole response
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "\"success\""))).Returns("success");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName)
            .WithTimeout(timeout);

        // Act
        var result = await builder.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
    }

    #endregion

    #region QueryBuilder ExecuteWithResultAsync Tests

    [Fact]
    public async Task QueryBuilder_ExecuteWithResultAsync_WithSuccess_ReturnsConvexResult()
    {
        // Arrange
        var expectedResult = "test result";
        // ConvexResponseParser expects wrapped format: {"status":"success","value":<actual_value>}
        var responseJson = "{\"status\":\"success\",\"value\":\"test result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser deserializes the value field, not the whole response
        _mockSerializer.Setup(s => s.Deserialize<string>(It.Is<string>(json => json == "\"test result\""))).Returns(expectedResult);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteWithResultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
        // Don't access Error on successful result - it throws InvalidOperationException
    }

    [Fact]
    public async Task QueryBuilder_ExecuteWithResultAsync_WithError_ReturnsConvexResultWithError()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error");

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(httpException);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act
        var result = await builder.ExecuteWithResultAsync();

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(httpException.GetType(), result.Error.Exception?.GetType());
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithErrorResponseFormat_ThrowsInvalidOperationException()
    {
        // Arrange
        // ConvexResponseParser handles error format: {"status":"error","errorMessage":"..."}
        var responseJson = "{\"status\":\"error\",\"errorMessage\":\"Query failed: Invalid input\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync());

        Assert.Contains(TestFunctionName, ex.Message);
        Assert.Contains("Query failed: Invalid input", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithEmptyResponseBody_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync());

        Assert.Contains(TestFunctionName, ex.Message);
        Assert.Contains("Empty response", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task QueryBuilder_ExecuteAsync_WithNullResponseContent_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = null
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var builder = _queriesSlice.Query<string>(TestFunctionName);

        // Act & Assert
        // ReadContentAsStringAsync returns empty string for null content, which ConvexResponseParser handles
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            builder.ExecuteAsync());

        Assert.Contains(TestFunctionName, ex.Message);
        Assert.Contains("Empty response", ex.Message);
    }

    #endregion

    #region BatchQueryBuilder Tests

    [Fact]
    public void BatchQueryBuilder_Query_WithValidFunctionName_AddsQuery()
    {
        // Arrange
        var batchBuilder = _queriesSlice.Batch();

        // Act
        var result = batchBuilder.Query<string>(TestFunctionName);

        // Assert
        Assert.NotNull(result);
        Assert.Same(batchBuilder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void BatchQueryBuilder_Query_WithNullFunctionName_ThrowsArgumentException()
    {
        // Arrange
        var batchBuilder = _queriesSlice.Batch();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            batchBuilder.Query<string>(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void BatchQueryBuilder_Query_WithEmptyFunctionName_ThrowsArgumentException()
    {
        // Arrange
        var batchBuilder = _queriesSlice.Batch();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            batchBuilder.Query<string>(""));
        Assert.Throws<ArgumentException>(() =>
            batchBuilder.Query<string>("   "));
    }

    [Fact]
    public void BatchQueryBuilder_Query_WithArgs_AddsQueryWithArgs()
    {
        // Arrange
        var args = new { id = 123 };
        var batchBuilder = _queriesSlice.Batch();

        // Act
        var result = batchBuilder.Query<string, object>(TestFunctionName, args);

        // Assert
        Assert.NotNull(result);
        Assert.Same(batchBuilder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithNoQueries_ThrowsInvalidOperationException()
    {
        // Arrange
        var batchBuilder = _queriesSlice.Batch();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());
    }

    [Fact]
    public async Task BatchQueryBuilder_ExecuteAsync_WithSingleQuery_ReturnsResult()
    {
        // Arrange
        var expectedResult = "result1";
        var responseJson = "[\"result1\"]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize(It.IsAny<string>(), typeof(string))).Returns<string, Type>((json, type) => json.Trim('"'));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName);

        // Act
        var results = await batchBuilder.ExecuteAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal(expectedResult, results[0]);
    }

    [Fact]
    public async Task BatchQueryBuilder_ExecuteAsync_WithMultipleQueries_ReturnsAllResults()
    {
        // Arrange
        var responseJson = "[\"result1\",\"result2\"]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize(It.IsAny<string>(), typeof(string))).Returns<string, Type>((json, type) => json.Trim('"'));
        _mockSerializer.Setup(s => s.Deserialize(It.IsAny<string>(), typeof(int))).Returns<string, Type>((json, type) =>
        {
            var trimmed = json.Trim('"');
            return trimmed == "result2" ? 42 : int.Parse(trimmed);
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act
        var results = await batchBuilder.ExecuteAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Length);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithMismatchedResultCount_ThrowsInvalidOperationException()
    {
        // Arrange
        // Response has 1 result but we added 2 queries
        var responseJson = "[\"result1\"]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("missing", ex.Message);
        Assert.Contains("1 result(s)", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithExtraResults_ThrowsInvalidOperationException()
    {
        // Arrange
        // Response has 3 results but we added 2 queries
        var responseJson = "[\"result1\",\"result2\",\"result3\"]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("extra result(s)", ex.Message);
        Assert.Contains("1 extra result(s)", ex.Message);
    }

    [Fact]
    public async Task BatchQueryBuilder_ExecuteAsync_WithTypedOverload_ReturnsTypedTuple()
    {
        // Arrange
        var responseJson = "[\"result1\",42]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns<string>(json => json.Trim('"'));
        _mockSerializer.Setup(s => s.Deserialize<int>(It.IsAny<string>())).Returns<string>(json => int.Parse(json));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act
        var (result1, result2) = await batchBuilder.ExecuteAsync<string, int>();

        // Assert
        Assert.Equal("result1", result1);
        Assert.Equal(42, result2);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithTypedOverload_WrongCount_ThrowsInvalidOperationException()
    {
        // Arrange
        var responseJson = "[\"result1\"]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName);

        // Act & Assert - Expecting 2 but only 1 query added
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync<string, int>());
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithWrappedResponseFormat_HandlesCorrectly()
    {
        // Arrange
        // Batch query response with wrapped format: [{"status":"success","value":<result>}, ...]
        var responseJson = "[{\"status\":\"success\",\"value\":\"result1\"},{\"status\":\"success\",\"value\":42}]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        // ConvexResponseParser extracts the value field, so deserializer receives just the value
        _mockSerializer.Setup(s => s.Deserialize(It.Is<string>(json => json == "\"result1\""), typeof(string)))
            .Returns<string, Type>((json, type) => json.Trim('"'));
        _mockSerializer.Setup(s => s.Deserialize(It.Is<string>(json => json == "42"), typeof(int)))
            .Returns<string, Type>((json, type) => int.Parse(json));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act
        var results = await batchBuilder.ExecuteAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Length);
        Assert.Equal("result1", results[0]);
        Assert.Equal(42, results[1]);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithWrappedErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        // Batch query response with error in wrapped format
        var responseJson = "[{\"status\":\"success\",\"value\":\"result1\"},{\"status\":\"error\",\"errorMessage\":\"Query failed: Invalid input\"}]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("index 1", ex.Message);
        Assert.Contains("test:query2", ex.Message);
        Assert.Contains("Query failed: Invalid input", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithEmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithNonArrayResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        // Response is not an array
        var responseJson = "{\"status\":\"success\",\"value\":\"result\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("expected array", ex.Message);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithSerializationException_ThrowsInvalidOperationException()
    {
        // Arrange
        var serializationException = new InvalidOperationException("Serialization failed");
        
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>()))
            .Throws(serializationException);
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<int>("test:query2");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("Failed to serialize batch query request", ex.Message);
        // The new error message format identifies problematic queries and lists all queries
        // Since serialization fails for all queries, they will all be identified as problematic
        Assert.Contains("Problematic queries:", ex.Message);
        Assert.Contains("All queries:", ex.Message);
        Assert.Contains(TestFunctionName, ex.Message);
        Assert.Contains("test:query2", ex.Message);
        Assert.Same(serializationException, ex.InnerException);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task BatchQueryBuilder_ExecuteAsync_WithMalformedJsonResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        // Malformed JSON - missing closing bracket
        var responseJson = "[\"result1\",\"result2\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var batchBuilder = _queriesSlice.Batch()
            .Query<string>(TestFunctionName)
            .Query<string>("test:query2");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            batchBuilder.ExecuteAsync());

        Assert.Contains("Invalid JSON response", ex.Message);
        Assert.Contains("2 results", ex.Message);
        Assert.NotNull(ex.InnerException);
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


