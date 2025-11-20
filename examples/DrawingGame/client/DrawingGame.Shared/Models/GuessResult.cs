using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

public class GuessResult
{
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("pointsAwarded")]
    public double PointsAwarded { get; set; }
}
