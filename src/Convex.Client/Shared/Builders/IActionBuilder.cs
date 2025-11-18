using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Shared.Resilience;

namespace Convex.Client.Shared.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex actions.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the action.</typeparam>
public interface IActionBuilder<TResult>
{
    /// <summary>
    /// Sets the arguments to pass to the Convex function.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;

    /// <summary>
    /// Sets the arguments using a builder function for type-safe construction.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="configure">A function that configures the arguments.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();

    /// <summary>
    /// Sets a timeout for the action execution.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Registers a callback to invoke when the action succeeds.
    /// </summary>
    /// <param name="onSuccess">The success callback that receives the server result.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess);

    /// <summary>
    /// Registers a callback to invoke when the action fails.
    /// </summary>
    /// <param name="onError">The error callback that receives the exception.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> OnError(Action<Exception> onError);

    /// <summary>
    /// Configures a retry policy for the action.
    /// If the action fails, it will be retried according to the configured policy.
    /// </summary>
    /// <param name="configure">A function to configure the retry policy.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);

    /// <summary>
    /// Uses a predefined retry policy for the action.
    /// </summary>
    /// <param name="policy">The retry policy to use.</param>
    /// <returns>The builder for method chaining.</returns>
    IActionBuilder<TResult> WithRetry(RetryPolicy policy);

    /// <summary>
    /// Executes the action and returns the result.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with the action result.</returns>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the action and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a Result containing either the action result or an error.</returns>
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);
}

