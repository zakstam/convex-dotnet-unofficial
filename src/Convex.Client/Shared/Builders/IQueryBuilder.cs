using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Resilience;

namespace Convex.Client.Shared.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex queries.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query.</typeparam>
public interface IQueryBuilder<TResult>
{
    /// <summary>
    /// Sets the arguments to pass to the Convex function.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;

    /// <summary>
    /// Sets the arguments using a builder function for type-safe construction.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="configure">A function that configures the arguments.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();

    /// <summary>
    /// Sets a timeout for the query execution.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Includes metadata (log lines, execution time, etc.) in the result.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> IncludeMetadata();

    /// <summary>
    /// Executes the query with consistent read semantics at the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to use for consistent reads.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This API is experimental and may change or disappear in future versions.
    /// </remarks>
    [Obsolete("This API is experimental: it may change or disappear. Use standard queries instead.")]
    IQueryBuilder<TResult> UseConsistency(long timestamp);

    /// <summary>
    /// Caches the query result for the specified duration.
    /// Subsequent calls within the cache window will return the cached value.
    /// </summary>
    /// <param name="cacheDuration">How long to cache the result.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> Cached(TimeSpan cacheDuration);

    /// <summary>
    /// Registers a callback to invoke if the query fails.
    /// </summary>
    /// <param name="onError">The error callback.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> OnError(Action<Exception> onError);

    /// <summary>
    /// Configures a retry policy for the query.
    /// If the query fails, it will be retried according to the configured policy.
    /// </summary>
    /// <param name="configure">A function to configure the retry policy.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);

    /// <summary>
    /// Uses a predefined retry policy for the query.
    /// </summary>
    /// <param name="policy">The retry policy to use.</param>
    /// <returns>The builder for method chaining.</returns>
    IQueryBuilder<TResult> WithRetry(RetryPolicy policy);

    /// <summary>
    /// Executes the query and returns the result.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with the query result.</returns>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a Result containing either the query result or an error.</returns>
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);
}

