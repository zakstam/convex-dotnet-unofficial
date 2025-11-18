using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Slices.VectorSearch;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class VectorSearchSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<ILogger> _mockLogger = null!;
    private VectorSearchSlice _vectorSearchSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestIndexName = "test:index";
    private static readonly float[] TestVector = new float[] { 0.1f, 0.2f, 0.3f };
    private static readonly float[] TestEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
    private static readonly float[][] TestEmbeddings = new float[][] { new float[] { 0.1f, 0.2f }, new float[] { 0.3f, 0.4f } };
    private static readonly string[] TestTexts = new string[] { "text1", "text2" };

    public VectorSearchSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        _vectorSearchSlice = new VectorSearchSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region VectorSearchSlice Entry Point Tests

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void VectorSearchSlice_Constructor_WithNullHttpProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VectorSearchSlice(null!, _mockSerializer.Object));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void VectorSearchSlice_Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VectorSearchSlice(_mockHttpProvider.Object, null!));
    }

    [Fact]
    public async Task VectorSearchSlice_SearchAsync_WithValidVector_ReturnsResults()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"results\":[{\"_id\":\"id1\",\"_score\":0.95,\"data\":{}}]}}";
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
        var results = await _vectorSearchSlice.SearchAsync<object>(TestIndexName, TestVector);

        // Assert
        Assert.NotNull(results);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task VectorSearchSlice_SearchAsync_WithNullVector_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexVectorSearchException>(() =>
            _vectorSearchSlice.SearchAsync<object>(TestIndexName, null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task VectorSearchSlice_SearchAsync_WithEmptyVector_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexVectorSearchException>(() =>
            _vectorSearchSlice.SearchAsync<object>(TestIndexName, Array.Empty<float>()));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task VectorSearchSlice_SearchAsync_WithNegativeLimit_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexVectorSearchException>(() =>
            _vectorSearchSlice.SearchAsync<object>(TestIndexName, TestVector, limit: -1));
    }

    [Fact]
    public async Task VectorSearchSlice_SearchAsync_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        var filter = new { category = "test" };
        var responseJson = "{\"status\":\"success\",\"value\":{\"results\":[]}}";
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
        var results = await _vectorSearchSlice.SearchAsync<object, object>(TestIndexName, TestVector, 10, filter);

        // Assert
        Assert.NotNull(results);
    }

    [Fact]
    public async Task VectorSearchSlice_SearchByTextAsync_WithValidText_ReturnsResults()
    {
        // Arrange
        var embeddingResponseJson = "{\"status\":\"success\",\"value\":{\"embedding\":[0.1,0.2,0.3]}}";
        var searchResponseJson = "{\"status\":\"success\",\"value\":{\"results\":[]}}";
        var embeddingResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(embeddingResponseJson, Encoding.UTF8, "application/json")
        };
        var searchResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(searchResponseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.SetupSequence(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResponse)  // First call: create embedding
            .ReturnsAsync(searchResponse);    // Second call: search

        // Act
        var results = await _vectorSearchSlice.SearchByTextAsync<object>(TestIndexName, "test query");

        // Assert
        Assert.NotNull(results);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task VectorSearchSlice_SearchByTextAsync_WithNullText_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexVectorSearchException>(() =>
            _vectorSearchSlice.SearchByTextAsync<object>(TestIndexName, null!));
    }

    [Fact]
    public async Task VectorSearchSlice_CreateEmbeddingAsync_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"embedding\":[0.1,0.2,0.3]}}";
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
        var embedding = await _vectorSearchSlice.CreateEmbeddingAsync("test text");

        // Assert
        Assert.NotNull(embedding);
        Assert.True(embedding.Length > 0);
    }

    [Fact]
    public async Task VectorSearchSlice_CreateEmbeddingsAsync_WithValidTexts_ReturnsEmbeddings()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"embeddings\":[[0.1,0.2],[0.3,0.4]]}}";
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
        var embeddings = await _vectorSearchSlice.CreateEmbeddingsAsync(TestTexts);

        // Assert
        Assert.NotNull(embeddings);
        Assert.Equal(2, embeddings.Length);
    }

    [Fact]
    public async Task VectorSearchSlice_GetIndexInfoAsync_WithValidIndex_ReturnsInfo()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"name\":\"" + TestIndexName + "\",\"dimension\":128,\"metric\":\"cosine\",\"table\":\"test_table\",\"vectorField\":\"vector\"}}";
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
        var info = await _vectorSearchSlice.GetIndexInfoAsync(TestIndexName);

        // Assert
        Assert.NotNull(info);
    }

    [Fact]
    public async Task VectorSearchSlice_ListIndicesAsync_ReturnsIndices()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"indices\":[]}}";
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
        var indices = await _vectorSearchSlice.ListIndicesAsync();

        // Assert
        Assert.NotNull(indices);
    }

    #endregion
}


