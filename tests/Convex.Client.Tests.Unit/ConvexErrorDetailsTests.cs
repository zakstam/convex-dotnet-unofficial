using System;
using Convex.Client.Infrastructure.ErrorHandling;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexErrorDetailsTests
{
    #region FromException Tests

    [Fact]
    public void FromException_WithNullException_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(static () => ConvexErrorDetails.FromException(null!));

    [Fact]
    public void FromException_WithBasicConvexException_CreatesDetails()
    {
        // Arrange
        var exception = new ConvexException("Test error");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotNull(details);
    }

    [Fact]
    public void FromException_WithRequestContext_PopulatesDetails()
    {
        // Arrange
        var exception = new ConvexException("Test error")
        {
            RequestContext = new RequestContext
            {
                FunctionName = "myFunction",
                RequestType = "mutation",
                RequestId = "req-123"
            }
        };

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.Equal("myFunction", details.FunctionName);
        Assert.Equal("mutation", details.RequestType);
        Assert.Equal("req-123", details.RequestId);
    }

    [Fact]
    public void FromException_WithoutRequestContext_LeavesDetailsNull()
    {
        // Arrange
        var exception = new ConvexException("Test error");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.Null(details.FunctionName);
        Assert.Null(details.RequestType);
        Assert.Null(details.RequestId);
    }

    #endregion FromException Tests

    #region Suggestions Tests

    [Fact]
    public void FromException_ConvexFunctionException_AddsFunctionSuggestions()
    {
        // Arrange
        var exception = new ConvexFunctionException("Function failed", "myFunction");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("myFunction"));
    }

    [Fact]
    public void FromException_ConvexArgumentException_AddsArgumentSuggestions()
    {
        // Arrange
        var exception = new ConvexArgumentException("Invalid argument", "paramName");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("paramName"));
    }

    [Fact]
    public void FromException_ConvexNetworkException_AddsNetworkSuggestions()
    {
        // Arrange
        var exception = new ConvexNetworkException("Connection failed", NetworkErrorType.ConnectionFailure);

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("internet connection"));
    }

    [Fact]
    public void FromException_ConvexNetworkException_Timeout_AddsTimeoutSuggestions()
    {
        // Arrange
        var exception = new ConvexNetworkException("Timeout", NetworkErrorType.Timeout);

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.Contains(details.Suggestions, static s => s.Contains("timeout"));
    }

    [Fact]
    public void FromException_ConvexAuthenticationException_AddsAuthSuggestions()
    {
        // Arrange
        var exception = new ConvexAuthenticationException("Auth failed");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("authentication token"));
    }

    [Fact]
    public void FromException_ConvexRateLimitException_AddsRateLimitSuggestions()
    {
        // Arrange
        var exception = new ConvexRateLimitException("Rate limited", TimeSpan.FromSeconds(30), 100);

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("30"));
    }

    [Fact]
    public void FromException_ConvexCircuitBreakerException_AddsCircuitBreakerSuggestions()
    {
        // Arrange
        var exception = new ConvexCircuitBreakerException("Circuit open", CircuitBreakerState.Open);

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.NotEmpty(details.Suggestions);
        Assert.Contains(details.Suggestions, static s => s.Contains("circuit breaker"));
    }

    [Fact]
    public void FromException_BaseConvexException_HasNoSuggestions()
    {
        // Arrange
        var exception = new ConvexException("Generic error");

        // Act
        var details = ConvexErrorDetails.FromException(exception);

        // Assert
        Assert.Empty(details.Suggestions);
    }

    #endregion Suggestions Tests

    #region ToFormattedMessage Tests

    [Fact]
    public void ToFormattedMessage_WithNoContext_ReturnsBasicMessage()
    {
        // Arrange
        var details = new ConvexErrorDetails();

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("Error occurred", message);
    }

    [Fact]
    public void ToFormattedMessage_WithFunctionName_IncludesFunction()
    {
        // Arrange
        var details = new ConvexErrorDetails { FunctionName = "myFunction" };

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("Function: myFunction", message);
    }

    [Fact]
    public void ToFormattedMessage_WithRequestType_IncludesType()
    {
        // Arrange
        var details = new ConvexErrorDetails { RequestType = "mutation" };

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("Type: mutation", message);
    }

    [Fact]
    public void ToFormattedMessage_WithStatusCode_IncludesStatus()
    {
        // Arrange
        var details = new ConvexErrorDetails { StatusCode = 500 };

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("HTTP Status: 500", message);
    }

    [Fact]
    public void ToFormattedMessage_WithSuggestions_IncludesSuggestions()
    {
        // Arrange
        var details = new ConvexErrorDetails();
        details.Suggestions.Add("Check your configuration");
        details.Suggestions.Add("Verify the connection");

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("Suggestions:", message);
        Assert.Contains("Check your configuration", message);
        Assert.Contains("Verify the connection", message);
    }

    [Fact]
    public void ToFormattedMessage_WithTimestamp_IncludesTime()
    {
        // Arrange
        var details = new ConvexErrorDetails
        {
            Timestamp = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero)
        };

        // Act
        var message = details.ToFormattedMessage();

        // Assert
        Assert.Contains("Time:", message);
        Assert.Contains("2024", message);
    }

    #endregion ToFormattedMessage Tests

    #region Property Tests

    [Fact]
    public void FunctionName_CanBeSet()
    {
        // Arrange & Act
        var details = new ConvexErrorDetails { FunctionName = "testFunc" };

        // Assert
        Assert.Equal("testFunc", details.FunctionName);
    }

    [Fact]
    public void RequestType_CanBeSet()
    {
        // Arrange & Act
        var details = new ConvexErrorDetails { RequestType = "query" };

        // Assert
        Assert.Equal("query", details.RequestType);
    }

    [Fact]
    public void Arguments_CanBeSet()
    {
        // Arrange
        var args = new { name = "test", value = 42 };

        // Act
        var details = new ConvexErrorDetails { Arguments = args };

        // Assert
        Assert.Same(args, details.Arguments);
    }

    [Fact]
    public void Timestamp_DefaultsToUtcNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var details = new ConvexErrorDetails();

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.InRange(details.Timestamp, before, after);
    }

    [Fact]
    public void RequestId_CanBeSet()
    {
        // Arrange & Act
        var details = new ConvexErrorDetails { RequestId = "req-456" };

        // Assert
        Assert.Equal("req-456", details.RequestId);
    }

    [Fact]
    public void StatusCode_CanBeSet()
    {
        // Arrange & Act
        var details = new ConvexErrorDetails { StatusCode = 404 };

        // Assert
        Assert.Equal(404, details.StatusCode);
    }

    [Fact]
    public void Suggestions_IsInitialized()
    {
        // Arrange & Act
        var details = new ConvexErrorDetails();

        // Assert
        Assert.NotNull(details.Suggestions);
        Assert.Empty(details.Suggestions);
    }

    [Fact]
    public void Suggestions_CanAddItems()
    {
        // Arrange
        var details = new ConvexErrorDetails();

        // Act
        details.Suggestions.Add("First suggestion");
        details.Suggestions.Add("Second suggestion");

        // Assert
        Assert.Equal(2, details.Suggestions.Count);
    }

    #endregion Property Tests
}
