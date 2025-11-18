using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for MarkMessageRead mutation.
/// </summary>
public class MarkMessageReadArgs
{
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = "";

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}

