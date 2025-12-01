using System.Collections.Generic;
using Convex.Client.Infrastructure.Validation;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class SchemaValidationResultTests
{
    #region Success Tests

    [Fact]
    public void Success_CreatesValidResult()
    {
        // Act
        var result = SchemaValidationResult.Success("string", "string");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_SetsExpectedType()
    {
        // Act
        var result = SchemaValidationResult.Success("number", "number");

        // Assert
        Assert.Equal("number", result.ExpectedType);
    }

    [Fact]
    public void Success_SetsActualType()
    {
        // Act
        var result = SchemaValidationResult.Success("boolean", "boolean");

        // Assert
        Assert.Equal("boolean", result.ActualType);
    }

    [Fact]
    public void Success_WithDifferentTypes_StillSucceeds()
    {
        // Act - Success can be called with different types (e.g., compatible types)
        var result = SchemaValidationResult.Success("int64", "number");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("int64", result.ExpectedType);
        Assert.Equal("number", result.ActualType);
    }

    [Fact]
    public void Success_HasEmptyErrorsList()
    {
        // Act
        var result = SchemaValidationResult.Success("object", "object");

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
    }

    #endregion Success Tests

    #region Failure Tests with Params

    [Fact]
    public void Failure_WithSingleError_CreatesInvalidResult()
    {
        // Act
        var result = SchemaValidationResult.Failure("string", "number", "Type mismatch");

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Failure_WithSingleError_ContainsError()
    {
        // Act
        var result = SchemaValidationResult.Failure("string", "number", "Type mismatch");

        // Assert
        var singleError = Assert.Single(result.Errors);
        Assert.Equal("Type mismatch", singleError);
    }

    [Fact]
    public void Failure_WithMultipleErrors_ContainsAllErrors()
    {
        // Act
        var result = SchemaValidationResult.Failure(
            "object",
            "string",
            "Type mismatch",
            "Missing required field",
            "Invalid format");

        // Assert
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains("Type mismatch", result.Errors);
        Assert.Contains("Missing required field", result.Errors);
        Assert.Contains("Invalid format", result.Errors);
    }

    [Fact]
    public void Failure_SetsExpectedType()
    {
        // Act
        var result = SchemaValidationResult.Failure("array", "object", "Not an array");

        // Assert
        Assert.Equal("array", result.ExpectedType);
    }

    [Fact]
    public void Failure_SetsActualType()
    {
        // Act
        var result = SchemaValidationResult.Failure("array", "object", "Not an array");

        // Assert
        Assert.Equal("object", result.ActualType);
    }

    [Fact]
    public void Failure_WithNoErrors_StillInvalid()
    {
        // Act
        var result = SchemaValidationResult.Failure("string", "number");

        // Assert
        Assert.False(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion Failure Tests with Params

    #region Failure Tests with IReadOnlyList

    [Fact]
    public void Failure_WithErrorList_CreatesInvalidResult()
    {
        // Arrange
        IReadOnlyList<string> errors = ["Error 1", "Error 2"];

        // Act
        var result = SchemaValidationResult.Failure("boolean", "string", errors);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Failure_WithErrorList_ContainsAllErrors()
    {
        // Arrange
        IReadOnlyList<string> errors = ["Error 1", "Error 2", "Error 3"];

        // Act
        var result = SchemaValidationResult.Failure("boolean", "string", errors);

        // Assert
        Assert.Equal(3, result.Errors.Count);
        Assert.Equal("Error 1", result.Errors[0]);
        Assert.Equal("Error 2", result.Errors[1]);
        Assert.Equal("Error 3", result.Errors[2]);
    }

    [Fact]
    public void Failure_WithEmptyErrorList_StillInvalid()
    {
        // Arrange
        IReadOnlyList<string> errors = [];

        // Act
        var result = SchemaValidationResult.Failure("int", "string", errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_WithErrorList_SetsExpectedType()
    {
        // Arrange
        IReadOnlyList<string> errors = ["Error"];

        // Act
        var result = SchemaValidationResult.Failure("float", "int", errors);

        // Assert
        Assert.Equal("float", result.ExpectedType);
    }

    [Fact]
    public void Failure_WithErrorList_SetsActualType()
    {
        // Arrange
        IReadOnlyList<string> errors = ["Error"];

        // Act
        var result = SchemaValidationResult.Failure("float", "int", errors);

        // Assert
        Assert.Equal("int", result.ActualType);
    }

    #endregion Failure Tests with IReadOnlyList

    #region Edge Cases

    [Fact]
    public void Success_WithEmptyStrings_Works()
    {
        // Act
        var result = SchemaValidationResult.Success("", "");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("", result.ExpectedType);
        Assert.Equal("", result.ActualType);
    }

    [Fact]
    public void Failure_WithEmptyStrings_Works()
    {
        // Act
        var result = SchemaValidationResult.Failure("", "", "Error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("", result.ExpectedType);
        Assert.Equal("", result.ActualType);
    }

    [Fact]
    public void Success_WithComplexTypeNames_Works()
    {
        // Act
        var result = SchemaValidationResult.Success(
            "Array<Object<string, number>>",
            "Array<Object<string, number>>");

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Array<Object<string, number>>", result.ExpectedType);
    }

    [Fact]
    public void Failure_WithLongErrorMessage_Works()
    {
        // Arrange
        var longError = new string('x', 10000);

        // Act
        var result = SchemaValidationResult.Failure("type1", "type2", longError);

        // Assert
        var singleError = Assert.Single(result.Errors);
        Assert.Equal(10000, singleError.Length);
    }

    [Fact]
    public void Errors_IsReadOnly()
    {
        // Arrange
        var result = SchemaValidationResult.Failure("a", "b", "error");

        // Assert - IReadOnlyList should not expose mutation methods
        _ = Assert.IsAssignableFrom<IReadOnlyList<string>>(result.Errors);
    }

    #endregion Edge Cases
}
