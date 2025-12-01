using System;
using System.Collections.Generic;
using Convex.Client.Infrastructure.Serialization;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConvexSerializerTests
{
    #region Test Types

    private class SimpleObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private class NestedObject
    {
        public string? Title { get; set; }
        public SimpleObject? Child { get; set; }
    }

    private class ObjectWithNullable
    {
        public string? Name { get; set; }
        public int? OptionalValue { get; set; }
    }

    private class CyclicObject
    {
        public string? Name { get; set; }
        public CyclicObject? Self { get; set; }
    }

    private enum TestEnum
    {
        FirstValue = 0,
        SecondValue = 1,
        ThirdValue = 2
    }

    private class ObjectWithEnum
    {
        public TestEnum Status { get; set; }
    }

    private class ObjectWithDateTime
    {
        public DateTime Timestamp { get; set; }
    }

    private class ObjectWithBytes
    {
        public byte[]? Data { get; set; }
    }

    #endregion Test Types

    #region Null Serialization Tests

    [Fact]
    public void SerializeToConvexJson_Null_ReturnsNullString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson<object>(null);

        // Assert
        Assert.Equal("null", result);
    }

    [Fact]
    public void SerializeToConvexJson_NullableIntNull_ReturnsNullString()
    {
        // Arrange
        int? value = null;

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(value);

        // Assert
        Assert.Equal("null", result);
    }

    #endregion Null Serialization Tests

    #region Boolean Serialization Tests

    [Fact]
    public void SerializeToConvexJson_True_ReturnsTrueString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(true);

        // Assert
        Assert.Equal("true", result);
    }

    [Fact]
    public void SerializeToConvexJson_False_ReturnsFalseString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(false);

        // Assert
        Assert.Equal("false", result);
    }

    #endregion Boolean Serialization Tests

    #region Integer Serialization Tests

    [Fact]
    public void SerializeToConvexJson_PositiveInt_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(42);

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeInt_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(-100);

        // Assert
        Assert.Equal("-100", result);
    }

    [Fact]
    public void SerializeToConvexJson_Zero_ReturnsZero()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(0);

        // Assert
        Assert.Equal("0", result);
    }

    [Fact]
    public void SerializeToConvexJson_MaxInt_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(int.MaxValue);

        // Assert
        Assert.Equal("2147483647", result);
    }

    [Fact]
    public void SerializeToConvexJson_MinInt_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(int.MinValue);

        // Assert
        Assert.Equal("-2147483648", result);
    }

    #endregion Integer Serialization Tests

    #region Long (BigInt) Serialization Tests

    [Fact]
    public void SerializeToConvexJson_PositiveLong_ReturnsIntegerFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(123456789L);

        // Assert
        Assert.Contains("\"$integer\":", result);
        Assert.StartsWith("{", result);
        Assert.EndsWith("}", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeLong_ReturnsIntegerFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(-987654321L);

        // Assert
        Assert.Contains("\"$integer\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_ZeroLong_ReturnsIntegerFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(0L);

        // Assert - Zero as long should still use $integer format
        Assert.Contains("\"$integer\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_MaxLong_ReturnsIntegerFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(long.MaxValue);

        // Assert
        Assert.Contains("\"$integer\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_MinLong_ReturnsIntegerFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(long.MinValue);

        // Assert
        Assert.Contains("\"$integer\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_LongValue_HasBase64Value()
    {
        // Arrange
        const long value = 1L;

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(value);

        // Assert - Should contain base64 encoded bytes
        Assert.Contains("\"$integer\":\"", result);
        Assert.Matches("[A-Za-z0-9+/=]+", result);
    }

    #endregion Long (BigInt) Serialization Tests

    #region Double Serialization Tests

    [Fact]
    public void SerializeToConvexJson_PositiveDouble_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(3.14159);

        // Assert
        Assert.Equal("3.14159", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeDouble_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(-2.71828);

        // Assert
        Assert.Equal("-2.71828", result);
    }

    [Fact]
    public void SerializeToConvexJson_DoubleWithScientificNotation_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(1.5e10);

        // Assert
        Assert.Equal("15000000000", result);
    }

    [Fact]
    public void SerializeToConvexJson_VerySmallDouble_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(0.00001);

        // Assert
        Assert.Equal("1E-05", result);
    }

    #endregion Double Serialization Tests

    #region Special Float Serialization Tests

    [Fact]
    public void SerializeToConvexJson_NaN_ReturnsFloatFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(double.NaN);

        // Assert
        Assert.Contains("\"$float\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_PositiveInfinity_ReturnsFloatFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(double.PositiveInfinity);

        // Assert
        Assert.Contains("\"$float\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeInfinity_ReturnsFloatFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(double.NegativeInfinity);

        // Assert
        Assert.Contains("\"$float\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeZero_ReturnsFloatFormat()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(-0.0);

        // Assert
        Assert.Contains("\"$float\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_SpecialFloat_HasBase64Value()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(double.NaN);

        // Assert - Should contain base64 encoded bytes
        Assert.Contains("\"$float\":\"", result);
        Assert.Matches("[A-Za-z0-9+/=]+", result);
    }

    #endregion Special Float Serialization Tests

    #region Float Serialization Tests

    [Fact]
    public void SerializeToConvexJson_PositiveFloat_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(3.14f);

        // Assert
        Assert.Contains("3.14", result);
    }

    [Fact]
    public void SerializeToConvexJson_NegativeFloat_ReturnsNumber()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(-2.5f);

        // Assert
        Assert.Equal("-2.5", result);
    }

    #endregion Float Serialization Tests

    #region String Serialization Tests

    [Fact]
    public void SerializeToConvexJson_SimpleString_ReturnsQuotedString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("hello");

        // Assert
        Assert.Equal("\"hello\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_EmptyString_ReturnsEmptyQuotedString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("");

        // Assert
        Assert.Equal("\"\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithQuotes_EscapesQuotes()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("say \"hello\"");

        // Assert
        Assert.Equal("\"say \\\"hello\\\"\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithBackslash_EscapesBackslash()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("path\\to\\file");

        // Assert
        Assert.Equal("\"path\\\\to\\\\file\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithNewline_EscapesNewline()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("line1\nline2");

        // Assert
        Assert.Equal("\"line1\\nline2\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithTab_EscapesTab()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("col1\tcol2");

        // Assert
        Assert.Equal("\"col1\\tcol2\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithCarriageReturn_EscapesCarriageReturn()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("line1\rline2");

        // Assert
        Assert.Equal("\"line1\\rline2\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithFormFeed_EscapesFormFeed()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("page1\fpage2");

        // Assert
        Assert.Equal("\"page1\\fpage2\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithBackspace_EscapesBackspace()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("text\bmore");

        // Assert
        Assert.Equal("\"text\\bmore\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithControlChar_EscapesAsUnicode()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("test\u0001char");

        // Assert
        Assert.Equal("\"test\\u0001char\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithUnicode_PreservesUnicode()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("日本語");

        // Assert - Unicode should be preserved, not escaped
        Assert.Equal("\"日本語\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringWithEmoji_PreservesEmoji()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson("Hello 👋 World 🌍");

        // Assert - Emoji should be preserved
        Assert.Equal("\"Hello 👋 World 🌍\"", result);
    }

    #endregion String Serialization Tests

    #region Byte Array Serialization Tests

    [Fact]
    public void SerializeToConvexJson_ByteArray_ReturnsBytesFormat()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(bytes);

        // Assert
        Assert.Contains("\"$bytes\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_EmptyByteArray_ReturnsBytesFormat()
    {
        // Arrange
        var bytes = Array.Empty<byte>();

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(bytes);

        // Assert
        Assert.Contains("\"$bytes\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_ByteArray_HasBase64Value()
    {
        // Arrange
        var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(bytes);

        // Assert - "Hello" in base64 is "SGVsbG8="
        Assert.Contains("SGVsbG8=", result);
    }

    #endregion Byte Array Serialization Tests

    #region DateTime Serialization Tests

    [Fact]
    public void SerializeToConvexJson_DateTime_ReturnsUnixMilliseconds()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var expectedMs = new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(dateTime);

        // Assert
        Assert.Equal(expectedMs.ToString(), result);
    }

    [Fact]
    public void SerializeToConvexJson_UnixEpoch_ReturnsZero()
    {
        // Arrange
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(epoch);

        // Assert
        Assert.Equal("0", result);
    }

    #endregion DateTime Serialization Tests

    #region Enum Serialization Tests

    [Fact]
    public void SerializeToConvexJson_Enum_ReturnsEnumNameAsString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(TestEnum.FirstValue);

        // Assert
        Assert.Equal("\"FirstValue\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_EnumSecondValue_ReturnsEnumNameAsString()
    {
        // Act
        var result = ConvexSerializer.SerializeToConvexJson(TestEnum.SecondValue);

        // Assert
        Assert.Equal("\"SecondValue\"", result);
    }

    #endregion Enum Serialization Tests

    #region Array Serialization Tests

    [Fact]
    public void SerializeToConvexJson_IntArray_ReturnsJsonArray()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(array);

        // Assert
        Assert.Equal("[1,2,3]", result);
    }

    [Fact]
    public void SerializeToConvexJson_EmptyArray_ReturnsEmptyJsonArray()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(array);

        // Assert
        Assert.Equal("[]", result);
    }

    [Fact]
    public void SerializeToConvexJson_StringArray_ReturnsJsonArray()
    {
        // Arrange
        var array = new[] { "a", "b", "c" };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(array);

        // Assert
        Assert.Equal("[\"a\",\"b\",\"c\"]", result);
    }

    [Fact]
    public void SerializeToConvexJson_MixedList_ReturnsJsonArray()
    {
        // Arrange
        var list = new List<object?> { 1, "two", true, null };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(list);

        // Assert
        Assert.Equal("[1,\"two\",true,null]", result);
    }

    [Fact]
    public void SerializeToConvexJson_NestedArray_ReturnsNestedJsonArray()
    {
        // Arrange
        int[][] array = [[1, 2], [3, 4]];

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(array);

        // Assert
        Assert.Equal("[[1,2],[3,4]]", result);
    }

    #endregion Array Serialization Tests

    #region Dictionary Serialization Tests

    [Fact]
    public void SerializeToConvexJson_Dictionary_ReturnsJsonObject()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "a", 1 },
            { "b", 2 }
        };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(dict);

        // Assert - Keys are sorted alphabetically
        Assert.Equal("{\"a\":1,\"b\":2}", result);
    }

    [Fact]
    public void SerializeToConvexJson_EmptyDictionary_ReturnsEmptyJsonObject()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(dict);

        // Assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public void SerializeToConvexJson_DictionaryWithStringValues_ReturnsJsonObject()
    {
        // Arrange
        var dict = new Dictionary<string, string>
        {
            { "name", "John" },
            { "city", "NYC" }
        };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(dict);

        // Assert - Keys are sorted alphabetically
        Assert.Equal("{\"city\":\"NYC\",\"name\":\"John\"}", result);
    }

    [Fact]
    public void SerializeToConvexJson_DictionaryKeysSorted_ReturnsAlphabeticallySorted()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "zebra", 1 },
            { "apple", 2 },
            { "mango", 3 }
        };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(dict);

        // Assert - Keys should be sorted: apple, mango, zebra
        Assert.Equal("{\"apple\":2,\"mango\":3,\"zebra\":1}", result);
    }

    #endregion Dictionary Serialization Tests

    #region Object Serialization Tests

    [Fact]
    public void SerializeToConvexJson_SimpleObject_ReturnsJsonObjectWithCamelCase()
    {
        // Arrange
        var obj = new SimpleObject { Name = "Test", Value = 42 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert - Properties should be camelCase and sorted
        Assert.Contains("\"name\":\"Test\"", result);
        Assert.Contains("\"value\":42", result);
    }

    [Fact]
    public void SerializeToConvexJson_NestedObject_ReturnsNestedJsonObject()
    {
        // Arrange
        var obj = new NestedObject
        {
            Title = "Parent",
            Child = new SimpleObject { Name = "Child", Value = 10 }
        };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"title\":\"Parent\"", result);
        Assert.Contains("\"child\":", result);
        Assert.Contains("\"name\":\"Child\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_ObjectWithNullProperty_OmitsNullProperty()
    {
        // Arrange
        var obj = new ObjectWithNullable { Name = "Test", OptionalValue = null };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert - Null properties should be omitted
        Assert.Contains("\"name\":\"Test\"", result);
        Assert.DoesNotContain("optionalValue", result);
    }

    [Fact]
    public void SerializeToConvexJson_ObjectWithNonNullOptional_IncludesProperty()
    {
        // Arrange
        var obj = new ObjectWithNullable { Name = "Test", OptionalValue = 42 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"optionalValue\":42", result);
    }

    [Fact]
    public void SerializeToConvexJson_ObjectPropertyNames_SortedAlphabetically()
    {
        // Arrange
        var obj = new SimpleObject { Name = "Test", Value = 42 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert - "name" should come before "value" (alphabetically sorted)
        var nameIndex = result.IndexOf("\"name\"");
        var valueIndex = result.IndexOf("\"value\"");
        Assert.True(nameIndex < valueIndex, "Properties should be sorted alphabetically");
    }

    #endregion Object Serialization Tests

    #region Cycle Detection Tests

    [Fact]
    public void SerializeToConvexJson_CyclicReference_ReturnsNullForCycle()
    {
        // Arrange
        var obj = new CyclicObject { Name = "Root" };
        obj.Self = obj; // Create cycle

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert - The cyclic reference should be serialized as null
        Assert.Contains("\"name\":\"Root\"", result);
        Assert.Contains("\"self\":null", result);
    }

    [Fact]
    public void SerializeToConvexJson_DeepCyclicReference_HandlesGracefully()
    {
        // Arrange
        var root = new CyclicObject { Name = "Root" };
        var child = new CyclicObject { Name = "Child" };
        root.Self = child;
        child.Self = root; // Create cycle back to root

        // Act - Should not throw or infinite loop
        var result = ConvexSerializer.SerializeToConvexJson(root);

        // Assert
        Assert.Contains("\"name\":\"Root\"", result);
        Assert.Contains("\"name\":\"Child\"", result);
    }

    #endregion Cycle Detection Tests

    #region Edge Cases

    [Fact]
    public void SerializeToConvexJson_ObjectWithEnum_SerializesEnumAsString()
    {
        // Arrange
        var obj = new ObjectWithEnum { Status = TestEnum.SecondValue };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"status\":\"SecondValue\"", result);
    }

    [Fact]
    public void SerializeToConvexJson_ObjectWithDateTime_SerializesAsMilliseconds()
    {
        // Arrange
        var obj = new ObjectWithDateTime
        {
            Timestamp = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"timestamp\":", result);
        // Should be unix milliseconds, not a string
        Assert.DoesNotContain("2024", result);
    }

    [Fact]
    public void SerializeToConvexJson_ObjectWithBytes_SerializesAsBase64()
    {
        // Arrange
        var obj = new ObjectWithBytes { Data = [0x01, 0x02, 0x03] };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"data\":", result);
        Assert.Contains("\"$bytes\":", result);
    }

    [Fact]
    public void SerializeToConvexJson_AnonymousObject_SerializesProperties()
    {
        // Arrange
        var obj = new { Name = "Anonymous", Value = 123 };

        // Act
        var result = ConvexSerializer.SerializeToConvexJson(obj);

        // Assert
        Assert.Contains("\"name\":\"Anonymous\"", result);
        Assert.Contains("\"value\":123", result);
    }

    #endregion Edge Cases
}
