using System.Reactive.Linq;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for error handling patterns commonly used with Convex clients.
/// These methods provide robust error recovery, circuit breaking, and monitoring capabilities.
/// </summary>
public static class ConvexErrorHandlingExtensions
{
    #region Retry Patterns

    /// <summary>
    /// Retries the observable sequence when a specified condition is met.
    /// Unlike standard Retry, this allows conditional retry logic based on the exception.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="retryCondition">Function that determines whether to retry based on the exception.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="delay">Delay between retry attempts (default: 1 second).</param>
    /// <returns>An observable that retries conditionally.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Query&lt;User&gt;("users:get", new { id })
    ///     .RetryWhen(ex => ex is ConvexException convexEx &amp;&amp;
    ///                      convexEx.ErrorCode == "transient_error", maxRetries: 5)
    ///     .Subscribe(user => DisplayUser(user));
    /// </code>
    /// </example>
    public static IObservable<T> RetryWhen<T>(
        this IObservable<T> source,
        Func<Exception, bool> retryCondition,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(retryCondition);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        var retryDelay = delay ?? TimeSpan.FromSeconds(1);

        return source.Catch<T, Exception>(ex =>
            maxRetries > 0 && retryCondition(ex)
                ? Observable.Timer(retryDelay)
                    .SelectMany(_ => source.RetryWhen(retryCondition, maxRetries - 1, retryDelay))
                : Observable.Throw<T>(ex));
    }

    #endregion

    #region Circuit Breaker Pattern

    /// <summary>
    /// Implements a circuit breaker pattern that opens after consecutive failures
    /// and allows limited requests through when half-open.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="failureThreshold">Number of consecutive failures before opening circuit (default: 5).</param>
    /// <param name="recoveryTimeout">Time to wait before attempting recovery (default: 30 seconds).</param>
    /// <param name="successThreshold">Number of successes needed to close circuit when half-open (default: 3).</param>
    /// <returns>An observable protected by a circuit breaker.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Mutation("messages:send", message)
    ///     .WithCircuitBreaker(failureThreshold: 3, recoveryTimeout: TimeSpan.FromMinutes(1))
    ///     .Subscribe(result => HandleSuccess(result),
    ///                error => HandleCircuitOpen(error));
    /// </code>
    /// </example>
    public static IObservable<T> WithCircuitBreaker<T>(
        this IObservable<T> source,
        int failureThreshold = 5,
        TimeSpan? recoveryTimeout = null,
        int successThreshold = 3)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(failureThreshold);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(successThreshold);

        var timeout = recoveryTimeout ?? TimeSpan.FromSeconds(30);

        return Observable.Create<T>(observer =>
        {
            var circuitState = CircuitState.Closed;
            var failureCount = 0;
            var successCount = 0;
            var lastFailureTime = DateTime.MinValue;
            var gate = new object();

            return source.Subscribe(
                onNext: value =>
                {
                    lock (gate)
                    {
                        if (circuitState == CircuitState.HalfOpen)
                        {
                            successCount++;
                            if (successCount >= successThreshold)
                            {
                                circuitState = CircuitState.Closed;
                                failureCount = 0;
                                successCount = 0;
                            }
                        }
                        else if (circuitState == CircuitState.Closed)
                        {
                            failureCount = 0;
                        }
                    }
                    observer.OnNext(value);
                },
                onError: error =>
                {
                    lock (gate)
                    {
                        failureCount++;
                        if (failureCount >= failureThreshold)
                        {
                            circuitState = CircuitState.Open;
                            lastFailureTime = DateTime.UtcNow;
                        }
                        else if (circuitState == CircuitState.HalfOpen)
                        {
                            circuitState = CircuitState.Open;
                            lastFailureTime = DateTime.UtcNow;
                            successCount = 0;
                        }
                    }

                    if (circuitState == CircuitState.Open)
                    {
                        var circuitOpenException = new CircuitOpenException(
                            $"Circuit breaker is open. Last failure: {lastFailureTime}, Timeout: {timeout}",
                            error);
                        observer.OnError(circuitOpenException);
                    }
                    else
                    {
                        observer.OnError(error);
                    }
                },
                onCompleted: observer.OnCompleted);
        });
    }

    #endregion

    #region Timeout Handling

    /// <summary>
    /// Applies a timeout to the observable sequence with a custom error message.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="timeoutMessage">Custom message for timeout exceptions.</param>
    /// <returns>An observable that times out with a custom message.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Query&lt;Data[]&gt;("data:fetch", parameters)
    ///     .TimeoutWithMessage(TimeSpan.FromSeconds(10), "Data fetch timed out")
    ///     .Subscribe(data => ProcessData(data));
    /// </code>
    /// </example>
    public static IObservable<T> TimeoutWithMessage<T>(
        this IObservable<T> source,
        TimeSpan timeout,
        string timeoutMessage)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(timeoutMessage);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        return source.Timeout(timeout, Observable.Throw<T>(
            new TimeoutException(timeoutMessage)));
    }

    #endregion

    #region Error Reporting and Monitoring

    /// <summary>
    /// Catches exceptions and reports them to a monitoring system while continuing the stream.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="errorReporter">Function to report errors (e.g., to logging or monitoring system).</param>
    /// <param name="continueOnError">Whether to continue the stream after reporting errors (default: true).</param>
    /// <returns>An observable that reports errors but may continue.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Observe&lt;Update&gt;("updates:stream")
    ///     .CatchAndReport(ex => logger.LogError(ex, "Update stream error"))
    ///     .Subscribe(update => ProcessUpdate(update));
    /// </code>
    /// </example>
    public static IObservable<T> CatchAndReport<T>(
        this IObservable<T> source,
        Action<Exception> errorReporter,
        bool continueOnError = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(errorReporter);

        return source.Catch<T, Exception>(ex =>
        {
            errorReporter(ex);
            return continueOnError ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        });
    }

    /// <summary>
    /// Catches exceptions and reports them with additional context information.
    /// </summary>
    /// <typeparam name="T">The type of the observable sequence.</typeparam>
    /// <typeparam name="TContext">The type of the context information.</typeparam>
    /// <param name="source">The source observable.</param>
    /// <param name="contextProvider">Function to provide additional context for error reporting.</param>
    /// <param name="errorReporter">Function to report errors with context.</param>
    /// <param name="continueOnError">Whether to continue the stream after reporting errors (default: true).</param>
    /// <returns>An observable that reports contextual errors.</returns>
    /// <example>
    /// <code>
    /// var subscription = client.Query&lt;User&gt;("users:get", new { id: userId })
    ///     .CatchAndReport(
    ///         () => new { UserId = userId, Timestamp = DateTime.UtcNow },
    ///         (ex, context) => logger.LogError(ex, "Failed to get user {UserId} at {Timestamp}", context.UserId, context.Timestamp))
    ///     .Subscribe(user => DisplayUser(user));
    /// </code>
    /// </example>
    public static IObservable<T> CatchAndReport<T, TContext>(
        this IObservable<T> source,
        Func<TContext> contextProvider,
        Action<Exception, TContext> errorReporter,
        bool continueOnError = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(contextProvider);
        ArgumentNullException.ThrowIfNull(errorReporter);

        return source.Catch<T, Exception>(ex =>
        {
            var context = contextProvider();
            errorReporter(ex, context);
            return continueOnError ? Observable.Empty<T>() : Observable.Throw<T>(ex);
        });
    }

    #endregion

    #region Circuit Breaker State

    private enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Exception thrown when the circuit breaker is open.
    /// </summary>
    public class CircuitOpenException(string message, Exception innerException) : Exception(message, innerException)
    {
    }

    #endregion
}
