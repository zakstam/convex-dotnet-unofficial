using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex queries.
/// Queries are read-only operations that fetch data from your Convex backend.
/// Use this builder to configure arguments, timeouts, retry policies, and error handling.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query. This should match the return type of your Convex function.</typeparam>
/// <remarks>
/// <para>
/// The builder pattern allows you to configure queries fluently before execution.
/// All configuration methods return the builder instance for method chaining.
/// </para>
    /// <para>
    /// Queries are executed via HTTP and return data synchronously. For real-time updates,
    /// use <see cref="Convex.Client.IConvexClient.Observe{T}(string)"/> instead.
    /// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple query
/// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
///     .ExecuteAsync();
///
/// // Query with arguments
/// var user = await client.Query&lt;User&gt;("functions/getUser")
///     .WithArgs(new { userId = "user123" })
///     .ExecuteAsync();
///
/// // Query with timeout and error handling
/// var result = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
///     .WithArgs(new { query = "laptop" })
///     .WithTimeout(TimeSpan.FromSeconds(5))
///     .OnError(ex => Console.WriteLine($"Query failed: {ex.Message}"))
///     .ExecuteAsync();
/// </code>
/// </example>
/// <seealso cref="Convex.Client.IConvexClient.Query{TResult}(string)"/>
/// <seealso cref="IMutationBuilder{TResult}"/>
/// <seealso cref="IActionBuilder{TResult}"/>
public interface IQueryBuilder<TResult>
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
    /// var user = await client.Query&lt;User&gt;("functions/getUser")
    ///     .WithArgs(new { userId = "user123", includeProfile = true })
    ///     .ExecuteAsync();
    ///
    /// // Using a class
    /// var searchArgs = new SearchArgs { Query = "laptop", Limit = 10 };
    /// var products = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
    ///     .WithArgs(searchArgs)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IQueryBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;

    /// <summary>
    /// Sets the arguments using a builder function for type-safe construction.
    /// Useful when you need to conditionally set arguments or when the arguments type has many properties.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object. Must be a class with a parameterless constructor.</typeparam>
    /// <param name="configure">A function that configures the arguments object. The object is created automatically and passed to this function.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Configure arguments with a builder function
    /// var products = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
    ///     .WithArgs&lt;SearchArgs&gt;(args => {
    ///         args.Query = "laptop";
    ///         args.Limit = 10;
    ///         args.SortBy = "price";
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IQueryBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();

    /// <summary>
    /// Sets a timeout for the query execution.
    /// Overrides the default timeout set on the client. The query will fail if it doesn't complete within this time.
    /// </summary>
    /// <param name="timeout">The timeout duration. Must be greater than zero.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero.</exception>
    /// <remarks>
    /// Use shorter timeouts for fast queries and longer timeouts for queries that may take time
    /// (e.g., complex aggregations or queries that process large datasets).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query with custom timeout
    /// var result = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
    ///     .WithArgs(new { query = "laptop" })
    ///     .WithTimeout(TimeSpan.FromSeconds(10))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IQueryBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Includes metadata (log lines, execution time, etc.) in the result.
    /// When enabled, the result will include additional information about query execution.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// Metadata includes information such as:
    /// <list type="bullet">
    /// <item>Execution time</item>
    /// <item>Log lines from the Convex function</item>
    /// <item>Other diagnostic information</item>
    /// </list>
    /// This is useful for debugging and performance monitoring.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query with metadata
    /// var result = await client.Query&lt;QueryResultWithMetadata&lt;List&lt;Todo&gt;&gt;&gt;("functions/listTodos")
    ///     .IncludeMetadata()
    ///     .ExecuteAsync();
    ///
    /// Console.WriteLine($"Execution time: {result.Metadata.ExecutionTimeMs}ms");
    /// Console.WriteLine($"Logs: {string.Join(", ", result.Metadata.Logs)}");
    /// </code>
    /// </example>
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
    /// Subsequent calls within the cache window will return the cached value without making a network request.
    /// </summary>
    /// <param name="cacheDuration">How long to cache the result. Must be greater than zero.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="cacheDuration"/> is less than or equal to zero.</exception>
    /// <remarks>
    /// <para>
    /// Caching is useful for queries that don't change frequently or when you want to reduce
    /// network requests. The cache is stored in memory and is specific to this query instance.
    /// </para>
    /// <para>
    /// Cache can be invalidated manually using <see cref="Convex.Client.IConvexClient.InvalidateQueryAsync(string)"/>
    /// or automatically when related mutations execute (if dependencies are configured).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Cache query results for 5 minutes
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Cached(TimeSpan.FromMinutes(5))
    ///     .ExecuteAsync();
    ///
    /// // Subsequent calls within 5 minutes return cached value
    /// var cachedTodos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .Cached(TimeSpan.FromMinutes(5))
    ///     .ExecuteAsync(); // Returns cached value, no network request
    /// </code>
    /// </example>
    /// <seealso cref="Convex.Client.IConvexClient.InvalidateQueryAsync(string)"/>
    IQueryBuilder<TResult> Cached(TimeSpan cacheDuration);

    /// <summary>
    /// Registers a callback to invoke if the query fails.
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
    /// <item><see cref="TimeoutException"/> - Query timed out</item>
    /// <item><see cref="HttpRequestException"/> - Network errors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query with error handling
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .OnError(ex => {
    ///         Console.WriteLine($"Query failed: {ex.Message}");
    ///         // Log error, show user-friendly message, etc.
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteWithResultAsync(CancellationToken)"/>
    IQueryBuilder<TResult> OnError(Action<Exception> onError);

    /// <summary>
    /// Configures a retry policy for the query.
    /// If the query fails, it will be retried according to the configured policy.
    /// Useful for handling transient network errors or temporary server issues.
    /// </summary>
    /// <param name="configure">A function to configure the retry policy using <see cref="RetryPolicyBuilder"/>. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Retry policies can be configured with:
    /// <list type="bullet">
    /// <item>Maximum number of retries</item>
    /// <item>Backoff strategy (exponential, linear, constant)</item>
    /// <item>Exception types to retry on</item>
    /// <item>Maximum delay between retries</item>
    /// </list>
    /// </para>
    /// <para>
    /// By default, retries use exponential backoff with jitter to avoid thundering herd problems.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Query with custom retry policy
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .WithRetry(policy => {
    ///         policy.MaxRetries(3)
    ///                .ExponentialBackoff(TimeSpan.FromSeconds(1))
    ///                .RetryOn&lt;HttpRequestException&gt;();
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="RetryPolicyBuilder"/>
    /// <seealso cref="WithRetry(RetryPolicy)"/>
    IQueryBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);

    /// <summary>
    /// Uses a predefined retry policy for the query.
    /// Useful when you want to reuse the same retry policy across multiple queries.
    /// </summary>
    /// <param name="policy">The retry policy to use. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <remarks>
    /// Predefined policies can be created using <see cref="RetryPolicy.Default()"/>,
    /// <see cref="RetryPolicy.Aggressive()"/>, <see cref="RetryPolicy.Conservative()"/>, etc.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Reuse a retry policy
    /// var retryPolicy = RetryPolicy.Default();
    ///
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .WithRetry(retryPolicy)
    ///     .ExecuteAsync();
    ///
    /// var users = await client.Query&lt;List&lt;User&gt;&gt;("functions/listUsers")
    ///     .WithRetry(retryPolicy)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="RetryPolicy"/>
    /// <seealso cref="WithRetry(Action{RetryPolicyBuilder})"/>
    IQueryBuilder<TResult> WithRetry(RetryPolicy policy);

    /// <summary>
    /// Executes the query and returns the result.
    /// This is the final step in the query builder pattern - call this to execute the configured query.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the query operation.</param>
    /// <returns>A task that completes with the query result of type <typeparamref name="TResult"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when the query fails. Use <see cref="OnError(Action{Exception})"/> to handle errors, or <see cref="ExecuteWithResultAsync(CancellationToken)"/> to avoid exceptions.</exception>
    /// <remarks>
    /// <para>
    /// This method executes the query immediately and returns the result. If the query fails,
    /// an exception is thrown. To handle errors without exceptions, use <see cref="ExecuteWithResultAsync(CancellationToken)"/>.
    /// </para>
    /// <para>
    /// The query is executed via HTTP and returns data synchronously. For real-time updates,
    /// use <see cref="Convex.Client.IConvexClient.Observe{T}(string)"/> instead.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute query
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteAsync();
    ///
    /// // With cancellation support
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    /// try
    /// {
    ///     var result = await client.Query&lt;List&lt;Product&gt;&gt;("functions/searchProducts")
    ///         .WithArgs(new { query = "laptop" })
    ///         .ExecuteAsync(cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     Console.WriteLine("Query was cancelled");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteWithResultAsync(CancellationToken)"/>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// Use this when you prefer functional error handling over exceptions.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the query operation.</param>
    /// <returns>A task that completes with a <see cref="ConvexResult{TResult}"/> containing either the query result or an error.</returns>
    /// <remarks>
    /// <para>
    /// The result object has an <see cref="ConvexResult{TResult}.IsSuccess"/> property to check if the query succeeded.
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
    /// var result = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteWithResultAsync();
    ///
    /// if (result.IsSuccess)
    /// {
    ///     var todos = result.Value;
    ///     Console.WriteLine($"Found {todos.Count} todos");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Query failed: {result.Error.Message}");
    /// }
    ///
    /// // Functional chaining
    /// var todos = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteWithResultAsync()
    ///     .GetValueOrDefault(new List&lt;Todo&gt;());
    /// </code>
    /// </example>
    /// <seealso cref="ConvexResult{TResult}"/>
    /// <seealso cref="ExecuteAsync(CancellationToken)"/>
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);
}

