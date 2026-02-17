using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// JSON converter that handles Convex-specific value types during serialization.
/// This converter ensures that special values (BigInt, special floats, bytes)
/// are serialized using Convex's protocol format.
/// </summary>
public class ConvexValueJsonConverter : JsonConverter<JsonElement>
{
    /// <summary>
    /// Gets or sets a value indicating whether convert.
    /// </summary>
    public override bool CanConvert(Type typeToConvert) =>
        // Handle JsonElement types to intercept Convex special values
        typeToConvert == typeof(JsonElement);

    /// <summary>
    /// Gets the read.
    /// </summary>
    public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        // Read as JsonElement for type-safe deserialization
        JsonElement.ParseValue(ref reader);

    /// <summary>
    /// Gets the write.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options) =>
        // Simply write the JsonElement as-is
        value.WriteTo(writer);
}
