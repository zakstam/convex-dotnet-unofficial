using System.Text.Json.Serialization;

namespace RealtimeChat.Shared.Models;

/// <summary>
/// Data Transfer Object for online user information.
/// </summary>
public class OnlineUserDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("lastSeen")]
    public long LastSeen { get; set; }
}

