using Convex.Client.Extensions.Batching.TimeBasedBatching;
using System.Text.Json.Serialization;

namespace DrawingGame.Shared.Models;

/// <summary>
/// Wrapper for StrokePointData that implements IBatchableEvent for time-based batching.
/// </summary>
public class BatchableStrokePoint : IBatchableEvent<StrokePointData>
{
    [JsonPropertyName("timeSinceBatchStart")]
    public double TimeSinceBatchStart { get; set; }

    [JsonPropertyName("eventData")]
    public StrokePointData EventData { get; set; } = new();
}

