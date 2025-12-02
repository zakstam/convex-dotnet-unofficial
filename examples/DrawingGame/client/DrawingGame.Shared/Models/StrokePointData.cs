using System.Text.Json.Serialization;
using Convex.Client.Extensions.Gaming.Sync;

namespace DrawingGame.Shared.Models;

/// <summary>
/// Clean stroke point data for batch events (without timestamp).
/// Matches the backend eventData structure.
/// Implements <see cref="IInterpolatable{T}"/> for smooth stroke rendering across network updates.
/// </summary>
public class StrokePointData : IInterpolatable<StrokePointData>
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; set; }

    /// <summary>
    /// Interpolates between this point and a target point.
    /// Used for smooth stroke rendering between network updates.
    /// </summary>
    /// <param name="target">The target point to interpolate towards.</param>
    /// <param name="t">Interpolation factor (0 = this, 1 = target).</param>
    /// <returns>An interpolated stroke point.</returns>
    public StrokePointData Interpolate(StrokePointData target, float t) => new()
    {
        X = X + ((target.X - X) * t),
        Y = Y + ((target.Y - Y) * t),
        Pressure = Pressure.HasValue && target.Pressure.HasValue
            ? Pressure.Value + ((target.Pressure.Value - Pressure.Value) * t)
            : target.Pressure ?? Pressure
    };
}

