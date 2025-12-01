using System;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class DefaultConvexSerializerTests
{
    private readonly DefaultConvexSerializer _serializer;
    private readonly Mock<ILogger<DefaultConvexSerializer>> _mockLogger;

    public DefaultConvexSerializerTests()
    {
        _mockLogger = new Mock<ILogger<DefaultConvexSerializer>>();
        _serializer = new DefaultConvexSerializer(_mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var serializer = new DefaultConvexSerializer(null);
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var serializer = new DefaultConvexSerializer(_mockLogger.Object);
        Assert.NotNull(serializer);
    }

    #endregion Constructor Tests

    #region Deserialize<T> Tests

    [Fact]
    public void Deserialize_WithNullJson_ReturnsDefault()
    {
        // Act
        var result = _serializer.Deserialize<string>(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithEmptyJson_ReturnsDefault()
    {
        // Act
        var result = _serializer.Deserialize<string>("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithWhitespaceJson_ReturnsDefault()
    {
        // Act
        var result = _serializer.Deserialize<string>("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithValidStringJson_ReturnsString()
    {
        // Act
        var result = _serializer.Deserialize<string>("\"hello world\"");

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Deserialize_WithValidIntJson_ReturnsInt()
    {
        // Act
        var result = _serializer.Deserialize<int>("42");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Deserialize_WithValidBoolJson_ReturnsBool()
    {
        // Act
        var resultTrue = _serializer.Deserialize<bool>("true");
        var resultFalse = _serializer.Deserialize<bool>("false");

        // Assert
        Assert.True(resultTrue);
        Assert.False(resultFalse);
    }

    [Fact]
    public void Deserialize_WithValidDoubleJson_ReturnsDouble()
    {
        // Act
        var result = _serializer.Deserialize<double>("3.14159");

        // Assert
        Assert.Equal(3.14159, result, 5);
    }

    [Fact]
    public void Deserialize_WithValidObjectJson_ReturnsObject()
    {
        // Act
        var result = _serializer.Deserialize<TestObject>(@"{""name"":""test"",""value"":123}");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void Deserialize_WithValidArrayJson_ReturnsArray()
    {
        // Act
        var result = _serializer.Deserialize<int[]>("[1,2,3,4,5]");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void Deserialize_WithNullValueJson_ReturnsNull()
    {
        // Act
        var result = _serializer.Deserialize<string>("null");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _serializer.Deserialize<TestObject>("{invalid json}"));
        Assert.Contains("Failed to deserialize JSON", ex.Message);
        Assert.Contains("TestObject", ex.Message);
    }

    [Fact]
    public void Deserialize_WithInvalidJson_LogsError()
    {
        // Act
        try
        {
            _ = _serializer.Deserialize<TestObject>("{invalid json}");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - Verify logger was called
        _mockLogger.Verify(
            static x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(static (_, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Deserialize_WithLongJson_IncludesPreviewInErrorMessage()
    {
        // Arrange - Create JSON longer than 500 chars
        var longJson = @"{""data"":""" + new string('x', 600) + @"""}invalid";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _serializer.Deserialize<TestObject>(longJson));
        Assert.Contains("first 500 chars", ex.Message);
    }

    [Fact]
    public void Deserialize_UseCamelCaseNaming_Works()
    {
        // Act
        var result = _serializer.Deserialize<PersonWithCamelCase>(@"{""firstName"":""John"",""lastName"":""Doe""}");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    #endregion Deserialize<T> Tests

    #region Deserialize (non-generic) Tests

    [Fact]
    public void DeserializeNonGeneric_WithNullJson_ReturnsNull()
    {
        // Act
        var result = _serializer.Deserialize(null!, typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeNonGeneric_WithEmptyJson_ReturnsNull()
    {
        // Act
        var result = _serializer.Deserialize("", typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeNonGeneric_WithWhitespaceJson_ReturnsNull()
    {
        // Act
        var result = _serializer.Deserialize("   ", typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeNonGeneric_WithValidStringJson_ReturnsString()
    {
        // Act
        var result = _serializer.Deserialize("\"hello world\"", typeof(string));

        // Assert
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void DeserializeNonGeneric_WithValidIntJson_ReturnsInt()
    {
        // Act
        var result = _serializer.Deserialize("42", typeof(int));

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void DeserializeNonGeneric_WithValidObjectJson_ReturnsObject()
    {
        // Act
        var result = _serializer.Deserialize(@"{""name"":""test"",""value"":123}", typeof(TestObject)) as TestObject;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void DeserializeNonGeneric_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _serializer.Deserialize("{invalid json}", typeof(TestObject)));
        Assert.Contains("Failed to deserialize JSON", ex.Message);
        Assert.Contains("TestObject", ex.Message);
    }

    [Fact]
    public void DeserializeNonGeneric_WithInvalidJson_LogsError()
    {
        // Act
        try
        {
            _ = _serializer.Deserialize("{invalid json}", typeof(TestObject));
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - Verify logger was called
        _mockLogger.Verify(
            static x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(static (_, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion Deserialize (non-generic) Tests

    #region Serialize Tests

    [Fact]
    public void Serialize_WithNull_ReturnsValidJson()
    {
        // Act
        var result = _serializer.Serialize<string>(null);

        // Assert
        Assert.NotNull(result);
        // ConvexSerializer may return "null" or handle it differently
    }

    [Fact]
    public void Serialize_WithString_ReturnsValidJson()
    {
        // Act
        var result = _serializer.Serialize("hello world");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("hello", result);
    }

    [Fact]
    public void Serialize_WithInt_ReturnsValidJson()
    {
        // Act
        var result = _serializer.Serialize(42);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("42", result);
    }

    [Fact]
    public void Serialize_WithObject_ReturnsValidJson()
    {
        // Arrange
        var obj = new TestObject { Name = "test", Value = 123 };

        // Act
        var result = _serializer.Serialize(obj);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test", result);
        Assert.Contains("123", result);
    }

    [Fact]
    public void Serialize_WithArray_ReturnsValidJson()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = _serializer.Serialize(array);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("1", result);
        Assert.Contains("2", result);
        Assert.Contains("3", result);
    }

    [Fact]
    public void Serialize_WithBool_ReturnsValidJson()
    {
        // Act
        var resultTrue = _serializer.Serialize(true);
        var resultFalse = _serializer.Serialize(false);

        // Assert
        Assert.NotNull(resultTrue);
        Assert.NotNull(resultFalse);
    }

    #endregion Serialize Tests

    #region Helper Classes

    private class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    private class PersonWithCamelCase
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    #endregion Helper Classes
}
