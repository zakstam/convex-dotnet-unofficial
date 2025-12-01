using System.Globalization;
using Convex.Client.Infrastructure.Serialization;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexNumberTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithZero_StoresZero()
    {
        // Arrange & Act
        var number = new ConvexNumber(0);

        // Assert
        Assert.Equal(0, number.Value);
    }

    [Fact]
    public void Constructor_WithPositiveValue_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(42.5);

        // Assert
        Assert.Equal(42.5, number.Value);
    }

    [Fact]
    public void Constructor_WithNegativeValue_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(-123.456);

        // Assert
        Assert.Equal(-123.456, number.Value);
    }

    [Fact]
    public void Constructor_WithMaxDouble_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(double.MaxValue);

        // Assert
        Assert.Equal(double.MaxValue, number.Value);
    }

    [Fact]
    public void Constructor_WithMinDouble_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(double.MinValue);

        // Assert
        Assert.Equal(double.MinValue, number.Value);
    }

    [Fact]
    public void Constructor_WithPositiveInfinity_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(double.PositiveInfinity);

        // Assert
        Assert.Equal(double.PositiveInfinity, number.Value);
    }

    [Fact]
    public void Constructor_WithNegativeInfinity_StoresValue()
    {
        // Arrange & Act
        var number = new ConvexNumber(double.NegativeInfinity);

        // Assert
        Assert.Equal(double.NegativeInfinity, number.Value);
    }

    [Fact]
    public void Constructor_WithNaN_StoresNaN()
    {
        // Arrange & Act
        var number = new ConvexNumber(double.NaN);

        // Assert
        Assert.True(double.IsNaN(number.Value));
    }

    #endregion Constructor Tests

    #region Implicit Conversion From C# Types Tests

    [Fact]
    public void ImplicitConversion_FromInt_Works()
    {
        // Act
        ConvexNumber number = 42;

        // Assert
        Assert.Equal(42.0, number.Value);
    }

    [Fact]
    public void ImplicitConversion_FromNegativeInt_Works()
    {
        // Act
        ConvexNumber number = -100;

        // Assert
        Assert.Equal(-100.0, number.Value);
    }

    [Fact]
    public void ImplicitConversion_FromLong_Works()
    {
        // Act
        ConvexNumber number = 9223372036854775807L;

        // Assert
        Assert.Equal(9223372036854775807d, number.Value);
    }

    [Fact]
    public void ImplicitConversion_FromDouble_Works()
    {
        // Act
        ConvexNumber number = 3.14159;

        // Assert
        Assert.Equal(3.14159, number.Value);
    }

    [Fact]
    public void ImplicitConversion_FromFloat_Works()
    {
        // Act
        ConvexNumber number = 2.5f;

        // Assert
        Assert.Equal(2.5, number.Value, 5);
    }

    [Fact]
    public void ImplicitConversion_FromDecimal_Works()
    {
        // Act
        ConvexNumber number = 123.456m;

        // Assert
        Assert.Equal(123.456, number.Value, 10);
    }

    #endregion Implicit Conversion From C# Types Tests

    #region Implicit Conversion To C# Types Tests

    [Fact]
    public void ImplicitConversion_ToDouble_Works()
    {
        // Arrange
        var number = new ConvexNumber(42.5);

        // Act & Assert
        Assert.Equal(42.5, (double)number);
    }

    [Fact]
    public void ImplicitConversion_ToFloat_Works()
    {
        // Arrange
        var number = new ConvexNumber(2.5);

        // Act & Assert
        Assert.Equal(2.5f, (float)number);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_Works()
    {
        // Arrange
        var number = new ConvexNumber(123.456);

        // Act & Assert
        Assert.Equal(123.456m, (decimal)number, 10);
    }

    #endregion Implicit Conversion To C# Types Tests

    #region Explicit Conversion Tests

    [Fact]
    public void ExplicitConversion_ToInt_Truncates()
    {
        // Arrange
        var number = new ConvexNumber(42.9);

        // Act
        int result = (int)number;

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void ExplicitConversion_ToLong_Truncates()
    {
        // Arrange
        var number = new ConvexNumber(9999999999.7);

        // Act
        long result = (long)number;

        // Assert
        Assert.Equal(9999999999L, result);
    }

    [Fact]
    public void ExplicitConversion_ToShort_Truncates()
    {
        // Arrange
        var number = new ConvexNumber(32000.5);

        // Act
        short result = (short)number;

        // Assert
        Assert.Equal((short)32000, result);
    }

    [Fact]
    public void ExplicitConversion_ToByte_Truncates()
    {
        // Arrange
        var number = new ConvexNumber(200.9);

        // Act
        byte result = (byte)number;

        // Assert
        Assert.Equal((byte)200, result);
    }

    [Fact]
    public void ExplicitConversion_NegativeToInt_Works()
    {
        // Arrange
        var number = new ConvexNumber(-42.9);

        // Act
        int result = (int)number;

        // Assert
        Assert.Equal(-42, result);
    }

    #endregion Explicit Conversion Tests

    #region Safe Conversion Methods Tests

    [Fact]
    public void AsInt32_WithWholeNumber_ReturnsExactValue()
    {
        // Arrange
        var number = new ConvexNumber(42);

        // Act
        var result = number.AsInt32();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void AsInt32_WithDecimal_Truncates()
    {
        // Arrange
        var number = new ConvexNumber(42.99);

        // Act
        var result = number.AsInt32();

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void AsInt64_WithWholeNumber_ReturnsExactValue()
    {
        // Arrange
        var number = new ConvexNumber(9999999999);

        // Act
        var result = number.AsInt64();

        // Assert
        Assert.Equal(9999999999L, result);
    }

    [Fact]
    public void AsDouble_ReturnsValue()
    {
        // Arrange
        var number = new ConvexNumber(3.14159);

        // Act
        var result = number.AsDouble();

        // Assert
        Assert.Equal(3.14159, result);
    }

    [Fact]
    public void AsSingle_ReturnsFloatValue()
    {
        // Arrange
        var number = new ConvexNumber(2.5);

        // Act
        var result = number.AsSingle();

        // Assert
        Assert.Equal(2.5f, result);
    }

    [Fact]
    public void AsDecimal_ReturnsDecimalValue()
    {
        // Arrange
        var number = new ConvexNumber(123.456);

        // Act
        var result = number.AsDecimal();

        // Assert
        Assert.Equal(123.456m, result, 10);
    }

    #endregion Safe Conversion Methods Tests

    #region Equality Tests

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        var number2 = new ConvexNumber(42);

        // Act & Assert
        Assert.True(number1.Equals(number2));
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        var number2 = new ConvexNumber(43);

        // Act & Assert
        Assert.False(number1.Equals(number2));
    }

    [Fact]
    public void Equals_ObjectSameValue_ReturnsTrue()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        object number2 = new ConvexNumber(42);

        // Act & Assert
        Assert.True(number1.Equals(number2));
    }

    [Fact]
    public void Equals_ObjectDifferentType_ReturnsFalse()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        object other = 42;

        // Act & Assert
        Assert.False(number1.Equals(other));
    }

    [Fact]
    public void Equals_ObjectNull_ReturnsFalse()
    {
        // Arrange
        var number1 = new ConvexNumber(42);

        // Act & Assert
        Assert.False(number1.Equals(null));
    }

    [Fact]
    public void GetHashCode_SameValue_ReturnsSameHash()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        var number2 = new ConvexNumber(42);

        // Act & Assert
        Assert.Equal(number1.GetHashCode(), number2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_SameValue_ReturnsTrue()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        var number2 = new ConvexNumber(42);

        // Act & Assert
        Assert.True(number1 == number2);
    }

    [Fact]
    public void OperatorNotEquals_DifferentValue_ReturnsTrue()
    {
        // Arrange
        var number1 = new ConvexNumber(42);
        var number2 = new ConvexNumber(43);

        // Act & Assert
        Assert.True(number1 != number2);
    }

    #endregion Equality Tests

    #region Comparison Tests

    [Fact]
    public void CompareTo_LessThan_ReturnsNegative()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(20);

        // Act
        var result = number1.CompareTo(number2);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareTo_Equal_ReturnsZero()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(10);

        // Act
        var result = number1.CompareTo(number2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CompareTo_GreaterThan_ReturnsPositive()
    {
        // Arrange
        var number1 = new ConvexNumber(20);
        var number2 = new ConvexNumber(10);

        // Act
        var result = number1.CompareTo(number2);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void OperatorLessThan_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(20);

        // Act & Assert
        Assert.True(number1 < number2);
        Assert.False(number2 < number1);
    }

    [Fact]
    public void OperatorLessThanOrEqual_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(10);
        var number3 = new ConvexNumber(20);

        // Act & Assert
        Assert.True(number1 <= number2);
        Assert.True(number1 <= number3);
        Assert.False(number3 <= number1);
    }

    [Fact]
    public void OperatorGreaterThan_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(20);
        var number2 = new ConvexNumber(10);

        // Act & Assert
        Assert.True(number1 > number2);
        Assert.False(number2 > number1);
    }

    [Fact]
    public void OperatorGreaterThanOrEqual_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(20);
        var number2 = new ConvexNumber(20);
        var number3 = new ConvexNumber(10);

        // Act & Assert
        Assert.True(number1 >= number2);
        Assert.True(number1 >= number3);
        Assert.False(number3 >= number1);
    }

    #endregion Comparison Tests

    #region Arithmetic Operator Tests

    [Fact]
    public void OperatorAdd_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(5);

        // Act
        var result = number1 + number2;

        // Assert
        Assert.Equal(15, result.Value);
    }

    [Fact]
    public void OperatorSubtract_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(10);
        var number2 = new ConvexNumber(3);

        // Act
        var result = number1 - number2;

        // Assert
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void OperatorMultiply_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(6);
        var number2 = new ConvexNumber(7);

        // Act
        var result = number1 * number2;

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void OperatorDivide_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(20);
        var number2 = new ConvexNumber(4);

        // Act
        var result = number1 / number2;

        // Assert
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void OperatorDivide_ByZero_ReturnsInfinity()
    {
        // Arrange
        var number1 = new ConvexNumber(20);
        var number2 = new ConvexNumber(0);

        // Act
        var result = number1 / number2;

        // Assert
        Assert.Equal(double.PositiveInfinity, result.Value);
    }

    [Fact]
    public void OperatorModulo_Works()
    {
        // Arrange
        var number1 = new ConvexNumber(17);
        var number2 = new ConvexNumber(5);

        // Act
        var result = number1 % number2;

        // Assert
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void OperatorUnaryMinus_Works()
    {
        // Arrange
        var number = new ConvexNumber(42);

        // Act
        var result = -number;

        // Assert
        Assert.Equal(-42, result.Value);
    }

    [Fact]
    public void OperatorUnaryMinus_NegativeValue_ReturnsPositive()
    {
        // Arrange
        var number = new ConvexNumber(-42);

        // Act
        var result = -number;

        // Assert
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void OperatorIncrement_Works()
    {
        // Arrange
        var number = new ConvexNumber(42);

        // Act
        var result = ++number;

        // Assert
        Assert.Equal(43, result.Value);
    }

    [Fact]
    public void OperatorDecrement_Works()
    {
        // Arrange
        var number = new ConvexNumber(42);

        // Act
        var result = --number;

        // Assert
        Assert.Equal(41, result.Value);
    }

    #endregion Arithmetic Operator Tests

    #region ToString Tests

    [Fact]
    public void ToString_WholeNumber_ReturnsWithoutDecimal()
    {
        // Arrange
        var number = new ConvexNumber(42);

        // Act
        var result = number.ToString();

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void ToString_DecimalNumber_ReturnsWithDecimal()
    {
        // Arrange
        var number = new ConvexNumber(3.14);

        // Act
        var result = number.ToString();

        // Assert
        Assert.Equal("3.14", result);
    }

    [Fact]
    public void ToString_WithFormat_FormatsCorrectly()
    {
        // Arrange
        var number = new ConvexNumber(3.14159);

        // Act
        var result = number.ToString("F2");

        // Assert
        Assert.Equal("3.14", result);
    }

    [Fact]
    public void ToString_WithFormatProvider_UsesProvider()
    {
        // Arrange
        var number = new ConvexNumber(1234.56);
        var germanCulture = new CultureInfo("de-DE");

        // Act
        var result = number.ToString(germanCulture);

        // Assert
        Assert.Equal("1234,56", result);
    }

    [Fact]
    public void ToString_WithFormatAndProvider_UsesBoth()
    {
        // Arrange
        var number = new ConvexNumber(1234.5678);
        var germanCulture = new CultureInfo("de-DE");

        // Act
        var result = number.ToString("F2", germanCulture);

        // Assert
        Assert.Equal("1234,57", result);
    }

    [Fact]
    public void ToString_PositiveInfinity_ReturnsInfinitySymbol()
    {
        // Arrange
        var number = new ConvexNumber(double.PositiveInfinity);

        // Act
        var result = number.ToString();

        // Assert
        Assert.Equal(double.PositiveInfinity.ToString(CultureInfo.InvariantCulture), result);
    }

    [Fact]
    public void ToString_NaN_ReturnsNaN()
    {
        // Arrange
        var number = new ConvexNumber(double.NaN);

        // Act
        var result = number.ToString();

        // Assert
        Assert.Equal("NaN", result);
    }

    #endregion ToString Tests

    #region Edge Case Tests

    [Fact]
    public void Arithmetic_WithSpecialValues_HandlesCorrectly()
    {
        // Arrange
        var positive = new ConvexNumber(double.PositiveInfinity);
        var nan = new ConvexNumber(double.NaN);

        // Act & Assert - Infinity + Infinity = Infinity
        Assert.Equal(double.PositiveInfinity, (positive + positive).Value);

        // Infinity - Infinity = NaN
        Assert.True(double.IsNaN((positive - positive).Value));

        // NaN + anything = NaN
        Assert.True(double.IsNaN((nan + new ConvexNumber(42)).Value));
    }

    [Fact]
    public void DefaultValue_IsZero()
    {
        // Arrange & Act
        ConvexNumber number = default;

        // Assert
        Assert.Equal(0, number.Value);
    }

    [Fact]
    public void VerySmallNumber_PreservesPrecision()
    {
        // Arrange
        var number = new ConvexNumber(1e-300);

        // Act
        var result = number.Value;

        // Assert
        Assert.Equal(1e-300, result);
    }

    [Fact]
    public void VeryLargeNumber_PreservesPrecision()
    {
        // Arrange
        var number = new ConvexNumber(1e300);

        // Act
        var result = number.Value;

        // Assert
        Assert.Equal(1e300, result);
    }

    #endregion Edge Case Tests
}
