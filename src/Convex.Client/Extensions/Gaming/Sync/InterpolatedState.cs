using System.Diagnostics;

namespace Convex.Client.Extensions.Gaming.Sync;

/// <summary>
/// Provides client-side interpolation between server state updates.
/// This enables smooth 60fps rendering even when receiving updates at lower frequencies (e.g., 10-20Hz).
/// </summary>
/// <typeparam name="T">The state type that implements <see cref="IInterpolatable{T}"/>.</typeparam>
/// <remarks>
/// <para>
/// Interpolation works by keeping a buffer of recent server states and rendering
/// slightly behind real-time (controlled by <see cref="InterpolationDelayMs"/>).
/// This delay provides a window for smooth transitions between states.
/// </para>
/// <para>
/// <b>Cost Savings:</b> Reduces subscription updates from 60/sec to 10-20/sec (67-83% reduction)
/// while maintaining smooth visuals.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var interpolated = new InterpolatedState&lt;PlayerPositions&gt;();
///
/// // Subscribe with throttling (10 updates/sec)
/// client.Observe&lt;PlayerPositions&gt;("game:positions")
///     .Sample(TimeSpan.FromMilliseconds(100))
///     .Subscribe(state => interpolated.PushState(state));
///
/// // In render loop (60fps)
/// void Render()
/// {
///     var smooth = interpolated.GetRenderState();
///     DrawPlayers(smooth);
/// }
/// </code>
/// </example>
public class InterpolatedState<T> where T : class, IInterpolatable<T>
{
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch;
    private readonly Queue<TimestampedState> _stateBuffer;
    private readonly int _maxBufferSize;

    private T? _currentState;
    private T? _previousState;
    private double _currentStateTime;
    private double _previousStateTime;

    /// <summary>
    /// Gets or sets the interpolation delay in milliseconds.
    /// Higher values provide smoother interpolation but increase visual latency.
    /// Default: 100ms (good balance for most games).
    /// </summary>
    /// <remarks>
    /// Recommended values:
    /// - Action games: 50-100ms
    /// - Casual games: 100-150ms
    /// - Turn-based: 0ms (no interpolation needed)
    /// </remarks>
    public double InterpolationDelayMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum extrapolation time in milliseconds.
    /// If no new state arrives within this time, extrapolation stops to avoid visual artifacts.
    /// Default: 250ms.
    /// </summary>
    public double MaxExtrapolationMs { get; set; } = 250;

    /// <summary>
    /// Gets whether interpolation is currently active (has at least 2 states).
    /// </summary>
    public bool IsInterpolating
    {
        get
        {
            lock (_lock)
            {
                return _previousState != null && _currentState != null;
            }
        }
    }

    /// <summary>
    /// Gets the number of states currently buffered.
    /// </summary>
    public int BufferedStateCount
    {
        get
        {
            lock (_lock)
            {
                return _stateBuffer.Count + (_currentState != null ? 1 : 0) + (_previousState != null ? 1 : 0);
            }
        }
    }

    /// <summary>
    /// Gets the current raw state (most recent from server, without interpolation).
    /// </summary>
    public T? CurrentRawState
    {
        get
        {
            lock (_lock)
            {
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InterpolatedState{T}"/>.
    /// </summary>
    /// <param name="maxBufferSize">Maximum number of states to buffer. Default: 3.</param>
    public InterpolatedState(int maxBufferSize = 3)
    {
        _maxBufferSize = maxBufferSize;
        _stateBuffer = new Queue<TimestampedState>(maxBufferSize);
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Pushes a new state from the server into the interpolation buffer.
    /// Call this from your subscription handler.
    /// </summary>
    /// <param name="state">The new state received from the server.</param>
    public void PushState(T state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_lock)
        {
            var now = _stopwatch.Elapsed.TotalMilliseconds;

            // Shift states
            _previousState = _currentState;
            _previousStateTime = _currentStateTime;

            _currentState = state;
            _currentStateTime = now;

            // Add to buffer for potential future use
            _stateBuffer.Enqueue(new TimestampedState(state, now));
            while (_stateBuffer.Count > _maxBufferSize)
            {
                _ = _stateBuffer.Dequeue();
            }
        }
    }

    /// <summary>
    /// Gets the interpolated state for rendering.
    /// Call this every frame in your render loop.
    /// </summary>
    /// <returns>
    /// The interpolated state, or the current raw state if interpolation isn't possible,
    /// or null if no state has been received yet.
    /// </returns>
    public T? GetRenderState() => GetRenderState(_stopwatch.Elapsed.TotalMilliseconds);

    /// <summary>
    /// Gets the interpolated state for a specific render time.
    /// Useful for replay systems or custom timing.
    /// </summary>
    /// <param name="renderTimeMs">The render time in milliseconds (from the same time source as PushState).</param>
    /// <returns>The interpolated state, or null if no state has been received.</returns>
    public T? GetRenderState(double renderTimeMs)
    {
        lock (_lock)
        {
            // No state yet
            if (_currentState == null)
            {
                return null;
            }

            // Only one state - return it directly
            if (_previousState == null)
            {
                return _currentState;
            }

            // Calculate the target render time (behind real-time by InterpolationDelayMs)
            var targetTime = renderTimeMs - InterpolationDelayMs;

            // If target time is before our previous state, just return previous
            if (targetTime <= _previousStateTime)
            {
                return _previousState;
            }

            // If target time is after our current state, check if we should extrapolate
            if (targetTime >= _currentStateTime)
            {
                var timeBeyondCurrent = targetTime - _currentStateTime;

                // If we're too far beyond, just return current state (no extrapolation)
                if (timeBeyondCurrent > MaxExtrapolationMs)
                {
                    return _currentState;
                }

                // Try to extrapolate if the type supports it
                // Note: We check at runtime since T may implement IExtrapolatable
                if (TryExtrapolate(_currentState, timeBeyondCurrent, out var extrapolated))
                {
                    return extrapolated;
                }

                // Otherwise just return current
                return _currentState;
            }

            // Normal interpolation between previous and current states
            var totalDuration = _currentStateTime - _previousStateTime;
            if (totalDuration <= 0)
            {
                return _currentState;
            }

            var elapsed = targetTime - _previousStateTime;
            var t = (float)Math.Clamp(elapsed / totalDuration, 0.0, 1.0);

            return _previousState.Interpolate(_currentState, t);
        }
    }

    /// <summary>
    /// Resets the interpolation state, clearing all buffered states.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _currentState = null;
            _previousState = null;
            _currentStateTime = 0;
            _previousStateTime = 0;
            _stateBuffer.Clear();
        }
    }

    private readonly record struct TimestampedState(T State, double TimeMs);

    private static bool TryExtrapolate(T state, double deltaTimeMs, out T? result)
    {
        // Use reflection to check for IExtrapolatable at runtime
        // This avoids the generic constraint issue while still allowing extrapolation
        var type = state.GetType();
        var extrapolatableInterface = type.GetInterfaces()
            .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExtrapolatable<>));

        if (extrapolatableInterface != null)
        {
            var method = type.GetMethod("Extrapolate");
            if (method != null)
            {
                result = (T?)method.Invoke(state, [deltaTimeMs]);
                return result != null;
            }
        }

        result = default;
        return false;
    }
}
