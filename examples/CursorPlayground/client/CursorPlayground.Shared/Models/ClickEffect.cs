using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents a click effect (particle burst) created by a user.
/// </summary>
public class ClickEffect
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public double Timestamp { get; set; }
}
