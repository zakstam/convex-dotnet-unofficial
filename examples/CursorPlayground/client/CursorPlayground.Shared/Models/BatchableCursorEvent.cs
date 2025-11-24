using Convex.Client.Extensions.Batching.TimeBasedBatching;
using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Wrapper for CursorPosition that implements IBatchableEvent for time-based batching.
/// </summary>
public class BatchableCursorEvent : IBatchableEvent<CursorPosition>
{
    [JsonPropertyName("timeSinceBatchStart")]
    public double TimeSinceBatchStart { get; set; }

    [JsonPropertyName("eventData")]
    public CursorPosition EventData { get; set; } = new();
}
