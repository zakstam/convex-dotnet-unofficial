using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents an emoji reaction dropped at a cursor position.
/// </summary>
public class Reaction
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "⭐";

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("timestamp")]
    public double Timestamp { get; set; }
}
