using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for sending a message via the sendMessage mutation.
/// </summary>
public class SendMessageArgs
{
    /// <summary>
    /// Username of the sender.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Message text content.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional: ID of parent message (for replies).
    /// </summary>
    [JsonPropertyName("parentMessageId")]
    public string? ParentMessageId { get; set; }

    /// <summary>
    /// Optional: File attachments.
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<AttachmentDto>? Attachments { get; set; }
}

