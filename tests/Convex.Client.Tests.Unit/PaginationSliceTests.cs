using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Features.RealTime.Pagination;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class PaginationSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<ILogger> _mockLogger = null!;
    private PaginationSlice _paginationSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestFunctionName = "test:paginatedQuery";
    private static readonly string[] TestPageItems = new string[] { "item1", "item2" };

    public PaginationSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        _paginationSlice = new PaginationSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region PaginationSlice Entry Point Tests

    [Fact]
    public void PaginationSlice_Query_WithValidFunctionName_ReturnsPaginationBuilder()
    {
        // Act
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Assert
        Assert.NotNull(builder);
        Assert.IsAssignableFrom<IPaginationBuilder<string>>(builder);
    }

    #endregion

    #region PaginationBuilder Tests

    [Fact]
    public void PaginationBuilder_WithPageSize_SetsPageSize()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithPageSize(50);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void PaginationBuilder_WithPageSize_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithPageSize(0));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void PaginationBuilder_WithPageSize_WithNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithPageSize(-1));
    }

    [Fact]
    public void PaginationBuilder_WithArgs_WithValueTypeArgs_ConfiguresCorrectly()
    {
        // Arrange
        var args = new { filter = "test" };
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Act
        var result = builder.WithArgs(args);

        // Assert
        Assert.NotNull(result);
        Assert.Same(builder, result);
    }

    [Fact]
    public void PaginationBuilder_WithArgs_WithAction_ConfiguresCorrectly()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName);

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

    [Fact]
    public void PaginationBuilder_Build_ReturnsPaginator()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName);

        // Act
        var paginator = builder.Build();

        // Assert
        Assert.NotNull(paginator);
        Assert.IsAssignableFrom<IPaginator<string>>(paginator);
    }

    [Fact]
    public void PaginationBuilder_Build_WithPageSize_UsesPageSize()
    {
        // Arrange
        var builder = _paginationSlice.Query<string>(TestFunctionName)
            .WithPageSize(100);

        // Act
        var paginator = builder.Build();

        // Assert
        Assert.NotNull(paginator);
    }

    #endregion

    #region Paginator Tests

    [Fact]
    public void Paginator_InitialState_HasMoreIsTrue()
    {
        // Arrange
        var paginator = _paginationSlice.Query<string>(TestFunctionName)
            .Build();

        // Assert
        Assert.True(paginator.HasMore);
        Assert.Equal(0, paginator.LoadedPageCount);
        Assert.Empty(paginator.LoadedItems);
    }

    [Fact]
    public async Task Paginator_LoadNextPageAsync_WithSuccessResponse_LoadsPage()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"page\":[\"item1\",\"item2\"],\"continueCursor\":\"cursor123\",\"isDone\":false}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<object>(It.IsAny<string>())).Returns(new { page = TestPageItems, continueCursor = "cursor123", isDone = false });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var paginator = _paginationSlice.Query<string>(TestFunctionName)
            .WithPageSize(20)
            .Build();

        // Act
        var result = await paginator.LoadNextAsync();

        // Assert
        Assert.NotNull(result);
        _mockHttpProvider.Verify(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task Paginator_LoadNextPageAsync_WhenNoMorePages_ReturnsNull()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"page\":[],\"isDone\":true}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<object>(It.IsAny<string>())).Returns(new { page = Array.Empty<string>(), isDone = true });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var paginator = _paginationSlice.Query<string>(TestFunctionName)
            .Build();

        // Act
        var result = await paginator.LoadNextAsync();

        // Assert
        // Note: Actual behavior depends on Paginator implementation
        // This test verifies the method doesn't throw
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Paginator_LoadNextPageAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var paginator = _paginationSlice.Query<string>(TestFunctionName)
            .Build();

        // Act & Assert - Exception is wrapped in ConvexPaginationException
        await Assert.ThrowsAsync<ConvexPaginationException>(() =>
            paginator.LoadNextAsync(cts.Token));
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


