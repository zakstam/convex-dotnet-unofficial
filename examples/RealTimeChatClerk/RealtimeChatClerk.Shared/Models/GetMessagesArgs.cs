using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for the getMessages function.
/// </summary>
public class GetMessagesArgs
{
    /// <summary>
    /// Maximum number of messages to return.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 50;
}

