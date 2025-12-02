using System.Text.Json;
using System.Text.Json.Serialization;

namespace Convex.Client.Infrastructure.Serialization;

/// <summary>
/// JSON converter for DateTimeOffset that handles Convex's timestamp format (Unix milliseconds as a number).
/// Converts between DateTimeOffset and double (Unix timestamp in milliseconds).
/// </summary>
public class ConvexDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var milliseconds = reader.GetDouble();
            return UnixEpoch.AddMilliseconds(milliseconds);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (double.TryParse(stringValue, out var parsedMilliseconds))
            {
                return UnixEpoch.AddMilliseconds(parsedMilliseconds);
            }

            // Try parsing as ISO 8601 date string
            if (DateTimeOffset.TryParse(stringValue, out var parsedDate))
            {
                return parsedDate;
            }

            throw new JsonException($"Unable to convert string '{stringValue}' to DateTimeOffset");
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to DateTimeOffset. Expected number (Unix milliseconds) or string.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var milliseconds = (value - UnixEpoch).TotalMilliseconds;
        writer.WriteNumberValue(milliseconds);
    }
}

/// <summary>
/// JSON converter for nullable DateTimeOffset that handles Convex's timestamp format (Unix milliseconds as a number).
/// Converts between DateTimeOffset? and double? (Unix timestamp in milliseconds).
/// </summary>
public class ConvexNullableDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset?>
{
    private static readonly ConvexDateTimeOffsetJsonConverter InnerConverter = new();

    /// <inheritdoc/>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return InnerConverter.Read(ref reader, typeof(DateTimeOffset), options);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            InnerConverter.Write(writer, value.Value, options);
        }
    }
}
