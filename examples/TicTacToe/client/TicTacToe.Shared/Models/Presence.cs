using System.Text.Json.Serialization;

namespace TicTacToe.Shared.Models;

/// <summary>
/// Represents player presence/online status.
/// Maps directly from Convex backend response.
/// </summary>
public class Presence
{
    /// <summary>
    /// Unique presence ID (assigned by Convex).
    /// </summary>
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username of the player.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Player status: "online", "in_game", or "offline".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "offline";

    /// <summary>
    /// Last seen timestamp (milliseconds since epoch).
    /// </summary>
    [JsonPropertyName("lastSeen")]
    public long LastSeen { get; set; }

    /// <summary>
    /// ID of current game the player is in, null if not in a game.
    /// </summary>
    [JsonPropertyName("currentGameId")]
    public string? CurrentGameId { get; set; }
}

