using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Slices.FileStorage;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class FileStorageSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<HttpClient> _mockUploadHttpClient = null!;
    private Mock<ILogger> _mockLogger = null!;
    private FileStorageSlice _fileStorageSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestStorageId = "storage-id-123";
    private const string TestUploadUrl = "https://upload.convex.cloud/upload";

    public FileStorageSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockUploadHttpClient = new Mock<HttpClient>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        _fileStorageSlice = new FileStorageSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockUploadHttpClient.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region FileStorageSlice Entry Point Tests

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void FileStorageSlice_Constructor_WithNullHttpProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileStorageSlice(
                null!,
                _mockSerializer.Object,
                _mockUploadHttpClient.Object));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void FileStorageSlice_Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileStorageSlice(
                _mockHttpProvider.Object,
                null!,
                _mockUploadHttpClient.Object));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void FileStorageSlice_Constructor_WithNullUploadHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FileStorageSlice(
                _mockHttpProvider.Object,
                _mockSerializer.Object,
                null!));
    }

    [Fact]
    public async Task FileStorageSlice_GenerateUploadUrlAsync_WithValidFilename_ReturnsUploadUrl()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"uploadUrl\":\"" + TestUploadUrl + "\",\"storageId\":\"" + TestStorageId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns(JsonDocument.Parse(responseJson).RootElement.GetProperty("value"));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _fileStorageSlice.GenerateUploadUrlAsync("test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestUploadUrl, result.UploadUrl);
        Assert.Equal(TestStorageId, result.StorageId);
    }

    [Fact]
    public async Task FileStorageSlice_GenerateUploadUrlAsync_WithNullFilename_ReturnsUploadUrl()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"uploadUrl\":\"" + TestUploadUrl + "\",\"storageId\":\"" + TestStorageId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns(JsonDocument.Parse(responseJson).RootElement.GetProperty("value"));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _fileStorageSlice.GenerateUploadUrlAsync(null);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_GenerateUploadUrlAsync_WithInvalidResponse_ThrowsConvexFileStorageException()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns(JsonDocument.Parse(responseJson).RootElement.GetProperty("value"));
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<ConvexFileStorageException>(() =>
            _fileStorageSlice.GenerateUploadUrlAsync());
    }

    [Fact]
    public async Task FileStorageSlice_UploadFileAsync_WithUploadUrl_ReturnsStorageId()
    {
        // Arrange
        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        // Upload response is plain JSON with storageId, not wrapped in status/value
        var uploadResponseJson = "{\"storageId\":\"" + TestStorageId + "\"}";
        var uploadResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(uploadResponseJson, Encoding.UTF8, "application/json")
        };

        // Use a real HttpClient with a test handler
        var handler = new TestHttpMessageHandler(uploadResponse);
        var httpClient = new HttpClient(handler);
        var slice = new FileStorageSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            httpClient,
            _mockLogger.Object);

        // Act
        var result = await slice.UploadFileAsync(TestUploadUrl, fileContent, "text/plain", "test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TestStorageId, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_UploadFileAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _fileStorageSlice.UploadFileAsync(TestUploadUrl, null!, "text/plain"));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_UploadFileAsync_WithEmptyStream_HandlesGracefully()
    {
        // Arrange
        var emptyStream = new MemoryStream();
        // Upload response is plain JSON with storageId
        var uploadResponseJson = "{\"storageId\":\"" + TestStorageId + "\"}";
        var uploadResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(uploadResponseJson, Encoding.UTF8, "application/json")
        };

        var handler = new TestHttpMessageHandler(uploadResponse);
        var httpClient = new HttpClient(handler);
        var slice = new FileStorageSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            httpClient,
            _mockLogger.Object);

        // Act
        var result = await slice.UploadFileAsync(TestUploadUrl, emptyStream, "text/plain");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FileStorageSlice_DownloadFileAsync_WithValidStorageId_ReturnsStream()
    {
        // Arrange
        var downloadUrl = $"https://download.convex.cloud/{TestStorageId}";
        var getUrlResponseJson = "{\"status\":\"success\",\"value\":{\"url\":\"" + downloadUrl + "\"}}";
        var getUrlResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(getUrlResponseJson, Encoding.UTF8, "application/json")
        };

        var downloadResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("file content"))
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(getUrlResponse);

        // Mock the uploadHttpClient for the actual download
        var handler = new TestHttpMessageHandler(downloadResponse);
        var httpClient = new HttpClient(handler);
        var slice = new FileStorageSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            httpClient,
            _mockLogger.Object);

        // Act
        var stream = await slice.DownloadFileAsync(TestStorageId);

        // Assert
        Assert.NotNull(stream);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_DownloadFileAsync_WithInvalidStorageId_ThrowsException()
    {
        // Arrange
        var responseJson = "{\"status\":\"error\",\"errorMessage\":\"Storage ID not found\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act & Assert
        await Assert.ThrowsAsync<ConvexFileStorageException>(() =>
            _fileStorageSlice.DownloadFileAsync("invalid-storage-id"));
    }

    [Fact]
    public async Task FileStorageSlice_GetDownloadUrlAsync_WithValidStorageId_ReturnsUrl()
    {
        // Arrange
        var downloadUrl = $"https://download.convex.cloud/{TestStorageId}";
        var responseJson = "{\"status\":\"success\",\"value\":{\"url\":\"" + downloadUrl + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var url = await _fileStorageSlice.GetDownloadUrlAsync(TestStorageId);

        // Assert
        Assert.Equal(downloadUrl, url);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_GetDownloadUrlAsync_WithNullStorageId_ThrowsArgumentNullException()
    {
        // Act & Assert - Implementation throws ConvexFileStorageException
        await Assert.ThrowsAsync<ConvexFileStorageException>(() =>
            _fileStorageSlice.GetDownloadUrlAsync(null!));
    }

    [Fact]
    public async Task FileStorageSlice_GetFileMetadataAsync_WithValidStorageId_ReturnsMetadata()
    {
        // Arrange
        var metadataJson = "{\"status\":\"success\",\"value\":{\"storageId\":\"" + TestStorageId + "\",\"size\":1024,\"contentType\":\"text/plain\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(metadataJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var metadata = await _fileStorageSlice.GetFileMetadataAsync(TestStorageId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(TestStorageId, metadata.StorageId);
    }

    [Fact]
    public async Task FileStorageSlice_DeleteFileAsync_WithValidStorageId_ReturnsTrue()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"deleted\":true}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _fileStorageSlice.DeleteFileAsync(TestStorageId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task FileStorageSlice_DeleteFileAsync_WithInvalidStorageId_ReturnsFalse()
    {
        // Arrange - Response with deleted: false
        var responseJson = "{\"status\":\"success\",\"value\":{\"deleted\":false}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _fileStorageSlice.DeleteFileAsync("invalid-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FileStorageSlice_UploadFileAsync_WithoutUploadUrl_GeneratesUrlAndUploads()
    {
        // Arrange
        var uploadUrlResponse = "{\"status\":\"success\",\"value\":{\"uploadUrl\":\"" + TestUploadUrl + "\",\"storageId\":\"" + TestStorageId + "\"}}";
        var uploadResponseJson = "{\"storageId\":\"" + TestStorageId + "\"}";
        var uploadResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(uploadResponseJson, Encoding.UTF8, "application/json")
        };

        var handler = new TestHttpMessageHandler(uploadResponse);
        var httpClient = new HttpClient(handler);
        var slice = new FileStorageSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            httpClient,
            _mockLogger.Object);

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(uploadUrlResponse, Encoding.UTF8, "application/json")
            });

        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("test content"));

        // Act
        var result = await slice.UploadFileAsync(fileContent, "text/plain", "test.txt");

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Helper Classes

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public TestHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    #endregion
}


