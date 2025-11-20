using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

public class Guess
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("round")]
    public double Round { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("guess")]
    public string GuessText { get; set; } = string.Empty;

    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("pointsAwarded")]
    public double PointsAwarded { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
