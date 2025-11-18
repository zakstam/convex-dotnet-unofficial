using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for GetReplies query.
/// </summary>
public class GetRepliesArgs
{
    [JsonPropertyName("parentMessageId")]
    public string ParentMessageId { get; set; } = "";
}

