using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Data Transfer Object for a message reaction.
/// </summary>
public class ReactionDto
{
    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = [];
}

