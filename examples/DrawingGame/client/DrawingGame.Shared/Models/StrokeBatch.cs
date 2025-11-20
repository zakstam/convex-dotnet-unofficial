using Convex.Client.Extensions.Batching.TimeBasedBatching;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

/// <summary>
/// Represents a batch of stroke points with style information.
/// Matches the backend strokeBatches table structure.
/// </summary>
public class StrokeBatch
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; } = string.Empty;

    [JsonPropertyName("round")]
    public double Round { get; set; }

    [JsonPropertyName("drawer")]
    public string Drawer { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<BatchableStrokePoint> Events { get; set; } = new();

    [JsonPropertyName("batchStartTime")]
    public double BatchStartTime { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#000000";

    [JsonPropertyName("thickness")]
    public double Thickness { get; set; } = 2;

    [JsonPropertyName("tool")]
    public string Tool { get; set; } = "pencil";

    [JsonPropertyName("lastUpdated")]
    public double LastUpdated { get; set; }
}

