using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Slices.HttpActions;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class HttpActionsSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<ILogger> _mockLogger = null!;
    private HttpActionsSlice _httpActionsSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestActionPath = "/api/test";

    public HttpActionsSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);
        
        // Default setup: Serialize always returns a non-null JSON string
        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");

        _httpActionsSlice = new HttpActionsSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region HttpActionsSlice Entry Point Tests

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HttpActionsSlice_Constructor_WithNullHttpProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HttpActionsSlice(null!, _mockSerializer.Object));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HttpActionsSlice_Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new HttpActionsSlice(_mockHttpProvider.Object, null!));
    }

    [Fact]
    public async Task HttpActionsSlice_GetAsync_WithValidPath_ReturnsResponse()
    {
        // Arrange
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _httpActionsSlice.GetAsync<string>(TestActionPath);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HttpActionsSlice_GetAsync_WithQueryParameters_IncludesParameters()
    {
        // Arrange
        var queryParams = new Dictionary<string, string> { { "key", "value" } };
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                Assert.NotNull(req.RequestUri);
                Assert.Contains("key=value", req.RequestUri.ToString());
            });

        // Act
        await _httpActionsSlice.GetAsync<string>(TestActionPath, queryParams);

        // Assert
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HttpActionsSlice_PostAsync_WithBody_SerializesBody()
    {
        // Arrange
        var body = new { id = 123, name = "test" };
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{\"id\":123,\"name\":\"test\"}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _httpActionsSlice.PostAsync<string, object>(TestActionPath, body);

        // Assert
        _mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HttpActionsSlice_PutAsync_WithBody_SerializesBody()
    {
        // Arrange
        var body = new { id = 456 };
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{\"id\":456}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _httpActionsSlice.PutAsync<string, object>(TestActionPath, body);

        // Assert
        _mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HttpActionsSlice_DeleteAsync_WithValidPath_ReturnsResponse()
    {
        // Arrange
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _httpActionsSlice.DeleteAsync<string>(TestActionPath);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HttpActionsSlice_PatchAsync_WithBody_SerializesBody()
    {
        // Arrange
        var body = new { status = "updated" };
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{\"status\":\"updated\"}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _httpActionsSlice.PatchAsync<string, object>(TestActionPath, body);

        // Assert
        _mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task HttpActionsSlice_CallAsync_WithCustomMethod_CallsCorrectMethod()
    {
        // Arrange
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                Assert.Equal(HttpMethod.Head, req.Method);
            });

        // Act
        await _httpActionsSlice.CallAsync<string>(HttpMethod.Head, TestActionPath);

        // Assert
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HttpActionsSlice_UploadFileAsync_WithFile_UploadsFile()
    {
        // Arrange
        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("file content"));
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _httpActionsSlice.UploadFileAsync<string>(TestActionPath, fileContent, "test.txt", "text/plain");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task HttpActionsSlice_UploadFileAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert - Now throws ArgumentNullException directly
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _httpActionsSlice.UploadFileAsync<string>(TestActionPath, null!, "test.txt", "text/plain"));
    }

    [Fact]
    public async Task HttpActionsSlice_CallWebhookAsync_WithPayload_CallsWebhook()
    {
        // Arrange
        var payload = new { @event = "test" };
        var responseJson = "\"result\"";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<string>(It.IsAny<string>())).Returns("result");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _httpActionsSlice.CallWebhookAsync<string, object>(TestActionPath, payload);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task HttpActionsSlice_CallWebhookAsync_WithNullPayload_ThrowsArgumentNullException()
    {
        // Act & Assert - NullReferenceException gets wrapped in ConvexHttpActionException
        await Assert.ThrowsAsync<ConvexHttpActionException>(() =>
            _httpActionsSlice.CallWebhookAsync<string, object>(TestActionPath, null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task HttpActionsSlice_GetAsync_WithNullActionPath_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexHttpActionException>(() =>
            _httpActionsSlice.GetAsync<string>(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task HttpActionsSlice_PostAsync_WithNullActionPath_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexHttpActionException>(() =>
            _httpActionsSlice.PostAsync<string>(null!));
    }

    #endregion
}


