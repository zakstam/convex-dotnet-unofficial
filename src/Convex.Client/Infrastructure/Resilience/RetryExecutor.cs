namespace Convex.Client.Infrastructure.Resilience;

/// <summary>
/// Executes operations with retry logic according to a configured retry policy.
/// </summary>
internal static class RetryExecutor
{
    /// <summary>
    /// Executes an async operation with retry logic.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="Exception">Throws the last exception if all retries are exhausted.</exception>
    public static async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> operation,
        RetryPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        Exception? lastException = null;
        var attemptNumber = 0;

        while (attemptNumber <= policy.MaxRetries)
        {
            attemptNumber++;

            try
            {
                // Execute the operation
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex) when (attemptNumber <= policy.MaxRetries)
            {
                lastException = ex;

                // Check if this exception type should trigger a retry
                if (!policy.ShouldRetry(ex))
                {
                    throw;
                }

                // Check if cancellation was requested
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        "Operation was canceled during retry attempt.",
                        ex,
                        cancellationToken);
                }

                // Calculate delay before next retry
                var delay = policy.CalculateDelay(attemptNumber);

                // Invoke retry callback if configured
                policy.OnRetryCallback?.Invoke(attemptNumber, ex, delay);

                // Wait before retrying
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // All retries exhausted, throw the last exception
        throw lastException ?? new InvalidOperationException("Retry failed with no exception captured.");
    }

    /// <summary>
    /// Executes an async operation (without return value) with retry logic.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="Exception">Throws the last exception if all retries are exhausted.</exception>
    public static async Task ExecuteAsync(
        Func<Task> operation,
        RetryPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        Exception? lastException = null;
        var attemptNumber = 0;

        while (attemptNumber <= policy.MaxRetries)
        {
            attemptNumber++;

            try
            {
                // Execute the operation
                await operation().ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (attemptNumber <= policy.MaxRetries)
            {
                lastException = ex;

                // Check if this exception type should trigger a retry
                if (!policy.ShouldRetry(ex))
                {
                    throw;
                }

                // Check if cancellation was requested
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(
                        "Operation was canceled during retry attempt.",
                        ex,
                        cancellationToken);
                }

                // Calculate delay before next retry
                var delay = policy.CalculateDelay(attemptNumber);

                // Invoke retry callback if configured
                policy.OnRetryCallback?.Invoke(attemptNumber, ex, delay);

                // Wait before retrying
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        // All retries exhausted, throw the last exception
        throw lastException ?? new InvalidOperationException("Retry failed with no exception captured.");
    }
}
