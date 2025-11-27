using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex actions.
/// Actions are server-side operations that can perform side effects like calling external APIs,
/// sending emails, or other operations that aren't pure database operations.
/// Unlike queries and mutations, actions can access external resources and have longer execution times.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the action. This should match the return type of your Convex action function.</typeparam>
/// <remarks>
/// <para>
/// The builder pattern allows you to configure actions fluently before execution.
/// All configuration methods return the builder instance for method chaining.
/// </para>
/// <para>
/// Actions are useful for:
/// <list type="bullet">
/// <item>Calling external APIs</item>
/// <item>Sending emails or notifications</item>
/// <item>Performing file operations</item>
/// <item>Any operation that requires side effects</item>
/// </list>
/// </para>
/// <para>
/// Actions typically have longer timeouts than queries/mutations since they may call external services.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple action call
/// var result = await client.Action&lt;string&gt;("functions/sendEmail")
///     .WithArgs(new { to = "user@example.com", subject = "Hello", body = "Welcome!" })
///     .ExecuteAsync();
///
/// // Action with longer timeout for external API calls
/// var apiResult = await client.Action&lt;ApiResponse&gt;("functions/callExternalApi")
///     .WithArgs(new { endpoint = "https://api.example.com/data" })
///     .WithTimeout(TimeSpan.FromSeconds(60))
///     .ExecuteAsync();
/// </code>
/// </example>
/// <seealso cref="Convex.Client.IConvexClient.Action{TResult}(string)"/>
/// <seealso cref="IQueryBuilder{TResult}"/>
/// <seealso cref="IMutationBuilder{TResult}"/>
public interface IActionBuilder<TResult>
{
    /// <summary>
    /// Sets the arguments to pass to the Convex function.
    /// Arguments are serialized to JSON and sent to the Convex backend.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object. Can be an anonymous type, class, record, or struct.</typeparam>
    /// <param name="args">The arguments to pass to the function. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Using anonymous type
    /// var result = await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello", body = "Welcome!" })
    ///     .ExecuteAsync();
    ///
    /// // Using a class
    /// var apiCallArgs = new ApiCallArgs { Endpoint = "https://api.example.com/data", Method = "GET" };
    /// var response = await client.Action&lt;ApiResponse&gt;("functions/callExternalApi")
    ///     .WithArgs(apiCallArgs)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
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
    /// Overrides the default timeout set on the client. Actions typically need longer timeouts than queries/mutations
    /// since they may call external APIs or perform time-consuming operations.
    /// </summary>
    /// <param name="timeout">The timeout duration. Must be greater than zero.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero.</exception>
    /// <remarks>
    /// Actions that call external APIs or perform complex operations may take longer to complete.
    /// Use longer timeouts (e.g., 30-60 seconds) for such actions.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Action with longer timeout for external API call
    /// var result = await client.Action&lt;ApiResponse&gt;("functions/callExternalApi")
    ///     .WithArgs(new { endpoint = "https://api.example.com/data" })
    ///     .WithTimeout(TimeSpan.FromSeconds(60))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IActionBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Registers a callback to invoke when the action succeeds.
    /// The callback receives the result returned by the action.
    /// </summary>
    /// <param name="onSuccess">The success callback that receives the server result. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onSuccess"/> is null.</exception>
    /// <remarks>
    /// The success callback is invoked after the action completes successfully, before the result is returned.
    /// Use this for logging, UI updates, or other side effects that should happen on success.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Action with success callback
    /// await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello" })
    ///     .OnSuccess(messageId => {
    ///         Console.WriteLine($"Email sent successfully: {messageId}");
    ///         ShowSuccessNotification();
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IActionBuilder<TResult> OnSuccess(Action<TResult> onSuccess);

    /// <summary>
    /// Registers a callback to invoke when the action fails.
    /// The callback receives the exception that caused the failure.
    /// </summary>
    /// <param name="onError">The error callback that receives the exception. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="onError"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The error callback is invoked before the exception is thrown. If you want to handle errors
    /// without exceptions, use <see cref="ExecuteWithResultAsync(CancellationToken)"/> instead.
    /// </para>
    /// <para>
    /// Common error types include:
    /// <list type="bullet">
    /// <item><see cref="ConvexException"/> - Convex-specific errors</item>
    /// <item><see cref="TimeoutException"/> - Action timed out</item>
    /// <item><see cref="HttpRequestException"/> - Network errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Action with error handling
    /// await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello" })
    ///     .OnError(ex => {
    ///         Console.WriteLine($"Failed to send email: {ex.Message}");
    ///         ShowErrorNotification("Email failed to send");
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteWithResultAsync(CancellationToken)"/>
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
    /// This is the final step in the action builder pattern - call this to execute the configured action.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the action operation.</param>
    /// <returns>A task that completes with the action result of type <typeparamref name="TResult"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when the action fails. Use <see cref="OnError(Action{Exception})"/> to handle errors, or <see cref="ExecuteWithResultAsync(CancellationToken)"/> to avoid exceptions.</exception>
    /// <remarks>
    /// <para>
    /// This method executes the action immediately and returns the result. If the action fails,
    /// an exception is thrown. To handle errors without exceptions, use <see cref="ExecuteWithResultAsync(CancellationToken)"/>.
    /// </para>
    /// <para>
    /// Actions may take longer to complete than queries or mutations since they can call external APIs
    /// or perform time-consuming operations. Consider using longer timeouts for such actions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute action
    /// var messageId = await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello", body = "Welcome!" })
    ///     .ExecuteAsync();
    ///
    /// // With cancellation support
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    /// try
    /// {
    ///     var result = await client.Action&lt;ApiResponse&gt;("functions/callExternalApi")
    ///         .WithArgs(new { endpoint = "https://api.example.com/data" })
    ///         .ExecuteAsync(cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     Console.WriteLine("Action was cancelled");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteWithResultAsync(CancellationToken)"/>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the action and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// Use this when you prefer functional error handling over exceptions.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the action operation.</param>
    /// <returns>A task that completes with a <see cref="ConvexResult{TResult}"/> containing either the action result or an error.</returns>
    /// <remarks>
    /// <para>
    /// The result object has an <see cref="ConvexResult{TResult}.IsSuccess"/> property to check if the action succeeded.
    /// If successful, access the value via <see cref="ConvexResult{TResult}.Value"/>.
    /// If failed, access the error via <see cref="ConvexResult{TResult}.Error"/>.
    /// </para>
    /// <para>
    /// This is useful when you want to handle errors without try-catch blocks, or when you want
    /// to chain multiple operations together functionally.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute with result handling
    /// var result = await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com", subject = "Hello" })
    ///     .ExecuteWithResultAsync();
    ///
    /// if (result.IsSuccess)
    /// {
    ///     var messageId = result.Value;
    ///     Console.WriteLine($"Email sent: {messageId}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Action failed: {result.Error.Message}");
    /// }
    ///
    /// // Functional chaining
    /// var messageId = await client.Action&lt;string&gt;("functions/sendEmail")
    ///     .WithArgs(new { to = "user@example.com" })
    ///     .ExecuteWithResultAsync()
    ///     .GetValueOrDefault("unknown");
    /// </code>
    /// </example>
    /// <seealso cref="ConvexResult{TResult}"/>
    /// <seealso cref="ExecuteAsync(CancellationToken)"/>
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);
}

