using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Features.Observability.Resilience;

/// <summary>
/// Provides resilience patterns (retry, circuit breaker) for robust client operations.
/// Thread-safe for concurrent resilience policy management.
/// </summary>
public interface IConvexResilience
{
    /// <summary>
    /// Gets or sets the retry policy for failed operations.
    /// </summary>
    RetryPolicy? RetryPolicy { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker policy for preventing cascade failures.
    /// </summary>
    ICircuitBreakerPolicy? CircuitBreakerPolicy { get; set; }

    /// <summary>
    /// Executes an operation with resilience policies applied (retry + circuit breaker).
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation without return value with resilience policies applied.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
