using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for SendReply mutation.
/// </summary>
public class SendReplyArgs
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("parentMessageId")]
    public string ParentMessageId { get; set; } = "";

    [JsonPropertyName("attachments")]
    public List<AttachmentDto>? Attachments { get; set; }
}

