using System.Text.Json.Serialization;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents a cursor position event with coordinates and optional velocity.
/// </summary>
public class CursorPosition
{
    /// <summary>
    /// X coordinate (percentage 0-100 or pixel value).
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Y coordinate (percentage 0-100 or pixel value).
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }

    /// <summary>
    /// Optional velocity for trail effects (calculated from movement speed).
    /// </summary>
    [JsonPropertyName("velocity")]
    public double? Velocity { get; set; }
}
