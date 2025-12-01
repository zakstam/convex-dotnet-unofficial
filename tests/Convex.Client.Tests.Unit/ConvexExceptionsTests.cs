using System;
using System.Linq;
using System.Net;
using Convex.Client.Infrastructure.ErrorHandling;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexExceptionsTests
{
    #region ConvexException Tests

    [Fact]
    public void ConvexException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new ConvexException("Test error message");

        // Assert
        Assert.Equal("Test error message", exception.Message);
    }

    [Fact]
    public void ConvexException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ConvexException("Outer error", inner);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ConvexException_ErrorCode_IsNullByDefault()
    {
        // Arrange & Act
        var exception = new ConvexException("Test");

        // Assert
        Assert.Null(exception.ErrorCode);
    }

    [Fact]
    public void ConvexException_ErrorCode_CanBeSet()
    {
        // Arrange & Act
        var exception = new ConvexException("Test") { ErrorCode = "ERR001" };

        // Assert
        Assert.Equal("ERR001", exception.ErrorCode);
    }

    [Fact]
    public void ConvexException_ErrorData_IsNullByDefault()
    {
        // Arrange & Act
        var exception = new ConvexException("Test");

        // Assert
        Assert.Null(exception.ErrorData);
    }

    [Fact]
    public void ConvexException_RequestContext_IsNullByDefault()
    {
        // Arrange & Act
        var exception = new ConvexException("Test");

        // Assert
        Assert.Null(exception.RequestContext);
    }

    [Fact]
    public void ConvexException_RequestContext_CanBeSet()
    {
        // Arrange
        var exception = new ConvexException("Test");
        var context = new RequestContext { FunctionName = "myFunction", RequestType = "query" };

        // Act
        exception.RequestContext = context;

        // Assert
        Assert.Same(context, exception.RequestContext);
    }

    [Fact]
    public void ConvexException_GetDetailedMessage_ReturnsMessageWhenNoDetails()
    {
        // Arrange
        var exception = new ConvexException("Test error");

        // Act
        var detailed = exception.GetDetailedMessage();

        // Assert
        Assert.Contains("Test error", detailed);
    }

    [Fact]
    public void ConvexException_ToString_ContainsMessage()
    {
        // Arrange
        var exception = new ConvexException("Test error");

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Contains("Test error", result);
        Assert.Contains("ConvexException", result);
    }

    #endregion ConvexException Tests

    #region ConvexFunctionException Tests

    [Fact]
    public void ConvexFunctionException_WithMessageAndFunctionName_SetsBoth()
    {
        // Arrange & Act
        var exception = new ConvexFunctionException("Function failed", "myFunction");

        // Assert
        Assert.Equal("Function failed", exception.Message);
        Assert.Equal("myFunction", exception.FunctionName);
    }

    [Fact]
    public void ConvexFunctionException_WithInnerException_SetsAll()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ConvexFunctionException("Outer error", "myFunction", inner);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.Equal("myFunction", exception.FunctionName);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ConvexFunctionException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexFunctionException("Test", "func");

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexFunctionException Tests

    #region ConvexArgumentException Tests

    [Fact]
    public void ConvexArgumentException_WithMessageAndArgumentName_SetsBoth()
    {
        // Arrange & Act
        var exception = new ConvexArgumentException("Invalid argument", "paramName");

        // Assert
        Assert.Equal("Invalid argument", exception.Message);
        Assert.Equal("paramName", exception.ArgumentName);
    }

    [Fact]
    public void ConvexArgumentException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexArgumentException("Test", "arg");

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexArgumentException Tests

    #region ConvexNetworkException Tests

    [Fact]
    public void ConvexNetworkException_WithMessageAndErrorType_SetsBoth()
    {
        // Arrange & Act
        var exception = new ConvexNetworkException("Connection failed", NetworkErrorType.ConnectionFailure);

        // Assert
        Assert.Equal("Connection failed", exception.Message);
        Assert.Equal(NetworkErrorType.ConnectionFailure, exception.ErrorType);
    }

    [Fact]
    public void ConvexNetworkException_WithInnerException_SetsAll()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ConvexNetworkException("Outer error", NetworkErrorType.Timeout, inner);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.Equal(NetworkErrorType.Timeout, exception.ErrorType);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ConvexNetworkException_StatusCode_IsNullByDefault()
    {
        // Arrange & Act
        var exception = new ConvexNetworkException("Test", NetworkErrorType.ServerError);

        // Assert
        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void ConvexNetworkException_StatusCode_CanBeSet()
    {
        // Arrange & Act
        var exception = new ConvexNetworkException("Test", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
    }

    [Fact]
    public void ConvexNetworkException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexNetworkException("Test", NetworkErrorType.Timeout);

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexNetworkException Tests

    #region ConvexAuthenticationException Tests

    [Fact]
    public void ConvexAuthenticationException_WithMessage_SetsMessage()
    {
        // Arrange & Act
        var exception = new ConvexAuthenticationException("Authentication failed");

        // Assert
        Assert.Equal("Authentication failed", exception.Message);
    }

    [Fact]
    public void ConvexAuthenticationException_WithInnerException_SetsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ConvexAuthenticationException("Outer error", inner);

        // Assert
        Assert.Equal("Outer error", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void ConvexAuthenticationException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexAuthenticationException("Test");

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexAuthenticationException Tests

    #region ConvexRateLimitException Tests

    [Fact]
    public void ConvexRateLimitException_SetsAllProperties()
    {
        // Arrange & Act
        var exception = new ConvexRateLimitException("Rate limit exceeded", TimeSpan.FromSeconds(30), 100);

        // Assert
        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(TimeSpan.FromSeconds(30), exception.RetryAfter);
        Assert.Equal(100, exception.CurrentLimit);
    }

    [Fact]
    public void ConvexRateLimitException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexRateLimitException("Test", TimeSpan.FromSeconds(5), 50);

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexRateLimitException Tests

    #region ConvexCircuitBreakerException Tests

    [Fact]
    public void ConvexCircuitBreakerException_SetsAllProperties()
    {
        // Arrange & Act
        var exception = new ConvexCircuitBreakerException("Circuit breaker open", CircuitBreakerState.Open);

        // Assert
        Assert.Equal("Circuit breaker open", exception.Message);
        Assert.Equal(CircuitBreakerState.Open, exception.CircuitState);
    }

    [Fact]
    public void ConvexCircuitBreakerException_IsConvexException()
    {
        // Arrange & Act
        var exception = new ConvexCircuitBreakerException("Test", CircuitBreakerState.HalfOpen);

        // Assert
        _ = Assert.IsAssignableFrom<ConvexException>(exception);
    }

    #endregion ConvexCircuitBreakerException Tests

    #region NetworkErrorType Enum Tests

    [Fact]
    public void NetworkErrorType_AllValuesAreDistinct()
    {
        // Arrange
        var values = new[]
        {
            NetworkErrorType.Timeout,
            NetworkErrorType.DnsResolution,
            NetworkErrorType.SslCertificate,
            NetworkErrorType.ServerError,
            NetworkErrorType.ConnectionFailure
        };

        // Assert - all values should be unique
        Assert.Equal(5, values.Distinct().Count());
    }

    [Fact]
    public void NetworkErrorType_CanBeUsedInSwitch()
    {
        // Act
        var result = NetworkErrorType.Timeout switch
        {
            NetworkErrorType.Timeout => "timeout",
            NetworkErrorType.DnsResolution => "dns",
            NetworkErrorType.SslCertificate => "ssl",
            NetworkErrorType.ServerError => "server",
            NetworkErrorType.ConnectionFailure => "connection",
            _ => "unknown"
        };

        // Assert
        Assert.Equal("timeout", result);
    }

    #endregion NetworkErrorType Enum Tests

    #region CircuitBreakerState Enum Tests

    [Fact]
    public void CircuitBreakerState_AllValuesAreDistinct()
    {
        // Arrange
        var values = new[]
        {
            CircuitBreakerState.Closed,
            CircuitBreakerState.Open,
            CircuitBreakerState.HalfOpen
        };

        // Assert - all values should be unique
        Assert.Equal(3, values.Distinct().Count());
    }

    [Fact]
    public void CircuitBreakerState_CanBeUsedInSwitch()
    {
        // Act
        var result = CircuitBreakerState.Open switch
        {
            CircuitBreakerState.Closed => "closed",
            CircuitBreakerState.Open => "open",
            CircuitBreakerState.HalfOpen => "half-open",
            _ => "unknown"
        };

        // Assert
        Assert.Equal("open", result);
    }

    #endregion CircuitBreakerState Enum Tests

    #region RequestContext Tests

    [Fact]
    public void RequestContext_DefaultValues_AreEmptyStrings()
    {
        // Arrange & Act
        var context = new RequestContext();

        // Assert
        Assert.Equal("", context.FunctionName);
        Assert.Equal("", context.RequestType);
        Assert.Equal("", context.RequestId);
    }

    [Fact]
    public void RequestContext_Timestamp_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var context = new RequestContext();

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(context.Timestamp, before, after);
    }

    [Fact]
    public void RequestContext_Properties_CanBeSet()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddHours(-1);

        // Act
        var context = new RequestContext
        {
            FunctionName = "myFunction",
            RequestType = "mutation",
            RequestId = "req-123",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("myFunction", context.FunctionName);
        Assert.Equal("mutation", context.RequestType);
        Assert.Equal("req-123", context.RequestId);
        Assert.Equal(timestamp, context.Timestamp);
    }

    #endregion RequestContext Tests
}
