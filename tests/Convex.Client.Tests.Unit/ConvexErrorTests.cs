using System;
using System.Net.Http;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ErrorHandling;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexErrorTests
{
    #region FromException Factory Tests

    [Fact]
    public void FromException_ConvexException_ReturnsConvexFunctionError()
    {
        // Arrange
        var exception = new ConvexException("Test error");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<ConvexFunctionError>(result);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FromException_HttpRequestException_ReturnsNetworkError()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<NetworkError>(result);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FromException_TimeoutException_ReturnsTimeoutError()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<TimeoutError>(result);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FromException_OperationCanceledException_ReturnsCancellationError()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation was cancelled");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<CancellationError>(result);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FromException_TaskCanceledException_ReturnsCancellationError()
    {
        // Arrange - TaskCanceledException is derived from OperationCanceledException
        var exception = new TaskCanceledException("Task was cancelled");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<CancellationError>(result);
    }

    [Fact]
    public void FromException_GenericException_ReturnsUnexpectedError()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<UnexpectedError>(result);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void FromException_ArgumentException_ReturnsUnexpectedError()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<UnexpectedError>(result);
    }

    [Fact]
    public void FromException_ConvexFunctionException_ReturnsConvexFunctionError()
    {
        // Arrange - Derived ConvexException types should still return ConvexFunctionError
        var exception = new ConvexFunctionException("Function failed", "myFunction");

        // Act
        var result = ConvexError.FromException(exception);

        // Assert
        _ = Assert.IsType<ConvexFunctionError>(result);
    }

    #endregion FromException Factory Tests

    #region Message Property Tests

    [Fact]
    public void Message_ReturnsExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test message");
        var error = ConvexError.FromException(exception);

        // Act
        var message = error.Message;

        // Assert
        Assert.Equal("Test message", message);
    }

    [Fact]
    public void Message_WithConvexException_ReturnsCorrectMessage()
    {
        // Arrange
        var exception = new ConvexException("Convex error message");
        var error = ConvexError.FromException(exception);

        // Act
        var message = error.Message;

        // Assert
        Assert.Equal("Convex error message", message);
    }

    #endregion Message Property Tests

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsTypNameAndMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("UnexpectedError: Test error", result);
    }

    [Fact]
    public void ToString_ConvexFunctionError_ReturnsCorrectFormat()
    {
        // Arrange
        var exception = new ConvexException("Function failed");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("ConvexFunctionError: Function failed", result);
    }

    [Fact]
    public void ToString_NetworkError_ReturnsCorrectFormat()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("NetworkError: Connection failed", result);
    }

    [Fact]
    public void ToString_TimeoutError_ReturnsCorrectFormat()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("TimeoutError: Request timed out", result);
    }

    [Fact]
    public void ToString_CancellationError_ReturnsCorrectFormat()
    {
        // Arrange
        var exception = new OperationCanceledException("Cancelled");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("CancellationError: Cancelled", result);
    }

    #endregion ToString Tests

    #region Match Tests

    [Fact]
    public void Match_ConvexFunctionError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new ConvexException("Test");
        var error = ConvexError.FromException(exception);
        var handlerCalled = false;

        // Act
        var result = error.Match(
            onConvexError: _ => { handlerCalled = true; return "convex"; },
            onNetworkError: _ => "network",
            onTimeoutError: _ => "timeout",
            onCancellationError: _ => "cancelled",
            onUnexpectedError: _ => "unexpected");

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("convex", result);
    }

    [Fact]
    public void Match_NetworkError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new HttpRequestException("Test");
        var error = ConvexError.FromException(exception);
        var handlerCalled = false;

        // Act
        var result = error.Match(
            onConvexError: _ => "convex",
            onNetworkError: _ => { handlerCalled = true; return "network"; },
            onTimeoutError: _ => "timeout",
            onCancellationError: _ => "cancelled",
            onUnexpectedError: _ => "unexpected");

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("network", result);
    }

    [Fact]
    public void Match_TimeoutError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new TimeoutException("Test");
        var error = ConvexError.FromException(exception);
        var handlerCalled = false;

        // Act
        var result = error.Match(
            onConvexError: _ => "convex",
            onNetworkError: _ => "network",
            onTimeoutError: _ => { handlerCalled = true; return "timeout"; },
            onCancellationError: _ => "cancelled",
            onUnexpectedError: _ => "unexpected");

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("timeout", result);
    }

    [Fact]
    public void Match_CancellationError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new OperationCanceledException("Test");
        var error = ConvexError.FromException(exception);
        var handlerCalled = false;

        // Act
        var result = error.Match(
            onConvexError: _ => "convex",
            onNetworkError: _ => "network",
            onTimeoutError: _ => "timeout",
            onCancellationError: _ => { handlerCalled = true; return "cancelled"; },
            onUnexpectedError: _ => "unexpected");

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("cancelled", result);
    }

    [Fact]
    public void Match_UnexpectedError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");
        var error = ConvexError.FromException(exception);
        var handlerCalled = false;

        // Act
        var result = error.Match(
            onConvexError: _ => "convex",
            onNetworkError: _ => "network",
            onTimeoutError: _ => "timeout",
            onCancellationError: _ => "cancelled",
            onUnexpectedError: _ => { handlerCalled = true; return "unexpected"; });

        // Assert
        Assert.True(handlerCalled);
        Assert.Equal("unexpected", result);
    }

    [Fact]
    public void Match_ReturnsCorrectValue()
    {
        // Arrange
        var exception = new ConvexException("Error code: 42");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.Match(
            onConvexError: static _ => 42,
            onNetworkError: static _ => 0,
            onTimeoutError: static _ => 0,
            onCancellationError: static _ => 0,
            onUnexpectedError: static _ => 0);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Match_PassesCorrectErrorToHandler()
    {
        // Arrange
        var exception = new ConvexFunctionException("Test", "myFunction");
        var error = ConvexError.FromException(exception);
        string? capturedFunctionName = null;

        // Act
        _ = error.Match(
            onConvexError: e =>
            {
                if (e.Exception is ConvexFunctionException funcEx)
                {
                    capturedFunctionName = funcEx.FunctionName;
                }
                return true;
            },
            onNetworkError: _ => false,
            onTimeoutError: _ => false,
            onCancellationError: _ => false,
            onUnexpectedError: _ => false);

        // Assert
        Assert.Equal("myFunction", capturedFunctionName);
    }

    #endregion Match Tests

    #region MatchOrDefault Tests

    [Fact]
    public void MatchOrDefault_ConvexFunctionError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new ConvexException("Test");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.MatchOrDefault(
            onConvexError: static _ => "matched",
            onNetworkError: static _ => "network",
            onTimeoutError: static _ => "timeout",
            onCancellationError: static _ => "cancelled",
            onUnexpectedError: static _ => "unexpected");

        // Assert
        Assert.Equal("matched", result);
    }

    [Fact]
    public void MatchOrDefault_NetworkError_CallsCorrectHandler()
    {
        // Arrange
        var exception = new HttpRequestException("Test");
        var error = ConvexError.FromException(exception);

        // Act
        var result = error.MatchOrDefault(
            onConvexError: static _ => "convex",
            onNetworkError: static _ => "matched",
            onTimeoutError: static _ => "timeout",
            onCancellationError: static _ => "cancelled",
            onUnexpectedError: static _ => "unexpected");

        // Assert
        Assert.Equal("matched", result);
    }

    #endregion MatchOrDefault Tests

    #region ConvexFunctionError Specific Tests

    [Fact]
    public void ConvexFunctionError_Exception_ReturnsTypedConvexException()
    {
        // Arrange
        var exception = new ConvexException("Test");
        var error = (ConvexFunctionError)ConvexError.FromException(exception);

        // Act
        var typedException = error.Exception;

        // Assert
        Assert.Same(exception, typedException);
        _ = Assert.IsType<ConvexException>(typedException);
    }

    [Fact]
    public void ConvexFunctionError_ErrorData_ReturnsExceptionErrorData()
    {
        // Arrange
        var exception = new ConvexException("Test");
        var error = (ConvexFunctionError)ConvexError.FromException(exception);

        // Act
        var errorData = error.ErrorData;

        // Assert - ErrorData should be null when not set
        Assert.Null(errorData);
    }

    #endregion ConvexFunctionError Specific Tests

    #region NetworkError Specific Tests

    [Fact]
    public void NetworkError_Exception_ReturnsTypedHttpRequestException()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");
        var error = (NetworkError)ConvexError.FromException(exception);

        // Act
        var typedException = error.Exception;

        // Assert
        Assert.Same(exception, typedException);
        _ = Assert.IsType<HttpRequestException>(typedException);
    }

    #endregion NetworkError Specific Tests

    #region TimeoutError Specific Tests

    [Fact]
    public void TimeoutError_Exception_ReturnsTypedTimeoutException()
    {
        // Arrange
        var exception = new TimeoutException("Timed out");
        var error = (TimeoutError)ConvexError.FromException(exception);

        // Act
        var typedException = error.Exception;

        // Assert
        Assert.Same(exception, typedException);
        _ = Assert.IsType<TimeoutException>(typedException);
    }

    #endregion TimeoutError Specific Tests

    #region CancellationError Specific Tests

    [Fact]
    public void CancellationError_Exception_ReturnsTypedOperationCanceledException()
    {
        // Arrange
        var exception = new OperationCanceledException("Cancelled");
        var error = (CancellationError)ConvexError.FromException(exception);

        // Act
        var typedException = error.Exception;

        // Assert
        Assert.Same(exception, typedException);
        _ = Assert.IsType<OperationCanceledException>(typedException);
    }

    #endregion CancellationError Specific Tests
}
