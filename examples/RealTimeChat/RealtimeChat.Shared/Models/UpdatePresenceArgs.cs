using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Arguments for UpdatePresence mutation.
/// </summary>
public class UpdatePresenceArgs
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";
}

