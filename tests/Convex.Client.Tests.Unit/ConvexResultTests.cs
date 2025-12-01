using System;
using System.Net.Http;
using Convex.Client.Infrastructure.ErrorHandling;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexResultTests
{
    #region Success Factory Tests

    [Fact]
    public void Success_WithValue_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = ConvexResult<int>.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_WithNullReferenceType_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = ConvexResult<string>.Success(null!);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Success_WithComplexObject_CreatesSuccessResult()
    {
        // Arrange
        var testObject = new TestData { Id = 1, Name = "Test" };

        // Act
        var result = ConvexResult<TestData>.Success(testObject);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(testObject, result.Value);
    }

    #endregion Success Factory Tests

    #region Failure Factory Tests

    [Fact]
    public void Failure_WithConvexError_CreatesFailureResult()
    {
        // Arrange
        var error = ConvexError.FromException(new InvalidOperationException("Test error"));

        // Act
        var result = ConvexResult<int>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_WithNullError_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(static () => ConvexResult<int>.Failure((ConvexError)null!));

    [Fact]
    public void Failure_WithException_CreatesFailureResult()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var result = ConvexResult<int>.Failure(exception);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        _ = Assert.IsType<UnexpectedError>(result.Error);
        Assert.Equal(exception, result.Error.Exception);
    }

    [Fact]
    public void Failure_WithHttpRequestException_CreatesNetworkError()
    {
        // Arrange
        var exception = new HttpRequestException("Network error");

        // Act
        var result = ConvexResult<int>.Failure(exception);

        // Assert
        _ = Assert.IsType<NetworkError>(result.Error);
    }

    [Fact]
    public void Failure_WithTimeoutException_CreatesTimeoutError()
    {
        // Arrange
        var exception = new TimeoutException("Timed out");

        // Act
        var result = ConvexResult<int>.Failure(exception);

        // Assert
        _ = Assert.IsType<TimeoutError>(result.Error);
    }

    [Fact]
    public void Failure_WithOperationCanceledException_CreatesCancellationError()
    {
        // Arrange
        var exception = new OperationCanceledException("Cancelled");

        // Act
        var result = ConvexResult<int>.Failure(exception);

        // Assert
        _ = Assert.IsType<CancellationError>(result.Error);
    }

    #endregion Failure Factory Tests

    #region Value Property Tests

    [Fact]
    public void Value_OnSuccess_ReturnsValue()
    {
        // Arrange
        var result = ConvexResult<string>.Success("test");

        // Act
        var value = result.Value;

        // Assert
        Assert.Equal("test", value);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        Assert.Contains("Cannot access Value on a failed result", ex.Message);
    }

    #endregion Value Property Tests

    #region Error Property Tests

    [Fact]
    public void Error_OnFailure_ReturnsError()
    {
        // Arrange
        var error = ConvexError.FromException(new InvalidOperationException("Test"));
        var result = ConvexResult<int>.Failure(error);

        // Act
        var returnedError = result.Error;

        // Assert
        Assert.Equal(error, returnedError);
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        Assert.Contains("Cannot access Error on a successful result", ex.Message);
    }

    #endregion Error Property Tests

    #region Match Tests (Func overload)

    [Fact]
    public void Match_OnSuccess_CallsOnSuccessFunction()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Message}");

        // Assert
        Assert.Equal("Value: 42", output);
    }

    [Fact]
    public void Match_OnFailure_CallsOnFailureFunction()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Test error"));

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Message}");

        // Assert
        Assert.Equal("Error: Test error", output);
    }

    [Fact]
    public void Match_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.Match(
                onSuccess: (Func<int, string>)null!,
                onFailure: static _ => "Error"));
    }

    [Fact]
    public void Match_WithNullOnFailure_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.Match(
                onSuccess: static _ => "Value",
                onFailure: null!));
    }

    #endregion Match Tests (Func overload)

    #region Match Tests (Action overload)

    [Fact]
    public void MatchAction_OnSuccess_CallsOnSuccessAction()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var successCalled = false;
        var failureCalled = false;

        // Act
        _ = result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        // Assert
        Assert.True(successCalled);
        Assert.False(failureCalled);
    }

    [Fact]
    public void MatchAction_OnFailure_CallsOnFailureAction()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));
        var successCalled = false;
        var failureCalled = false;

        // Act
        _ = result.Match(
            onSuccess: _ => successCalled = true,
            onFailure: _ => failureCalled = true);

        // Assert
        Assert.False(successCalled);
        Assert.True(failureCalled);
    }

    [Fact]
    public void MatchAction_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.Match(
                onSuccess: null!,
                onFailure: static _ => { }));
    }

    [Fact]
    public void MatchAction_WithNullOnFailure_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.Match(
                onSuccess: static _ => { },
                onFailure: null!));
    }

    #endregion Match Tests (Action overload)

    #region OnSuccess Tests

    [Fact]
    public void OnSuccess_WithSuccessResult_ExecutesAction()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var capturedValue = 0;

        // Act
        var returnedResult = result.OnSuccess(v => capturedValue = v);

        // Assert
        Assert.Equal(42, capturedValue);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnSuccess_WithFailureResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));
        var actionCalled = false;

        // Act
        var returnedResult = result.OnSuccess(_ => actionCalled = true);

        // Assert
        Assert.False(actionCalled);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnSuccess_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => result.OnSuccess(null!));
    }

    #endregion OnSuccess Tests

    #region OnFailure Tests

    [Fact]
    public void OnFailure_WithFailureResult_ExecutesAction()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Test error"));
        ConvexError? capturedError = null;

        // Act
        var returnedResult = result.OnFailure(e => capturedError = e);

        // Assert
        Assert.NotNull(capturedError);
        Assert.Equal("Test error", capturedError.Message);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnFailure_WithSuccessResult_DoesNotExecuteAction()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var actionCalled = false;

        // Act
        var returnedResult = result.OnFailure(_ => actionCalled = true);

        // Assert
        Assert.False(actionCalled);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnFailure_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => result.OnFailure(null!));
    }

    #endregion OnFailure Tests

    #region Map Tests

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        // Arrange
        var result = ConvexResult<int>.Success(21);

        // Act
        var mapped = result.Map(static v => v * 2);

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        // Arrange
        var error = ConvexError.FromException(new InvalidOperationException("Error"));
        var result = ConvexResult<int>.Failure(error);

        // Act
        var mapped = result.Map(static v => v * 2);

        // Assert
        Assert.True(mapped.IsFailure);
        Assert.Equal(error, mapped.Error);
    }

    [Fact]
    public void Map_WithTypeChange_TransformsCorrectly()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act
        var mapped = result.Map(static v => v.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("42", mapped.Value);
    }

    [Fact]
    public void Map_WithNullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => result.Map<string>(null!));
    }

    [Fact]
    public void Map_ChainedTransformations_WorkCorrectly()
    {
        // Arrange
        var result = ConvexResult<int>.Success(5);

        // Act
        var mapped = result
            .Map(static v => v * 2)
            .Map(static v => v + 3)
            .Map(static v => v.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.True(mapped.IsSuccess);
        Assert.Equal("13", mapped.Value);
    }

    #endregion Map Tests

    #region Bind Tests

    [Fact]
    public void Bind_OnSuccess_ExecutesBinder()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act
        var bound = result.Bind(static v => ConvexResult<string>.Success($"Value: {v}"));

        // Assert
        Assert.True(bound.IsSuccess);
        Assert.Equal("Value: 42", bound.Value);
    }

    [Fact]
    public void Bind_OnFailure_PropagatesError()
    {
        // Arrange
        var error = ConvexError.FromException(new InvalidOperationException("Error"));
        var result = ConvexResult<int>.Failure(error);
        var binderCalled = false;

        // Act
        var bound = result.Bind(_ =>
        {
            binderCalled = true;
            return ConvexResult<string>.Success("Should not be called");
        });

        // Assert
        Assert.False(binderCalled);
        Assert.True(bound.IsFailure);
        Assert.Equal(error, bound.Error);
    }

    [Fact]
    public void Bind_BinderReturnsFailure_PropagatesNewError()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var newError = ConvexError.FromException(new InvalidOperationException("Binder error"));

        // Act
        var bound = result.Bind(_ => ConvexResult<string>.Failure(newError));

        // Assert
        Assert.True(bound.IsFailure);
        Assert.Equal(newError, bound.Error);
    }

    [Fact]
    public void Bind_WithNullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.Bind<string>(null!));
    }

    #endregion Bind Tests

    #region GetValueOrDefault Tests

    [Fact]
    public void GetValueOrDefault_OnSuccess_ReturnsValue()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act
        var value = result.GetValueOrDefault(0);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ReturnsDefault()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));

        // Act
        var value = result.GetValueOrDefault(99);

        // Assert
        Assert.Equal(99, value);
    }

    [Fact]
    public void GetValueOrDefault_OnSuccess_WithFactory_ReturnsValue()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var factoryCalled = false;

        // Act
        var value = result.GetValueOrDefault(_ =>
        {
            factoryCalled = true;
            return 0;
        });

        // Assert
        Assert.Equal(42, value);
        Assert.False(factoryCalled);
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_WithFactory_CallsFactory()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Test error"));

        // Act
        var value = result.GetValueOrDefault(static e => e.Message.Length);

        // Assert
        Assert.Equal("Test error".Length, value);
    }

    [Fact]
    public void GetValueOrDefault_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var result = ConvexResult<int>.Failure(new InvalidOperationException("Error"));

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            result.GetValueOrDefault(null!));
    }

    #endregion GetValueOrDefault Tests

    #region ToString Tests

    [Fact]
    public void ToString_OnSuccess_ReturnsFormattedString()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);

        // Act
        var str = result.ToString();

        // Assert
        Assert.Equal("Success(42)", str);
    }

    [Fact]
    public void ToString_OnFailure_ReturnsFormattedString()
    {
        // Arrange
        var result = ConvexResult<string>.Failure(new InvalidOperationException("Test error"));

        // Act
        var str = result.ToString();

        // Assert
        Assert.StartsWith("Failure(", str);
        Assert.Contains("Test error", str);
    }

    #endregion ToString Tests

    #region Chaining Tests

    [Fact]
    public void Fluent_OnSuccessAndOnFailure_CanBeChained()
    {
        // Arrange
        var result = ConvexResult<int>.Success(42);
        var successCalled = false;
        var failureCalled = false;

        // Act
        _ = result
            .OnSuccess(_ => successCalled = true)
            .OnFailure(_ => failureCalled = true);

        // Assert
        Assert.True(successCalled);
        Assert.False(failureCalled);
    }

    #endregion Chaining Tests

    #region Helper Classes

    private sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion Helper Classes
}
