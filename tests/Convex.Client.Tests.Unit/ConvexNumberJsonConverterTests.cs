using System.Text.Json;
using Convex.Client.Infrastructure.Serialization;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexNumberJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ConvexNumberJsonConverter() }
    };

    #region Read - JSON Numbers

    [Fact]
    public void Read_PositiveInteger_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "42";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(42.0, result.Value);
    }

    [Fact]
    public void Read_NegativeInteger_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "-100";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(-100.0, result.Value);
    }

    [Fact]
    public void Read_Zero_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "0";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(0.0, result.Value);
    }

    [Fact]
    public void Read_DecimalNumber_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "3.14159";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(3.14159, result.Value, 10);
    }

    [Fact]
    public void Read_ScientificNotation_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "1.5e10";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(1.5e10, result.Value);
    }

    [Fact]
    public void Read_VerySmallNumber_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "1e-10";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(1e-10, result.Value);
    }

    [Fact]
    public void Read_LargeNumber_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "9007199254740991";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(9007199254740991.0, result.Value);
    }

    #endregion Read - JSON Numbers

    #region Read - String Numbers

    [Fact]
    public void Read_StringInteger_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "\"42\"";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(42.0, result.Value);
    }

    [Fact]
    public void Read_StringNegative_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "\"-100\"";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(-100.0, result.Value);
    }

    [Fact]
    public void Read_StringDecimal_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "\"3.14\"";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(3.14, result.Value);
    }

    [Fact]
    public void Read_StringScientific_ReturnsConvexNumber()
    {
        // Arrange
        const string json = "\"1e5\"";

        // Act
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(100000.0, result.Value);
    }

    #endregion Read - String Numbers

    #region Read - Error Cases

    [Fact]
    public void Read_InvalidString_ThrowsJsonException()
    {
        // Arrange
        const string json = "\"not-a-number\"";

        // Act & Assert
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConvexNumber>(json, Options));
    }

    [Fact]
    public void Read_Boolean_ThrowsJsonException()
    {
        // Arrange
        const string json = "true";

        // Act & Assert
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConvexNumber>(json, Options));
    }

    [Fact]
    public void Read_Object_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"value\":42}";

        // Act & Assert
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConvexNumber>(json, Options));
    }

    [Fact]
    public void Read_Array_ThrowsJsonException()
    {
        // Arrange
        const string json = "[1,2,3]";

        // Act & Assert
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConvexNumber>(json, Options));
    }

    #endregion Read - Error Cases

    #region Write Tests

    [Fact]
    public void Write_PositiveNumber_WritesNumber()
    {
        // Arrange
        var value = new ConvexNumber(42);

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("42", json);
    }

    [Fact]
    public void Write_NegativeNumber_WritesNumber()
    {
        // Arrange
        var value = new ConvexNumber(-999);

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("-999", json);
    }

    [Fact]
    public void Write_DecimalNumber_WritesNumber()
    {
        // Arrange
        var value = new ConvexNumber(3.14);

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("3.14", json);
    }

    [Fact]
    public void Write_Zero_WritesZero()
    {
        // Arrange
        var value = new ConvexNumber(0);

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("0", json);
    }

    [Fact]
    public void Write_VerySmallNumber_WritesScientific()
    {
        // Arrange
        var value = new ConvexNumber(0.00001);

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("1E-05", json);
    }

    #endregion Write Tests

    #region Round-Trip Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(42)]
    [InlineData(-12345)]
    [InlineData(3.14159)]
    [InlineData(-2.71828)]
    public void RoundTrip_PreservesValue(double originalValue)
    {
        // Arrange
        var original = new ConvexNumber(originalValue);

        // Act
        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<ConvexNumber>(json, Options);

        // Assert
        Assert.Equal(originalValue, result.Value, 10);
    }

    #endregion Round-Trip Tests

    #region Integration with Object

    private class ObjectWithConvexNumber
    {
        public ConvexNumber Value { get; set; }
    }

    [Fact]
    public void Deserialize_ObjectWithConvexNumber_Works()
    {
        // Arrange
        const string json = "{\"Value\":42.5}";

        // Act
        var result = JsonSerializer.Deserialize<ObjectWithConvexNumber>(json, Options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42.5, result.Value.Value);
    }

    [Fact]
    public void Serialize_ObjectWithConvexNumber_Works()
    {
        // Arrange
        var obj = new ObjectWithConvexNumber { Value = new ConvexNumber(99.9) };

        // Act
        var json = JsonSerializer.Serialize(obj, Options);

        // Assert
        Assert.Contains("99.9", json);
    }

    #endregion Integration with Object
}
