namespace Convex.Client.Extensions.Gaming.Sync;

/// <summary>
/// Represents a state type that supports client-side prediction.
/// Implement this interface to enable instant local response to player inputs
/// while maintaining server authority.
/// </summary>
/// <typeparam name="TState">The state type (must be self-referencing for proper typing).</typeparam>
/// <typeparam name="TInput">The input type that can be applied to the state.</typeparam>
/// <remarks>
/// <para>
/// Client-side prediction works by:
/// </para>
/// <list type="number">
/// <item>Applying inputs locally for instant feedback</item>
/// <item>Sending inputs to the server</item>
/// <item>When server state arrives, reconciling by re-applying unacknowledged inputs</item>
/// </list>
/// <para>
/// The <see cref="ApplyInput"/> method must be deterministic - given the same state
/// and input, it must always produce the same result. This ensures client and server
/// stay synchronized.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PlayerState : IPredictable&lt;PlayerState, MoveInput&gt;
/// {
///     public float X { get; init; }
///     public float Y { get; init; }
///     public float Speed { get; init; } = 5f;
///
///     public PlayerState ApplyInput(MoveInput input, double deltaTimeMs)
///     {
///         var dt = (float)(deltaTimeMs / 1000.0);
///         return this with
///         {
///             X = X + (input.DirectionX * Speed * dt),
///             Y = Y + (input.DirectionY * Speed * dt)
///         };
///     }
///
///     public PlayerState Clone() => this with { };
/// }
/// </code>
/// </example>
public interface IPredictable<TState, in TInput>
    where TState : IPredictable<TState, TInput>
{
    /// <summary>
    /// Applies an input to the current state and returns the new state.
    /// </summary>
    /// <param name="input">The input to apply.</param>
    /// <param name="deltaTimeMs">The time delta in milliseconds since the last state update.</param>
    /// <returns>A new state with the input applied.</returns>
    /// <remarks>
    /// This method must be deterministic and should not modify the current instance.
    /// Return a new state object with the input applied.
    /// </remarks>
    TState ApplyInput(TInput input, double deltaTimeMs);

    /// <summary>
    /// Creates a deep copy of the current state.
    /// </summary>
    /// <returns>A new instance that is a copy of the current state.</returns>
    /// <remarks>
    /// This is used during reconciliation to preserve the confirmed state
    /// before re-applying pending inputs.
    /// </remarks>
    TState Clone();
}
