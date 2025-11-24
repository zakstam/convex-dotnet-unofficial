using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents a user in the cursor playground.
/// </summary>
public class User
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "👤";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#64C8FF";

    [JsonPropertyName("lastSeen")]
    public double LastSeen { get; set; }
}
