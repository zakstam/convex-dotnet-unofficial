using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for GetReplies query.
/// </summary>
public class GetRepliesArgs
{
    [JsonPropertyName("parentMessageId")]
    public string ParentMessageId { get; set; } = "";
}

