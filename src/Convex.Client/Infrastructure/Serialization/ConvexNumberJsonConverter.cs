using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// JSON converter for ConvexNumber that handles serialization to/from Convex's number format.
/// Convex represents all numbers as IEEE 754 doubles (same as JavaScript numbers).
/// </summary>
public class ConvexNumberJsonConverter : JsonConverter<ConvexNumber>
{
    public override ConvexNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => new ConvexNumber(reader.GetDouble()),
            JsonTokenType.String when double.TryParse(reader.GetString(), out var value) => new ConvexNumber(value),
            _ => throw new JsonException($"Unable to convert {reader.TokenType} to ConvexNumber.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ConvexNumber value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
}
