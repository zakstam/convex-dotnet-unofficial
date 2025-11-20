using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

/// <summary>
/// Clean stroke point data for batch events (without timestamp).
/// Matches the backend eventData structure.
/// </summary>
public class StrokePointData
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; set; }
}

