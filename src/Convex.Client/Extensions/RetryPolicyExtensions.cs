using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Extensions;

/// <summary>
/// Extension methods for simplified retry policy configuration on builders.
/// Provides convenient shortcuts for common retry scenarios.
/// </summary>
public static class RetryPolicyExtensions
{
    #region Mutation Builder Extensions

    /// <summary>
    /// Adds a standard retry policy (3 retries with exponential backoff).
    /// Convenient shortcut for common retry scenarios.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
    /// <param name="builder">The mutation builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMutationBuilder<TResult> WithStandardRetry<TResult>(
        this IMutationBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Default());

    /// <summary>
    /// Adds an aggressive retry policy (5 retries with faster backoff).
    /// Useful for operations where immediate success is critical.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
    /// <param name="builder">The mutation builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMutationBuilder<TResult> WithAggressiveRetry<TResult>(
        this IMutationBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Aggressive());

    /// <summary>
    /// Adds a conservative retry policy (2 retries with longer delays).
    /// Useful for non-critical operations or when avoiding server load.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
    /// <param name="builder">The mutation builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMutationBuilder<TResult> WithConservativeRetry<TResult>(
        this IMutationBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Conservative());

    /// <summary>
    /// Adds a simple retry policy with the specified number of retries.
    /// Uses exponential backoff by default, or constant backoff if specified.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
    /// <param name="builder">The mutation builder.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="exponential">Use exponential backoff (default: true). If false, uses constant 1 second delays.</param>
    /// <param name="initialDelaySeconds">Initial delay in seconds (default: 1 second).</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMutationBuilder<TResult> WithRetry<TResult>(
        this IMutationBuilder<TResult> builder,
        int maxRetries,
        bool exponential = true,
        double initialDelaySeconds = 1.0)
    {
        return builder.WithRetry(policy =>
        {
            _ = policy.MaxRetries(maxRetries);

            if (exponential)
            {
                _ = policy.ExponentialBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
            else
            {
                _ = policy.ConstantBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
        });
    }

    #endregion

    #region Action Builder Extensions

    /// <summary>
    /// Adds a standard retry policy (3 retries with exponential backoff).
    /// Convenient shortcut for common retry scenarios.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="builder">The action builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IActionBuilder<TResult> WithStandardRetry<TResult>(
        this IActionBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Default());

    /// <summary>
    /// Adds an aggressive retry policy (5 retries with faster backoff).
    /// Useful for operations where immediate success is critical.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="builder">The action builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IActionBuilder<TResult> WithAggressiveRetry<TResult>(
        this IActionBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Aggressive());

    /// <summary>
    /// Adds a conservative retry policy (2 retries with longer delays).
    /// Useful for non-critical operations or when avoiding server load.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="builder">The action builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IActionBuilder<TResult> WithConservativeRetry<TResult>(
        this IActionBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Conservative());

    /// <summary>
    /// Adds a simple retry policy with the specified number of retries.
    /// Uses exponential backoff by default, or constant backoff if specified.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="builder">The action builder.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="exponential">Use exponential backoff (default: true). If false, uses constant 1 second delays.</param>
    /// <param name="initialDelaySeconds">Initial delay in seconds (default: 1 second).</param>
    /// <returns>The builder for method chaining.</returns>
    public static IActionBuilder<TResult> WithRetry<TResult>(
        this IActionBuilder<TResult> builder,
        int maxRetries,
        bool exponential = true,
        double initialDelaySeconds = 1.0)
    {
        return builder.WithRetry(policy =>
        {
            _ = policy.MaxRetries(maxRetries);

            if (exponential)
            {
                _ = policy.ExponentialBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
            else
            {
                _ = policy.ConstantBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
        });
    }

    #endregion

    #region Query Builder Extensions

    /// <summary>
    /// Adds a standard retry policy (3 retries with exponential backoff).
    /// Convenient shortcut for common retry scenarios.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IQueryBuilder<TResult> WithStandardRetry<TResult>(
        this IQueryBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Default());

    /// <summary>
    /// Adds an aggressive retry policy (5 retries with faster backoff).
    /// Useful for operations where immediate success is critical.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IQueryBuilder<TResult> WithAggressiveRetry<TResult>(
        this IQueryBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Aggressive());

    /// <summary>
    /// Adds a conservative retry policy (2 retries with longer delays).
    /// Useful for non-critical operations or when avoiding server load.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IQueryBuilder<TResult> WithConservativeRetry<TResult>(
        this IQueryBuilder<TResult> builder) => builder.WithRetry(RetryPolicy.Conservative());

    /// <summary>
    /// Adds a simple retry policy with the specified number of retries.
    /// Uses exponential backoff by default, or constant backoff if specified.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="builder">The query builder.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="exponential">Use exponential backoff (default: true). If false, uses constant 1 second delays.</param>
    /// <param name="initialDelaySeconds">Initial delay in seconds (default: 1 second).</param>
    /// <returns>The builder for method chaining.</returns>
    public static IQueryBuilder<TResult> WithRetry<TResult>(
        this IQueryBuilder<TResult> builder,
        int maxRetries,
        bool exponential = true,
        double initialDelaySeconds = 1.0)
    {
        return builder.WithRetry(policy =>
        {
            _ = policy.MaxRetries(maxRetries);

            if (exponential)
            {
                _ = policy.ExponentialBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
            else
            {
                _ = policy.ConstantBackoff(TimeSpan.FromSeconds(initialDelaySeconds));
            }
        });
    }

    #endregion
}
