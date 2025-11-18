using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for SetTyping mutation.
/// </summary>
public class SetTypingArgs
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("isTyping")]
    public bool IsTyping { get; set; }
}

