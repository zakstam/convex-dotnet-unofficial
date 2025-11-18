using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Data Transfer Object for a message read receipt.
/// </summary>
public class MessageReadDto
{
    /// <summary>
    /// Username of the user who read the message.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    /// <summary>
    /// Timestamp when the message was read (milliseconds since epoch).
    /// </summary>
    [JsonPropertyName("readAt")]
    public long ReadAt { get; set; }
}

