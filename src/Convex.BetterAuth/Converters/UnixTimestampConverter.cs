using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.BetterAuth.Converters;

/// <summary>
/// Converts Unix timestamps (numbers) to DateTime.
/// Better Auth returns expiresAt as a Unix timestamp in milliseconds.
/// </summary>
internal class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Better Auth returns Unix timestamp in milliseconds
            var timestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (DateTime.TryParse(str, out var result))
            {
                return result;
            }
        }

        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var timestamp = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        writer.WriteNumberValue(timestamp);
    }
}
