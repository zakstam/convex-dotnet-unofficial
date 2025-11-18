using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for ToggleReaction mutation.
/// </summary>
public class ToggleReactionArgs
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("emoji")]
    public string Emoji { get; set; } = "";

    [JsonPropertyName("add")]
    public bool Add { get; set; }
}

