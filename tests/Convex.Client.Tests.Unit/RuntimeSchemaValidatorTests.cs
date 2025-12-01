using System;
using System.Collections.Generic;
using Convex.Client.Infrastructure.Validation;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class RuntimeSchemaValidatorTests
{
    private readonly RuntimeSchemaValidator _validator = new();
    private readonly SchemaValidationOptions _defaultOptions = new();
    private readonly SchemaValidationOptions _strictOptions = SchemaValidationOptions.Strict();

    #region Validate<TExpected> Generic Tests

    [Fact]
    public void ValidateGeneric_WithMatchingType_ReturnsSuccess()
    {
        // Arrange
        const string value = "test";

        // Act
        var result = _validator.Validate<string>(value, "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateGeneric_WithMismatchedType_ReturnsFailure()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = _validator.Validate<string>(value, "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion Validate<TExpected> Generic Tests

    #region Null Value Handling

    [Fact]
    public void Validate_NullValueWithReferenceType_ReturnsSuccess()
    {
        // Arrange
        const string? value = null;

        // Act
        var result = _validator.Validate(value, typeof(string), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("null", result.ActualType);
    }

    [Fact]
    public void Validate_NullValueWithNullableValueType_ReturnsSuccess()
    {
        // Arrange
        int? value = null;

        // Act
        var result = _validator.Validate(value, typeof(int?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullValueWithNonNullableValueType_ReturnsFailure()
    {
        // Arrange
        int? value = null;

        // Act
        var result = _validator.Validate(value, typeof(int), "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Expected non-null value of type int, but got null", result.Errors);
    }

    #endregion Null Value Handling

    #region Exact Type Match

    [Fact]
    public void Validate_ExactStringMatch_ReturnsSuccess()
    {
        // Arrange
        const string value = "hello";

        // Act
        var result = _validator.Validate(value, typeof(string), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("string", result.ExpectedType);
        Assert.Equal("string", result.ActualType);
    }

    [Fact]
    public void Validate_ExactIntMatch_ReturnsSuccess()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = _validator.Validate(value, typeof(int), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("int", result.ExpectedType);
        Assert.Equal("int", result.ActualType);
    }

    [Fact]
    public void Validate_ExactLongMatch_ReturnsSuccess()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var result = _validator.Validate(value, typeof(long), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("long", result.ExpectedType);
        Assert.Equal("long", result.ActualType);
    }

    [Fact]
    public void Validate_ExactDoubleMatch_ReturnsSuccess()
    {
        // Arrange
        const double value = 3.14159;

        // Act
        var result = _validator.Validate(value, typeof(double), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("double", result.ExpectedType);
        Assert.Equal("double", result.ActualType);
    }

    [Fact]
    public void Validate_ExactBoolMatch_ReturnsSuccess()
    {
        // Arrange
        const bool value = true;

        // Act
        var result = _validator.Validate(value, typeof(bool), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("bool", result.ExpectedType);
        Assert.Equal("bool", result.ActualType);
    }

    #endregion Exact Type Match

    #region Assignable Type Match (Non-Strict Mode)

    [Fact]
    public void Validate_AssignableType_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new List<string> { "a", "b" };

        // Act
        var result = _validator.Validate(value, typeof(IEnumerable<string>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DerivedClass_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new DerivedClass();

        // Act
        var result = _validator.Validate(value, typeof(BaseClass), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DerivedClass_StrictMode_ReturnsFailure()
    {
        // Arrange
        var value = new DerivedClass();

        // Act
        var result = _validator.Validate(value, typeof(BaseClass), "testFunction", _strictOptions);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion Assignable Type Match (Non-Strict Mode)

    #region Nullable Type Handling

    [Fact]
    public void Validate_NonNullValueForNullableInt_ReturnsSuccess()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = _validator.Validate(value, typeof(int?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("int?", result.ExpectedType);
    }

    [Fact]
    public void Validate_NullValueForNullableInt_ReturnsSuccess()
    {
        // Arrange
        int? value = null;

        // Act
        var result = _validator.Validate(value, typeof(int?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NonNullValueForNullableLong_ReturnsSuccess()
    {
        // Arrange
        const long value = 123456789L;

        // Act
        var result = _validator.Validate(value, typeof(long?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("long?", result.ExpectedType);
    }

    [Fact]
    public void Validate_NonNullValueForNullableDouble_ReturnsSuccess()
    {
        // Arrange
        const double value = 3.14;

        // Act
        var result = _validator.Validate(value, typeof(double?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("double?", result.ExpectedType);
    }

    [Fact]
    public void Validate_NonNullValueForNullableBool_ReturnsSuccess()
    {
        // Arrange
        const bool value = true;

        // Act
        var result = _validator.Validate(value, typeof(bool?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("bool?", result.ExpectedType);
    }

    #endregion Nullable Type Handling

    #region Collection Type Validation

    [Fact]
    public void Validate_ArrayWithMatchingElementType_ReturnsSuccess()
    {
        // Arrange
        int[] value = [1, 2, 3];

        // Act
        var result = _validator.Validate(value, typeof(int[]), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ListWithMatchingElementType_ReturnsSuccess()
    {
        // Arrange
        var value = new List<string> { "a", "b", "c" };

        // Act
        var result = _validator.Validate(value, typeof(List<string>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ArrayAndList_DifferentCollectionTypes_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new List<int> { 1, 2, 3 };

        // Act - List<int> should be assignable to IEnumerable<int>
        var result = _validator.Validate(value, typeof(IEnumerable<int>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_CollectionWithMismatchedElementType_ReturnsFailure()
    {
        // Arrange
        var value = new List<int> { 1, 2, 3 };

        // Act
        var result = _validator.Validate(value, typeof(List<string>), "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, static e => e.Contains("Collection element type mismatch"));
    }

    [Fact]
    public void Validate_StringIsNotTreatedAsCollection()
    {
        // Arrange
        const string value = "hello";

        // Act - string implements IEnumerable but should not be treated as collection
        var result = _validator.Validate(value, typeof(string), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion Collection Type Validation

    #region Type Mismatch Tests

    [Fact]
    public void Validate_IntExpectedStringReceived_ReturnsFailure()
    {
        // Arrange
        const string value = "hello";

        // Act
        var result = _validator.Validate(value, typeof(int), "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Type mismatch: Expected int, got string", result.Errors);
    }

    [Fact]
    public void Validate_StringExpectedIntReceived_ReturnsFailure()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = _validator.Validate(value, typeof(string), "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Type mismatch: Expected string, got int", result.Errors);
    }

    [Fact]
    public void Validate_DoubleExpectedIntReceived_ReturnsFailure()
    {
        // Arrange - int is not assignable to double without explicit conversion
        const int value = 42;

        // Act
        var result = _validator.Validate(value, typeof(double), "testFunction", _defaultOptions);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion Type Mismatch Tests

    #region ArgumentNullException Tests

    [Fact]
    public void Validate_NullExpectedType_ThrowsArgumentNullException()
    {
        // Arrange
        const string value = "test";

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => _validator.Validate(value, null!, "testFunction", _defaultOptions));
    }

    #endregion ArgumentNullException Tests

    #region Generic Type Name Tests

    [Fact]
    public void Validate_GenericList_ReturnsCorrectTypeName()
    {
        // Arrange
        var value = new List<int> { 1, 2 };

        // Act
        var result = _validator.Validate(value, typeof(List<int>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("List<int>", result.ExpectedType);
        Assert.Equal("List<int>", result.ActualType);
    }

    [Fact]
    public void Validate_GenericDictionary_ReturnsCorrectTypeName()
    {
        // Arrange
        var value = new Dictionary<string, int> { { "key", 1 } };

        // Act
        var result = _validator.Validate(value, typeof(Dictionary<string, int>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Dictionary<string, int>", result.ExpectedType);
    }

    #endregion Generic Type Name Tests

    #region Custom Class Tests

    [Fact]
    public void Validate_CustomClass_ReturnsCorrectTypeName()
    {
        // Arrange
        var value = new TestClass();

        // Act
        var result = _validator.Validate(value, typeof(TestClass), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("TestClass", result.ExpectedType);
    }

    [Fact]
    public void Validate_ImplementsInterface_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new TestImplementation();

        // Act
        var result = _validator.Validate(value, typeof(ITestInterface), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ImplementsInterface_StrictMode_ReturnsFailure()
    {
        // Arrange
        var value = new TestImplementation();

        // Act
        var result = _validator.Validate(value, typeof(ITestInterface), "testFunction", _strictOptions);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion Custom Class Tests

    #region Nullable Underlying Type with Assignable

    [Fact]
    public void Validate_DerivedTypeForNullableBase_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new DerivedValueType();

        // Act - Testing underlying nullable type assignability
        var result = _validator.Validate(value, typeof(DerivedValueType?), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion Nullable Underlying Type with Assignable

    #region Collection Element Type Matching with Inheritance

    [Fact]
    public void Validate_ListOfDerived_ExpectListOfBase_NonStrictMode_ReturnsSuccess()
    {
        // Arrange
        var value = new List<DerivedClass> { new() };

        // Act - In non-strict mode, derived element types should be valid
        var result = _validator.Validate(value, typeof(List<BaseClass>), "testFunction", _defaultOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ListOfDerived_ExpectListOfBase_StrictMode_ReturnsFailure()
    {
        // Arrange
        var value = new List<DerivedClass> { new() };

        // Act
        var result = _validator.Validate(value, typeof(List<BaseClass>), "testFunction", _strictOptions);

        // Assert
        Assert.False(result.IsValid);
    }

    #endregion Collection Element Type Matching with Inheritance

    #region Helper Classes

    private class TestClass;

    private class BaseClass;

    private class DerivedClass : BaseClass;

    private interface ITestInterface;

    private class TestImplementation : ITestInterface;

    private struct DerivedValueType;

    #endregion Helper Classes
}
