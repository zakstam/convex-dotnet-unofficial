using System.Text.Json.Serialization;
using Convex.Client.Extensions.Gaming.Sync;

namespace CursorPlayground.Shared.Models;

/// <summary>
/// Represents a cursor position event with coordinates and optional velocity.
/// Implements <see cref="IInterpolatable{T}"/> for smooth cursor rendering across network updates.
/// </summary>
public class CursorPosition : IInterpolatable<CursorPosition>
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

    /// <summary>
    /// Interpolates between this position and a target position.
    /// Used for smooth cursor rendering between network updates.
    /// </summary>
    /// <param name="target">The target position to interpolate towards.</param>
    /// <param name="t">Interpolation factor (0 = this, 1 = target).</param>
    /// <returns>An interpolated cursor position.</returns>
    public CursorPosition Interpolate(CursorPosition target, float t) => new()
    {
        X = X + ((target.X - X) * t),
        Y = Y + ((target.Y - Y) * t),
        Velocity = Velocity.HasValue && target.Velocity.HasValue
            ? Velocity.Value + ((target.Velocity.Value - Velocity.Value) * t)
            : target.Velocity ?? Velocity
    };
}
