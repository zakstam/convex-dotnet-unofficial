using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Shared.Serialization;

/// <summary>
/// JSON converter for long (Int64) that handles both regular JSON numbers and Convex's $integer format.
/// This matches the TypeScript client's behavior where jsonToConvex converts { "$integer": "base64string" } to BigInt.
/// </summary>
public class ConvexInt64JsonConverter : JsonConverter<long>
{
    /// <inheritdoc/>
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle regular JSON number
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Read as double first (all JSON numbers can be read as doubles)
            // Then check if it fits in Int64 range
            var doubleValue = reader.GetDouble();

            // Check if it's within Int64 range
            if (doubleValue is >= long.MinValue and <= long.MaxValue)
            {
                // Check if it's a whole number (not a fractional part)
                return doubleValue == Math.Truncate(doubleValue)
                    ? (long)doubleValue
                    : throw new JsonException($"The JSON value {doubleValue} is not a whole number and cannot be converted to System.Int64");
            }
            throw new JsonException($"The JSON value {doubleValue} is out of bounds for System.Int64 (range: {long.MinValue} to {long.MaxValue})");
        }

        // Handle Convex $integer format: { "$integer": "base64string" }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Read the object properties
            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected property name in $integer object, got {reader.TokenType}");
            }

            var propertyName = reader.GetString();

            if (propertyName != "$integer")
            {
                throw new JsonException($"Expected {{ $integer: string }}, got object with property '{propertyName}'");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Malformed $integer field: expected string value, got {reader.TokenType}");
            }

            var base64String = reader.GetString();
            if (string.IsNullOrEmpty(base64String))
            {
                throw new JsonException("Malformed $integer field: empty base64 string");
            }

            // Decode base64 to bytes
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64String);
            }
            catch (FormatException ex)
            {
                throw new JsonException($"Malformed $integer field: invalid base64 string", ex);
            }

            // Verify exactly 8 bytes (as per Convex spec)
            if (bytes.Length != 8)
            {
                throw new JsonException(
                    $"Malformed $integer field: received {bytes.Length} bytes, expected 8 for $integer");
            }

            // Convert bytes to long using little-endian format (matching TypeScript's DataView.getBigInt64(0, true))
            var value = BinaryPrimitives.ReadInt64LittleEndian(bytes);

            // Read past the end of the object
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException($"Malformed $integer field: expected object with single property, got multiple properties or unexpected token {reader.TokenType}");
            }

            return value;
        }

        // Handle string representation of numbers (fallback)
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return long.TryParse(stringValue, out var parsedValue)
                ? parsedValue
                : throw new JsonException($"Unable to convert string '{stringValue}' to long (Int64)");
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to long (Int64). Expected number, string, or {{ $integer: string }}");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        // For writing, we could serialize as Convex $integer format, but to maintain compatibility
        // with standard JSON, we'll write as a regular number. If Convex-specific serialization
        // is needed, it should be handled by ConvexSerializer.SerializeBigInt.
        writer.WriteNumberValue(value);
}

