namespace Convex.Client.Extensions.Gaming.Sync;

/// <summary>
/// Interface for types that support interpolation between states.
/// Implement this interface on your game state types to enable smooth
/// client-side interpolation between server updates.
/// </summary>
/// <typeparam name="T">The type that implements this interface.</typeparam>
/// <example>
/// <code>
/// public class PlayerPosition : IInterpolatable&lt;PlayerPosition&gt;
/// {
///     public float X { get; set; }
///     public float Y { get; set; }
///
///     public PlayerPosition Interpolate(PlayerPosition target, float t)
///     {
///         return new PlayerPosition
///         {
///             X = X + (target.X - X) * t,
///             Y = Y + (target.Y - Y) * t
///         };
///     }
/// }
/// </code>
/// </example>
public interface IInterpolatable<T> where T : IInterpolatable<T>
{
    /// <summary>
    /// Interpolates between this state and the target state.
    /// </summary>
    /// <param name="target">The target state to interpolate towards.</param>
    /// <param name="t">The interpolation factor (0.0 = this state, 1.0 = target state).</param>
    /// <returns>A new state that is the interpolation between this and target.</returns>
    T Interpolate(T target, float t);
}

/// <summary>
/// Interface for types that support extrapolation (prediction beyond known states).
/// Optional extension of IInterpolatable for dead reckoning scenarios.
/// </summary>
/// <typeparam name="T">The type that implements this interface.</typeparam>
public interface IExtrapolatable<T> : IInterpolatable<T> where T : IExtrapolatable<T>
{
    /// <summary>
    /// Extrapolates this state forward in time.
    /// Useful for dead reckoning when server updates are delayed.
    /// </summary>
    /// <param name="deltaTimeMs">The time in milliseconds to extrapolate forward.</param>
    /// <returns>A new state extrapolated from this state.</returns>
    T Extrapolate(double deltaTimeMs);
}
