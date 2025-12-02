using System.Diagnostics;

namespace Convex.Client.Extensions.Gaming.Sync;

/// <summary>
/// Represents a timestamped input with a unique sequence ID for reconciliation.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <param name="SequenceId">The unique sequence ID for this input.</param>
/// <param name="Input">The input data.</param>
/// <param name="TimestampMs">The timestamp when this input was created, in milliseconds.</param>
/// <param name="DeltaTimeMs">The delta time used when applying this input, in milliseconds.</param>
public readonly record struct TimestampedInput<TInput>(
    long SequenceId,
    TInput Input,
    double TimestampMs,
    double DeltaTimeMs);

/// <summary>
/// Provides client-side prediction for game state, enabling instant local response
/// to player inputs while maintaining server authority.
/// </summary>
/// <typeparam name="TState">The state type that implements <see cref="IPredictable{TState, TInput}"/>.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <remarks>
/// <para>
/// <b>How Prediction Works:</b>
/// </para>
/// <list type="number">
/// <item>Player performs an action (e.g., moves)</item>
/// <item><see cref="ApplyInput(TInput)"/> applies it locally for instant feedback</item>
/// <item>Input is queued and sent to the server</item>
/// <item>Server processes input and sends back authoritative state</item>
/// <item><see cref="OnServerState"/> reconciles by re-applying unacknowledged inputs</item>
/// </list>
/// <para>
/// <b>Cost Benefits:</b>
/// </para>
/// <list type="bullet">
/// <item>Inputs can be batched (reducing mutations by 90%+)</item>
/// <item>Only need to sync state periodically (reducing subscription traffic)</item>
/// <item>Players experience instant response despite network latency</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var prediction = new PredictedState&lt;PlayerState, MoveInput&gt;(initialState);
///
/// // In input handler (called every frame)
/// var input = new MoveInput { DirectionX = 1, DirectionY = 0 };
/// var timestamped = prediction.ApplyInput(input, deltaTimeMs);
/// await batcher.AddAsync(timestamped); // Batch inputs to server
///
/// // When server state arrives
/// prediction.OnServerState(serverState, serverLastProcessedInputId);
///
/// // In render loop
/// Render(prediction.PredictedState);
/// </code>
/// </example>
public sealed class PredictedState<TState, TInput>
    where TState : class, IPredictable<TState, TInput>
{
    private readonly object _lock = new();
    private readonly Queue<TimestampedInput<TInput>> _pendingInputs;
    private readonly int _maxPendingInputs;
    private readonly Stopwatch _stopwatch;

    private TState _confirmedState;
    private TState _predictedState;
    private long _nextSequenceId;
    private double _lastInputTimeMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictedState{TState, TInput}"/> class.
    /// </summary>
    /// <param name="initialState">The initial confirmed state from the server.</param>
    /// <param name="maxPendingInputs">
    /// Maximum number of pending inputs to keep. Older inputs are discarded if exceeded.
    /// Default: 60 (about 1-3 seconds of input at typical rates).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialState"/> is null.</exception>
    public PredictedState(TState initialState, int maxPendingInputs = 60)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPendingInputs, 1);

        _confirmedState = initialState;
        _predictedState = initialState.Clone();
        _pendingInputs = new Queue<TimestampedInput<TInput>>(maxPendingInputs);
        _maxPendingInputs = maxPendingInputs;
        _stopwatch = Stopwatch.StartNew();
        _lastInputTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Gets the last confirmed state from the server.
    /// </summary>
    public TState ConfirmedState
    {
        get
        {
            lock (_lock)
            {
                return _confirmedState;
            }
        }
    }

    /// <summary>
    /// Gets the current predicted state (confirmed state + pending inputs applied).
    /// Use this for rendering.
    /// </summary>
    public TState CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _predictedState;
            }
        }
    }

    /// <summary>
    /// Gets the number of inputs that have been applied locally but not yet confirmed by the server.
    /// </summary>
    public int PendingInputCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingInputs.Count;
            }
        }
    }

    /// <summary>
    /// Gets the sequence ID that will be assigned to the next input.
    /// </summary>
    public long NextSequenceId
    {
        get
        {
            lock (_lock)
            {
                return _nextSequenceId;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether prediction is enabled.
    /// When disabled, inputs are not applied locally and <see cref="CurrentState"/>
    /// returns the confirmed state.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Applies an input locally and returns the timestamped input for sending to the server.
    /// </summary>
    /// <param name="input">The input to apply.</param>
    /// <returns>
    /// A <see cref="TimestampedInput{TInput}"/> containing the input with its sequence ID
    /// and timestamp. Send this to the server for processing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The input is immediately applied to the predicted state for instant feedback.
    /// The returned timestamped input should be sent to the server (typically via batching).
    /// </para>
    /// <para>
    /// Delta time is automatically calculated from the time since the last input.
    /// </para>
    /// </remarks>
    public TimestampedInput<TInput> ApplyInput(TInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        lock (_lock)
        {
            var currentTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
            var deltaTimeMs = currentTimeMs - _lastInputTimeMs;
            _lastInputTimeMs = currentTimeMs;

            var timestamped = new TimestampedInput<TInput>(
                _nextSequenceId++,
                input,
                currentTimeMs,
                deltaTimeMs
            );

            if (IsEnabled)
            {
                // Apply to predicted state
                _predictedState = _predictedState.ApplyInput(input, deltaTimeMs);

                // Queue the input for reconciliation
                _pendingInputs.Enqueue(timestamped);

                // Trim old inputs if we exceed max
                while (_pendingInputs.Count > _maxPendingInputs)
                {
                    _ = _pendingInputs.Dequeue();
                }
            }

            return timestamped;
        }
    }

    /// <summary>
    /// Applies an input locally with a specific delta time.
    /// </summary>
    /// <param name="input">The input to apply.</param>
    /// <param name="deltaTimeMs">The delta time in milliseconds.</param>
    /// <returns>
    /// A <see cref="TimestampedInput{TInput}"/> containing the input with its sequence ID
    /// and timestamp. Send this to the server for processing.
    /// </returns>
    public TimestampedInput<TInput> ApplyInput(TInput input, double deltaTimeMs)
    {
        ArgumentNullException.ThrowIfNull(input);

        lock (_lock)
        {
            var currentTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
            _lastInputTimeMs = currentTimeMs;

            var timestamped = new TimestampedInput<TInput>(
                _nextSequenceId++,
                input,
                currentTimeMs,
                deltaTimeMs
            );

            if (IsEnabled)
            {
                // Apply to predicted state
                _predictedState = _predictedState.ApplyInput(input, deltaTimeMs);

                // Queue the input for reconciliation
                _pendingInputs.Enqueue(timestamped);

                // Trim old inputs if we exceed max
                while (_pendingInputs.Count > _maxPendingInputs)
                {
                    _ = _pendingInputs.Dequeue();
                }
            }

            return timestamped;
        }
    }

    /// <summary>
    /// Processes an authoritative state update from the server.
    /// Reconciles by discarding acknowledged inputs and re-applying pending ones.
    /// </summary>
    /// <param name="serverState">The authoritative state from the server.</param>
    /// <param name="lastProcessedInputId">
    /// The sequence ID of the last input the server has processed.
    /// Inputs with IDs less than or equal to this are considered acknowledged.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method:
    /// </para>
    /// <list type="number">
    /// <item>Updates the confirmed state to the server state</item>
    /// <item>Discards all inputs that have been acknowledged (ID &lt;= lastProcessedInputId)</item>
    /// <item>Re-applies any remaining pending inputs to get the new predicted state</item>
    /// </list>
    /// <para>
    /// If there's a mismatch between predicted and server state (misprediction),
    /// the client will snap to the correct state. For smoother correction,
    /// consider combining with interpolation.
    /// </para>
    /// </remarks>
    public void OnServerState(TState serverState, long lastProcessedInputId)
    {
        ArgumentNullException.ThrowIfNull(serverState);

        lock (_lock)
        {
            // Update confirmed state
            _confirmedState = serverState;

            if (!IsEnabled)
            {
                _predictedState = serverState.Clone();
                _pendingInputs.Clear();
                return;
            }

            // Remove acknowledged inputs
            while (_pendingInputs.Count > 0 && _pendingInputs.Peek().SequenceId <= lastProcessedInputId)
            {
                _ = _pendingInputs.Dequeue();
            }

            // Re-apply pending inputs to server state
            _predictedState = serverState.Clone();
            foreach (var pending in _pendingInputs)
            {
                _predictedState = _predictedState.ApplyInput(pending.Input, pending.DeltaTimeMs);
            }
        }
    }

    /// <summary>
    /// Resets the prediction state to a new confirmed state, clearing all pending inputs.
    /// </summary>
    /// <param name="newState">The new state to reset to.</param>
    public void Reset(TState newState)
    {
        ArgumentNullException.ThrowIfNull(newState);

        lock (_lock)
        {
            _confirmedState = newState;
            _predictedState = newState.Clone();
            _pendingInputs.Clear();
            _lastInputTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    /// <summary>
    /// Clears all pending inputs without changing the confirmed state.
    /// The predicted state will be reset to match the confirmed state.
    /// </summary>
    public void ClearPendingInputs()
    {
        lock (_lock)
        {
            _pendingInputs.Clear();
            _predictedState = _confirmedState.Clone();
        }
    }
}
