using Convex.Client.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Observability.Resilience;

/// <summary>
/// Resilience slice - provides retry and circuit breaker patterns for robust operations.
/// This is a self-contained vertical slice that handles all resilience functionality.
/// </summary>
public class ResilienceSlice(ILogger? logger = null, bool enableDebugLogging = false) : IConvexResilience
{
    private readonly ResilienceCoordinatorWrapper _implementation = new ResilienceCoordinatorWrapper(logger, enableDebugLogging);

    /// <summary>
    /// Represents retry policy.
    /// </summary>
    public RetryPolicy? RetryPolicy
    {
        get => _implementation.RetryPolicy;
        set => _implementation.RetryPolicy = value;
    }

    /// <summary>
    /// Represents circuit breaker policy.
    /// </summary>
    public ICircuitBreakerPolicy? CircuitBreakerPolicy
    {
        get => _implementation.CircuitBreakerPolicy;
        set => _implementation.CircuitBreakerPolicy = value;
    }

    /// <summary>
    /// Executes the execute operation.
    /// </summary>
    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        => _implementation.ExecuteAsync(operation, cancellationToken);

    /// <summary>
    /// Executes the execute operation.
    /// </summary>
    public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        => _implementation.ExecuteAsync(operation, cancellationToken);
}
