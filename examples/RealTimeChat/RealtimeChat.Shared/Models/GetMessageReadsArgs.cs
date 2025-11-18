using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for GetMessageReads query.
/// </summary>
public class GetMessageReadsArgs
{
    [JsonPropertyName("messageIds")]
    public List<string> MessageIds { get; set; } = [];
}

