using System;
using System.Buffers.Binary;
using System.Text.Json;
using Convex.Client.Infrastructure.Serialization;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexInt64JsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ConvexInt64JsonConverter() }
    };

    #region Read - Regular JSON Numbers

    [Fact]
    public void Read_PositiveInteger_ReturnsLong()
    {
        // Arrange
        const string json = "42";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(42L, result);
    }

    [Fact]
    public void Read_NegativeInteger_ReturnsLong()
    {
        // Arrange
        const string json = "-100";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(-100L, result);
    }

    [Fact]
    public void Read_Zero_ReturnsZero()
    {
        // Arrange
        const string json = "0";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public void Read_LargeNumber_ReturnsLong()
    {
        // Arrange
        const string json = "9007199254740991"; // Max safe integer in JavaScript

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(9007199254740991L, result);
    }

    [Fact]
    public void Read_FractionalNumber_ThrowsJsonException()
    {
        // Arrange
        const string json = "3.14";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("not a whole number", ex.Message);
    }

    #endregion Read - Regular JSON Numbers

    #region Read - Convex $integer Format

    [Fact]
    public void Read_IntegerFormatPositive_ReturnsLong()
    {
        // Arrange - Create base64 for value 42
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, 42L);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(42L, result);
    }

    [Fact]
    public void Read_IntegerFormatNegative_ReturnsLong()
    {
        // Arrange - Create base64 for value -999
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, -999L);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(-999L, result);
    }

    [Fact]
    public void Read_IntegerFormatZero_ReturnsZero()
    {
        // Arrange - Create base64 for value 0
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, 0L);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public void Read_IntegerFormatMaxLong_ReturnsMaxLong()
    {
        // Arrange
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, long.MaxValue);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(long.MaxValue, result);
    }

    [Fact]
    public void Read_IntegerFormatMinLong_ReturnsMinLong()
    {
        // Arrange
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, long.MinValue);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(long.MinValue, result);
    }

    #endregion Read - Convex $integer Format

    #region Read - String Fallback

    [Fact]
    public void Read_StringNumber_ReturnsLong()
    {
        // Arrange
        const string json = "\"12345\"";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(12345L, result);
    }

    [Fact]
    public void Read_NegativeStringNumber_ReturnsLong()
    {
        // Arrange
        const string json = "\"-54321\"";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(-54321L, result);
    }

    [Fact]
    public void Read_InvalidString_ThrowsJsonException()
    {
        // Arrange
        const string json = "\"not-a-number\"";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("Unable to convert string", ex.Message);
    }

    #endregion Read - String Fallback

    #region Read - Error Cases

    [Fact]
    public void Read_WrongPropertyName_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"$wrong\":\"value\"}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("$wrong", ex.Message);
    }

    [Fact]
    public void Read_EmptyBase64_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"$integer\":\"\"}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("empty base64 string", ex.Message);
    }

    [Fact]
    public void Read_InvalidBase64_ThrowsJsonException()
    {
        // Arrange
        const string json = "{\"$integer\":\"not-valid-base64!!!\"}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("invalid base64", ex.Message);
    }

    [Fact]
    public void Read_WrongByteCount_ThrowsJsonException()
    {
        // Arrange - Only 4 bytes instead of 8
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("received 4 bytes", ex.Message);
    }

    [Fact]
    public void Read_MultipleProperties_ThrowsJsonException()
    {
        // Arrange - Object with multiple properties
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, 42L);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\",\"extra\":\"property\"}}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("single property", ex.Message);
    }

    [Fact]
    public void Read_BooleanValue_ThrowsJsonException()
    {
        // Arrange
        const string json = "true";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("Unable to convert", ex.Message);
    }

    [Fact]
    public void Read_NullValue_ThrowsJsonException()
    {
        // Arrange
        const string json = "null";

        // Act & Assert - null for value type
        _ = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
    }

    [Fact]
    public void Read_ArrayValue_ThrowsJsonException()
    {
        // Arrange
        const string json = "[1,2,3]";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("Unable to convert", ex.Message);
    }

    [Fact]
    public void Read_IntegerValueNotString_ThrowsJsonException()
    {
        // Arrange - $integer value is not a string
        const string json = "{\"$integer\":42}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, Options));
        Assert.Contains("expected string value", ex.Message);
    }

    #endregion Read - Error Cases

    #region Write Tests

    [Fact]
    public void Write_PositiveValue_WritesNumber()
    {
        // Arrange
        const long value = 42L;

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("42", json);
    }

    [Fact]
    public void Write_NegativeValue_WritesNumber()
    {
        // Arrange
        const long value = -999L;

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("-999", json);
    }

    [Fact]
    public void Write_Zero_WritesZero()
    {
        // Arrange
        const long value = 0L;

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("0", json);
    }

    [Fact]
    public void Write_MaxLong_WritesNumber()
    {
        // Arrange
        const long value = long.MaxValue;

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("9223372036854775807", json);
    }

    [Fact]
    public void Write_MinLong_WritesNumber()
    {
        // Arrange
        const long value = long.MinValue;

        // Act
        var json = JsonSerializer.Serialize(value, Options);

        // Assert
        Assert.Equal("-9223372036854775808", json);
    }

    #endregion Write Tests

    #region Round-Trip Tests

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(-1L)]
    [InlineData(42L)]
    [InlineData(-12345L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void RoundTrip_WithIntegerFormat_PreservesValue(long originalValue)
    {
        // Arrange - Create $integer format
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, originalValue);
        var base64 = Convert.ToBase64String(bytes);
        var json = $"{{\"$integer\":\"{base64}\"}}";

        // Act
        var result = JsonSerializer.Deserialize<long>(json, Options);

        // Assert
        Assert.Equal(originalValue, result);
    }

    #endregion Round-Trip Tests
}
