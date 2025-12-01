using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ConsistentQueries;
using Convex.Client.Infrastructure.ErrorHandling;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class TimestampManagerTests
{
    private const string TestDeploymentUrl = "https://test.convex.cloud";

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new TimestampManager(null!, TestDeploymentUrl));

    [Fact]
    public void Constructor_WithNullDeploymentUrl_ThrowsArgumentNullException()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => new TimestampManager(httpClient, null!));
    }

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        // Arrange
        using var httpClient = new HttpClient();

        // Act
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Assert
        Assert.NotNull(manager);
        Assert.Null(manager.CachedTimestamp);
        Assert.False(manager.HasValidTimestamp);
    }

    [Fact]
    public void Constructor_TrimsTrailingSlash()
    {
        // Arrange
        using var httpClient = new HttpClient();
        const string urlWithSlash = "https://test.convex.cloud/";

        // Act
        var manager = new TimestampManager(httpClient, urlWithSlash);

        // Assert - No exception should be thrown
        Assert.NotNull(manager);
    }

    #endregion Constructor Tests

    #region CachedTimestamp Property Tests

    [Fact]
    public void CachedTimestamp_Initially_IsNull()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Assert
        Assert.Null(manager.CachedTimestamp);
    }

    #endregion CachedTimestamp Property Tests

    #region HasValidTimestamp Property Tests

    [Fact]
    public void HasValidTimestamp_Initially_IsFalse()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Assert
        Assert.False(manager.HasValidTimestamp);
    }

    #endregion HasValidTimestamp Property Tests

    #region ClearTimestampAsync Tests

    [Fact]
    public async Task ClearTimestampAsync_ClearsTimestamp()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act
        await manager.ClearTimestampAsync();

        // Assert
        Assert.Null(manager.CachedTimestamp);
        Assert.False(manager.HasValidTimestamp);
    }

    [Fact]
    public async Task ClearTimestampAsync_RespectsCancellationToken()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            manager.ClearTimestampAsync(cts.Token));
    }

    #endregion ClearTimestampAsync Tests

    #region GetTimestampAsync Tests

    [Fact]
    public async Task GetTimestampAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            manager.GetTimestampAsync(cts.Token));
    }

    [Fact]
    public async Task GetTimestampAsync_WithMockedSuccessResponse_ReturnsTimestamp()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ts\":\"12345.67890\"}")
            });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act
        var timestamp = await manager.GetTimestampAsync();

        // Assert
        Assert.Equal("12345.67890", timestamp);
        Assert.Equal("12345.67890", manager.CachedTimestamp);
        Assert.True(manager.HasValidTimestamp);
    }

    [Fact]
    public async Task GetTimestampAsync_WithCachedTimestamp_ReturnsCachedValue()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = new MockHttpMessageHandler(() =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"ts\":\"timestamp_{requestCount}\"}}")
            };
        });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act - First call
        var timestamp1 = await manager.GetTimestampAsync();

        // Act - Second call (should use cached value)
        var timestamp2 = await manager.GetTimestampAsync();

        // Assert
        Assert.Equal("timestamp_1", timestamp1);
        Assert.Equal("timestamp_1", timestamp2);
        Assert.Equal(1, requestCount); // Should only make one request
    }

    [Fact]
    public async Task GetTimestampAsync_WithErrorResponse_ThrowsConvexException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Internal Server Error")
            });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ConvexException>(() => manager.GetTimestampAsync());
    }

    [Fact]
    public async Task GetTimestampAsync_WithEmptyTimestamp_ThrowsConvexException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ts\":\"\"}")
            });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ConvexException>(() => manager.GetTimestampAsync());
    }

    [Fact]
    public async Task GetTimestampAsync_WithNullResponse_ThrowsConvexException()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act & Assert
        _ = await Assert.ThrowsAsync<ConvexException>(() => manager.GetTimestampAsync());
    }

    [Fact]
    public async Task GetTimestampAsync_AfterClear_FetchesNewTimestamp()
    {
        // Arrange
        var requestCount = 0;
        var mockHandler = new MockHttpMessageHandler(() =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"ts\":\"timestamp_{requestCount}\"}}")
            };
        });

        using var httpClient = new HttpClient(mockHandler);
        var manager = new TimestampManager(httpClient, TestDeploymentUrl);

        // Act
        var timestamp1 = await manager.GetTimestampAsync();
        await manager.ClearTimestampAsync();
        var timestamp2 = await manager.GetTimestampAsync();

        // Assert
        Assert.Equal("timestamp_1", timestamp1);
        Assert.Equal("timestamp_2", timestamp2);
        Assert.Equal(2, requestCount);
    }

    #endregion GetTimestampAsync Tests

    #region ConsistentQueryOptions Tests

    [Fact]
    public void ConsistentQueryOptions_DefaultValues()
    {
        // Arrange & Act
        var options = new ConsistentQueryOptions();

        // Assert
        Assert.False(options.ForceNewTimestamp);
        Assert.Null(options.TimestampValidity);
    }

    [Fact]
    public void ConsistentQueryOptions_CanSetProperties()
    {
        // Arrange & Act
        var options = new ConsistentQueryOptions
        {
            ForceNewTimestamp = true,
            TimestampValidity = TimeSpan.FromSeconds(60)
        };

        // Assert
        Assert.True(options.ForceNewTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(60), options.TimestampValidity);
    }

    #endregion ConsistentQueryOptions Tests

    #region Helper Classes

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory;

        public MockHttpMessageHandler(HttpResponseMessage response)
            : this(() => response) { }

        public MockHttpMessageHandler(Func<HttpResponseMessage> responseFactory) =>
            _responseFactory = responseFactory;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responseFactory());
    }

    #endregion Helper Classes
}
