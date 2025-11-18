using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for sending a message via the sendMessage mutation.
/// Note: Username is now extracted from the authenticated user's token on the backend.
/// </summary>
public class SendMessageArgs
{
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

