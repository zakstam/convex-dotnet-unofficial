using Microsoft.Extensions.Logging;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Telemetry;

namespace Convex.Client.Infrastructure.Resilience;

/// <summary>
/// Coordinates resilience policies (retry, circuit breaker) for operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ResilienceCoordinator class.
/// </remarks>
/// <param name="retryPolicy">Optional retry policy.</param>
/// <param name="circuitBreakerPolicy">Optional circuit breaker policy.</param>
/// <param name="logger">Optional logger.</param>
/// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
public class ResilienceCoordinator(RetryPolicy? retryPolicy = null, ICircuitBreakerPolicy? circuitBreakerPolicy = null, ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    /// <summary>
    /// Gets or sets the retry policy.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; set; } = retryPolicy;

    /// <summary>
    /// Gets or sets the circuit breaker policy.
    /// </summary>
    public ICircuitBreakerPolicy? CircuitBreakerPolicy { get; set; } = circuitBreakerPolicy;

    /// <summary>
    /// Executes an operation with resilience policies applied.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // Check cancellation token before starting
        cancellationToken.ThrowIfCancellationRequested();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Resilience] Starting operation execution: HasRetryPolicy: {HasRetryPolicy}, HasCircuitBreakerPolicy: {HasCircuitBreakerPolicy}",
                RetryPolicy != null, CircuitBreakerPolicy != null);
        }

        // Check circuit breaker first
        if (CircuitBreakerPolicy != null && !CircuitBreakerPolicy.AllowRequest())
        {
            var state = CircuitBreakerPolicy.State;
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogWarning("[Resilience] Circuit breaker is {State}, request blocked", state);
            }
            else
            {
                _logger?.LogWarning("Circuit breaker is {State}, request blocked", state);
            }
            throw new ConvexCircuitBreakerException("Circuit breaker is open", state);
        }

        var attempt = 0;
        Exception? lastException = null;

        while (true)
        {
            try
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging) && attempt > 0)
                {
                    _logger!.LogDebug("[Resilience] Retrying operation: Attempt: {Attempt}", attempt + 1);
                }

                var result = await operation();

                // Record success for circuit breaker
                CircuitBreakerPolicy?.RecordSuccess();

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Resilience] Operation completed successfully: Attempt: {Attempt}, ResultType: {ResultType}",
                        attempt + 1, typeof(T).Name);
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning(ex, "[Resilience] Operation failed: Attempt: {Attempt}, ErrorType: {ErrorType}, Message: {Message}",
                        attempt + 1, ex.GetType().Name, ex.Message);
                }

                // Record failure for circuit breaker
                var previousState = CircuitBreakerPolicy?.State;
                CircuitBreakerPolicy?.RecordFailure(ex);
                var newState = CircuitBreakerPolicy?.State;

                if (previousState != newState && ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning("[Resilience] Circuit breaker state changed: {PreviousState} -> {NewState}", previousState, newState);
                }

                // Check if we should retry
                if (RetryPolicy == null || !RetryPolicy.ShouldRetry(ex))
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Resilience] Operation failed, no retry: Attempt: {Attempt}, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                            attempt + 1, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }
                    else
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                    }
                    throw;
                }

                // Check if we've exceeded max retries
                if (attempt >= RetryPolicy.MaxRetries)
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Resilience] Operation failed after {Attempts} attempts: ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                            attempt + 1, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }
                    else
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                    }
                    throw;
                }

                // Wait before retrying (attempt is 0-based, CalculateDelay expects 1-based)
                var retryDelay = RetryPolicy.CalculateDelay(attempt + 1);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Resilience] Waiting before retry: Attempt: {Attempt}, Delay: {DelayMs}ms",
                        attempt + 1, retryDelay.TotalMilliseconds);
                }
                else
                {
                    _logger?.LogWarning(ex, "Operation failed on attempt {Attempt}, retrying in {Delay}ms",
                        attempt + 1, retryDelay.TotalMilliseconds);
                }

                await Task.Delay(retryDelay, cancellationToken);

                attempt++;
            }
        }
    }

    /// <summary>
    /// Executes an operation without return value with resilience policies applied.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        // Check cancellation token before starting
        cancellationToken.ThrowIfCancellationRequested();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[Resilience] Starting operation execution (void): HasRetryPolicy: {HasRetryPolicy}, HasCircuitBreakerPolicy: {HasCircuitBreakerPolicy}",
                RetryPolicy != null, CircuitBreakerPolicy != null);
        }

        // Check circuit breaker first
        if (CircuitBreakerPolicy != null && !CircuitBreakerPolicy.AllowRequest())
        {
            var state = CircuitBreakerPolicy.State;
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogWarning("[Resilience] Circuit breaker is {State}, request blocked", state);
            }
            else
            {
                _logger?.LogWarning("Circuit breaker is {State}, request blocked", state);
            }
            throw new ConvexCircuitBreakerException("Circuit breaker is open", state);
        }

        var attempt = 0;
        Exception? lastException = null;

        while (true)
        {
            try
            {
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging) && attempt > 0)
                {
                    _logger!.LogDebug("[Resilience] Retrying operation (void): Attempt: {Attempt}", attempt + 1);
                }

                await operation();

                // Record success for circuit breaker
                CircuitBreakerPolicy?.RecordSuccess();

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Resilience] Operation completed successfully (void): Attempt: {Attempt}",
                        attempt + 1);
                }

                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning(ex, "[Resilience] Operation failed (void): Attempt: {Attempt}, ErrorType: {ErrorType}, Message: {Message}",
                        attempt + 1, ex.GetType().Name, ex.Message);
                }

                // Record failure for circuit breaker
                var previousState = CircuitBreakerPolicy?.State;
                CircuitBreakerPolicy?.RecordFailure(ex);
                var newState = CircuitBreakerPolicy?.State;

                if (previousState != newState && ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogWarning("[Resilience] Circuit breaker state changed: {PreviousState} -> {NewState}", previousState, newState);
                }

                // Check if we should retry
                if (RetryPolicy == null || !RetryPolicy.ShouldRetry(ex))
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Resilience] Operation failed, no retry (void): Attempt: {Attempt}, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                            attempt + 1, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }
                    else
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                    }
                    throw;
                }

                // Check if we've exceeded max retries
                if (attempt >= RetryPolicy.MaxRetries)
                {
                    if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                    {
                        _logger!.LogError(ex, "[Resilience] Operation failed after {Attempts} attempts (void): ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                            attempt + 1, ex.GetType().Name, ex.Message, ex.StackTrace);
                    }
                    else
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                    }
                    throw;
                }

                // Wait before retrying (attempt is 0-based, CalculateDelay expects 1-based)
                var retryDelay = RetryPolicy.CalculateDelay(attempt + 1);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogDebug("[Resilience] Waiting before retry (void): Attempt: {Attempt}, Delay: {DelayMs}ms",
                        attempt + 1, retryDelay.TotalMilliseconds);
                }
                else
                {
                    _logger?.LogWarning(ex, "Operation failed on attempt {Attempt}, retrying in {Delay}ms",
                        attempt + 1, retryDelay.TotalMilliseconds);
                }

                await Task.Delay(retryDelay, cancellationToken);

                attempt++;
            }
        }
    }
}

