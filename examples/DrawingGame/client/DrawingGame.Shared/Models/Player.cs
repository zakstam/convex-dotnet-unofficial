using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

public class Player
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("hasGuessedCorrectly")]
    public bool HasGuessedCorrectly { get; set; }

    [JsonPropertyName("drawingTurn")]
    public double? DrawingTurn { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "connected";

    [JsonPropertyName("lastSeen")]
    public long LastSeen { get; set; }
}
