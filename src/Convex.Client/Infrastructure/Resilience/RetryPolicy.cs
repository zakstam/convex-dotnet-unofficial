using System.Net;
using Convex.Client.Infrastructure.ErrorHandling;

namespace Convex.Client.Infrastructure.Resilience;

/// <summary>
/// Defines a retry policy for failed operations.
/// Provides fluent API for configuring retry behavior including backoff strategies,
/// exception filtering, and retry callbacks.
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; internal set; }

    /// <summary>
    /// Gets the backoff strategy to use between retries.
    /// </summary>
    public BackoffStrategy BackoffStrategy { get; internal set; }

    /// <summary>
    /// Gets the initial delay for the backoff strategy.
    /// </summary>
    public TimeSpan InitialDelay { get; internal set; }

    /// <summary>
    /// Gets the backoff multiplier (for exponential backoff).
    /// </summary>
    public double BackoffMultiplier { get; internal set; }

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    public TimeSpan? MaxDelay { get; internal set; }

    /// <summary>
    /// Gets whether to use jitter (random variance) in delay calculations to prevent thundering herd.
    /// </summary>
    public bool UseJitter { get; internal set; }

    /// <summary>
    /// Gets the set of exception types that should trigger a retry.
    /// If empty, uses default transient exception detection logic.
    /// </summary>
    public HashSet<Type> RetryableExceptionTypes { get; }

    /// <summary>
    /// Gets the callback to invoke before each retry attempt.
    /// Parameters: (attemptNumber, exception, delayBeforeRetry)
    /// </summary>
    public Action<int, Exception, TimeSpan>? OnRetryCallback { get; internal set; }

    internal RetryPolicy()
    {
        MaxRetries = 0;
        BackoffStrategy = BackoffStrategy.Exponential;
        InitialDelay = TimeSpan.FromSeconds(1);
        BackoffMultiplier = 2.0;
        MaxDelay = TimeSpan.FromSeconds(30);
        UseJitter = true;
        RetryableExceptionTypes = [];
    }

    /// <summary>
    /// Gets a default retry policy with 3 retries and exponential backoff with jitter.
    /// </summary>
    public static RetryPolicy Default()
    {
        return new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(100), multiplier: 2.0, useJitter: true)
            .WithMaxDelay(TimeSpan.FromSeconds(30))
            .Build();
    }

    /// <summary>
    /// Gets an aggressive retry policy with 5 retries and faster backoff.
    /// </summary>
    public static RetryPolicy Aggressive()
    {
        return new RetryPolicyBuilder()
            .MaxRetries(5)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(500), multiplier: 1.5, useJitter: true)
            .WithMaxDelay(TimeSpan.FromSeconds(10))
            .Build();
    }

    /// <summary>
    /// Gets a conservative retry policy with 2 retries and longer delays.
    /// </summary>
    public static RetryPolicy Conservative()
    {
        return new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromSeconds(2), multiplier: 3.0, useJitter: true)
            .WithMaxDelay(TimeSpan.FromMinutes(1))
            .Build();
    }

    /// <summary>
    /// Gets a retry policy with no retries (fail immediately).
    /// </summary>
    public static RetryPolicy None()
    {
        return new RetryPolicyBuilder()
            .MaxRetries(0)
            .Build();
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The retry attempt number (1-based).</param>
    /// <returns>The delay to wait before retrying.</returns>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");
        }

        var delay = BackoffStrategy switch
        {
            BackoffStrategy.Constant => InitialDelay,
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * attemptNumber),
            BackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber - 1)),
            _ => InitialDelay
        };

        // Apply max delay cap if specified
        if (MaxDelay.HasValue && delay > MaxDelay.Value)
        {
            delay = MaxDelay.Value;
        }

        // Apply jitter if enabled (prevents thundering herd)
        if (UseJitter)
        {
            var jitterRange = delay.TotalMilliseconds * 0.25; // ±25% jitter
            var random = new Random();
            var jitterMs = (random.NextDouble() - 0.5) * 2 * jitterRange;
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitterMs);
        }

        return delay;
    }

    /// <summary>
    /// Determines whether the given exception should trigger a retry.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the exception should trigger a retry, false otherwise.</returns>
    public bool ShouldRetry(Exception exception)
    {
        // If specific exception types are configured, only retry those types
        if (RetryableExceptionTypes.Count > 0)
        {
            var exceptionType = exception.GetType();
            return RetryableExceptionTypes.Any(type => type.IsAssignableFrom(exceptionType));
        }

        // Default: Use intelligent transient exception detection
        return IsTransientException(exception);
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception switch
        {
            ConvexNetworkException networkEx => IsTransientNetworkError(networkEx),
            TaskCanceledException => true, // Timeout - usually transient
            HttpRequestException => true, // Network issues - usually transient
            ConvexFunctionException => false, // Function errors - not transient
            ConvexArgumentException => false, // Argument errors - not transient
            ConvexAuthenticationException => false, // Auth errors - not transient
            _ => false // Unknown errors - don't retry by default
        };
    }

    private static bool IsTransientNetworkError(ConvexNetworkException networkEx)
    {
        return networkEx.ErrorType switch
        {
            NetworkErrorType.Timeout => true,
            NetworkErrorType.ConnectionFailure => true,
            NetworkErrorType.DnsResolution => false, // DNS issues are usually persistent
            NetworkErrorType.SslCertificate => false, // SSL issues are usually persistent
            NetworkErrorType.ServerError => IsTransientHttpStatus(networkEx.StatusCode),
            _ => false
        };
    }

    private static bool IsTransientHttpStatus(HttpStatusCode? statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => true, // 500
            HttpStatusCode.BadGateway => true, // 502
            HttpStatusCode.ServiceUnavailable => true, // 503
            HttpStatusCode.GatewayTimeout => true, // 504
            HttpStatusCode.TooManyRequests => true, // 429 - rate limit
            _ => false
        };
    }
}

