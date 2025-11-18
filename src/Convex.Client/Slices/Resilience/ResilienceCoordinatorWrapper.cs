using Convex.Client.Shared.Resilience;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Slices.Resilience;

/// <summary>
/// Internal wrapper around ResilienceCoordinator for resilience pattern execution.
/// Combines retry and circuit breaker policies for robust operation execution.
/// </summary>
internal sealed class ResilienceCoordinatorWrapper(ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly ResilienceCoordinator _coordinator = new ResilienceCoordinator(null, null, logger, enableDebugLogging);

    public RetryPolicy? RetryPolicy
    {
        get => _coordinator.RetryPolicy;
        set => _coordinator.RetryPolicy = value;
    }

    public ICircuitBreakerPolicy? CircuitBreakerPolicy
    {
        get => _coordinator.CircuitBreakerPolicy;
        set => _coordinator.CircuitBreakerPolicy = value;
    }

    public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        => _coordinator.ExecuteAsync(operation, cancellationToken);

    public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        => _coordinator.ExecuteAsync(operation, cancellationToken);
}
