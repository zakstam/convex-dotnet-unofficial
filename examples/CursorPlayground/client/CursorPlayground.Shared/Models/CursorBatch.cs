using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents a batch of cursor position events from a user.
/// Matches the backend strokeBatches table structure.
/// </summary>
public class CursorBatch
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<BatchableCursorEvent> Events { get; set; } = new();

    [JsonPropertyName("batchStartTime")]
    public double BatchStartTime { get; set; }
}