/// <summary>
/// Defines the backoff strategy for retry delays.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>
    /// Use a constant delay between retries.
    /// </summary>
    Constant,

    /// <summary>
    /// Use linear backoff (delay increases linearly with attempt number).
    /// </summary>
    Linear,

    /// <summary>
    /// Use exponential backoff (delay doubles with each attempt).
    /// </summary>
    Exponential
}

/// <summary>
/// Fluent builder for creating retry policies.
/// </summary>
public sealed class RetryPolicyBuilder
{
    private readonly RetryPolicy _policy;

    /// <summary>
    /// Creates a new retry policy builder.
    /// </summary>
    public RetryPolicyBuilder() => _policy = new RetryPolicy();

    /// <summary>
    /// Sets the maximum number of retry attempts.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries (0 to disable retries).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder MaxRetries(int maxRetries)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Max retries cannot be negative.");
        }

        _policy.MaxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Configures exponential backoff with the specified initial delay and multiplier.
    /// </summary>
    /// <param name="initialDelay">The initial delay before the first retry.</param>
    /// <param name="multiplier">The multiplier for exponential growth (default 2.0).</param>
    /// <param name="useJitter">Whether to add random jitter to prevent thundering herd (default: true).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder ExponentialBackoff(TimeSpan initialDelay, double multiplier = 2.0, bool useJitter = true)
    {
        if (initialDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be positive.");
        }

        if (multiplier <= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be greater than 1.0.");
        }

        _policy.BackoffStrategy = BackoffStrategy.Exponential;
        _policy.InitialDelay = initialDelay;
        _policy.BackoffMultiplier = multiplier;
        _policy.UseJitter = useJitter;
        return this;
    }

    /// <summary>
    /// Configures linear backoff with the specified initial delay.
    /// </summary>
    /// <param name="initialDelay">The base delay that will be multiplied by attempt number.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder LinearBackoff(TimeSpan initialDelay)
    {
        if (initialDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial delay must be positive.");
        }

        _policy.BackoffStrategy = BackoffStrategy.Linear;
        _policy.InitialDelay = initialDelay;
        return this;
    }

    /// <summary>
    /// Configures constant backoff with the specified delay.
    /// </summary>
    /// <param name="delay">The constant delay between retries.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder ConstantBackoff(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be positive.");
        }

        _policy.BackoffStrategy = BackoffStrategy.Constant;
        _policy.InitialDelay = delay;
        return this;
    }

    /// <summary>
    /// Sets the maximum delay between retries (caps the backoff).
    /// </summary>
    /// <param name="maxDelay">The maximum delay to wait between retries.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder WithMaxDelay(TimeSpan maxDelay)
    {
        if (maxDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be positive.");
        }

        _policy.MaxDelay = maxDelay;
        return this;
    }

    /// <summary>
    /// Configures the policy to only retry on specific exception types.
    /// </summary>
    /// <typeparam name="TException">The exception type to retry on.</typeparam>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder RetryOn<TException>() where TException : Exception
    {
        _ = _policy.RetryableExceptionTypes.Add(typeof(TException));
        return this;
    }

    /// <summary>
    /// Configures a callback to invoke before each retry attempt.
    /// </summary>
    /// <param name="callback">
    /// The callback to invoke. Parameters: (attemptNumber, exception, delayBeforeRetry).
    /// </param>
    /// <returns>This builder for fluent chaining.</returns>
    public RetryPolicyBuilder OnRetry(Action<int, Exception, TimeSpan> callback)
    {
        _policy.OnRetryCallback = callback ?? throw new ArgumentNullException(nameof(callback));
        return this;
    }

    /// <summary>
    /// Builds the configured retry policy.
    /// </summary>
    /// <returns>The configured retry policy.</returns>
    public RetryPolicy Build() => _policy;
}
