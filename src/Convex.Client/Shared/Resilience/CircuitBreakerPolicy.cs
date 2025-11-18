using Convex.Client.Shared.ErrorHandling;

namespace Convex.Client.Shared.Resilience;

/// <summary>
/// Interface for circuit breaker policies that prevent cascade failures.
/// </summary>
public interface ICircuitBreakerPolicy
{
    /// <summary>
    /// Gets the failure threshold before opening the circuit.
    /// </summary>
    int FailureThreshold { get; }

    /// <summary>
    /// Gets the duration to keep the circuit open.
    /// </summary>
    TimeSpan BreakDuration { get; }

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    CircuitBreakerState State { get; }

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Records a failed operation.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    void RecordFailure(Exception exception);

    /// <summary>
    /// Determines if the circuit allows a request to proceed.
    /// </summary>
    /// <returns>True if the request can proceed.</returns>
    bool AllowRequest();
}

/// <summary>
/// Standard circuit breaker implementation with configurable thresholds.
/// </summary>
/// <remarks>
/// Initializes a new instance of the CircuitBreakerPolicy class.
/// </remarks>
/// <param name="failureThreshold">Number of failures before opening circuit.</param>
/// <param name="breakDuration">Duration to keep circuit open.</param>
public class CircuitBreakerPolicy(int failureThreshold = 5, TimeSpan? breakDuration = null) : ICircuitBreakerPolicy
{
    private readonly object _lock = new();
    private int _failureCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerState _state = CircuitBreakerState.Closed;

    /// <summary>
    /// Gets the failure threshold before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; } = failureThreshold;

    /// <summary>
    /// Gets the duration to keep the circuit open.
    /// </summary>
    public TimeSpan BreakDuration { get; } = breakDuration ?? TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerState State => _state;

    /// <inheritdoc/>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitBreakerState.Closed;
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(Exception exception)
    {
        // Only count failures that indicate service issues
        if (!ShouldCountFailure(exception))
            return;

        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            if (_failureCount >= FailureThreshold)
            {
                _state = CircuitBreakerState.Open;
            }
        }
    }

    /// <inheritdoc/>
    public bool AllowRequest()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitBreakerState.Closed:
                    return true;

                case CircuitBreakerState.Open:
                    if (DateTime.UtcNow - _lastFailureTime >= BreakDuration)
                    {
                        _state = CircuitBreakerState.HalfOpen;
                        return true;
                    }
                    return false;

                case CircuitBreakerState.HalfOpen:
                    return true;

                default:
                    return false;
            }
        }
    }

    private static bool ShouldCountFailure(Exception exception)
    {
        return exception switch
        {
            ConvexNetworkException networkEx => networkEx.ErrorType != NetworkErrorType.DnsResolution,
            TaskCanceledException => true,
            HttpRequestException => true,
            ConvexFunctionException => false, // Function errors don't indicate service issues
            ConvexArgumentException => false, // Argument errors don't indicate service issues
            _ => false
        };
    }
}

/// <summary>
/// Interface for reconnect policies that determine reconnection behavior.
/// </summary>
public interface IReconnectPolicy
{
    /// <summary>
    /// Gets the delay before the next reconnection attempt.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (starting from 1).</param>
    /// <returns>The delay before reconnecting, or null to stop reconnecting.</returns>
    TimeSpan? GetReconnectDelay(int attemptNumber);

    /// <summary>
    /// Resets the reconnect policy state.
    /// </summary>
    void Reset();
}

