using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for GetReactions query.
/// </summary>
public class GetReactionsArgs
{
    [JsonPropertyName("messageIds")]
    public List<string> MessageIds { get; set; } = [];
}

