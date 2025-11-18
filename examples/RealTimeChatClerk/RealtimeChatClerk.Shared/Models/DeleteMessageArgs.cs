using System.Text.Json.Serialization;

namespace RealtimeChatClerk.Shared.Models;

/// <summary>
/// Arguments for DeleteMessage mutation.
/// </summary>
public class DeleteMessageArgs
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

