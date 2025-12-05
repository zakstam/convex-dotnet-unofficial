using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.OptimisticUpdates;
using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex mutations.
/// Mutations are write operations that modify data in your Convex backend.
/// Mutations are queued and executed sequentially to ensure ordering guarantees.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the mutation. This should match the return type of your Convex function.</typeparam>
/// <remarks>
/// <para>
/// The builder pattern allows you to configure mutations fluently before execution.
/// All configuration methods return the builder instance for method chaining.
/// </para>
/// <para>
/// Mutations support optimistic updates for instant UI feedback. If the server mutation fails,
/// optimistic updates are automatically rolled back.
/// </para>
/// <para>
/// Mutations are queued by default to ensure ordering guarantees. Use <see cref="SkipQueue"/>
/// to bypass the queue if ordering is not important.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple mutation
/// var newTodo = await client.Mutate&lt;Todo&gt;("functions/createTodo")
///     .WithArgs(new { text = "Learn Convex .NET", completed = false })
///     .ExecuteAsync();
///
/// // Mutation with optimistic update
/// await client.Mutate&lt;Todo&gt;("functions/updateTodo")
///     .WithArgs(new { id = "todo123", completed = true })
///     .Optimistic(result => {
///         // Update UI immediately
///         _todos.First(t => t.Id == result.Id).IsCompleted = true;
///     })
///     .ExecuteAsync();
/// </code>
/// </example>
/// <seealso cref="Convex.Client.IConvexClient.Mutate{TResult}(string)"/>
/// <seealso cref="IQueryBuilder{TResult}"/>
/// <seealso cref="IActionBuilder{TResult}"/>
public interface IMutationBuilder<TResult>
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
    /// var todo = await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///     .WithArgs(new { text = "Learn Convex", completed = false })
    ///     .ExecuteAsync();
    ///
    /// // Using a class
    /// var updateArgs = new UpdateTodoArgs { Id = "todo123", Completed = true };
    /// var updated = await client.Mutate&lt;Todo&gt;("functions/updateTodo")
    ///     .WithArgs(updateArgs)
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IMutationBuilder<TResult> WithArgs<TArgs>(TArgs args) where TArgs : notnull;

    /// <summary>
    /// Sets the arguments using a builder function for type-safe construction.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="configure">A function that configures the arguments.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new();

    /// <summary>
    /// Sets a timeout for the mutation execution.
    /// Overrides the default timeout set on the client. The mutation will fail if it doesn't complete within this time.
    /// </summary>
    /// <param name="timeout">The timeout duration. Must be greater than zero.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero.</exception>
    /// <remarks>
    /// Mutations typically complete quickly, but complex mutations may take longer.
    /// Use longer timeouts for mutations that process large amounts of data or perform complex operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mutation with custom timeout
    /// var result = await client.Mutate&lt;BatchResult&gt;("functions/batchUpdate")
    ///     .WithArgs(new { items = largeItemList })
    ///     .WithTimeout(TimeSpan.FromMinutes(2))
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IMutationBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Enables optimistic update with a custom value to use before the server responds.
    /// If the server mutation fails, the optimistic update is automatically rolled back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note: This uses a state-focused approach (operates on arbitrary state), which differs
    /// from convex-js's query-focused optimistic updates (operates on query results).
    /// Both patterns are functionally equivalent but use different API styles.
    /// </para>
    /// </remarks>
    /// <param name="optimisticValue">The value to use for the optimistic update.</param>
    /// <param name="apply">A callback that applies the optimistic value to the UI.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply);

    /// <summary>
    /// Enables optimistic update with automatic rollback on failure.
    /// Captures the current state before applying the update, and automatically restores it if the mutation fails.
    /// This is the recommended approach as it eliminates the need to manually implement rollback logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note: This uses a state-focused approach (operates on arbitrary state), which differs
    /// from convex-js's query-focused optimistic updates (operates on query results).
    /// Both patterns are functionally equivalent but use different API styles.
    /// </para>
    /// </remarks>
    /// <typeparam name="TState">The type of state to update optimistically.</typeparam>
    /// <param name="getter">Function to get the current state (called before update).</param>
    /// <param name="setter">Action to set the state (used for both apply and rollback).</param>
    /// <param name="update">Function to compute the optimistically updated state.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Example: Optimistically add an item to a collection with automatic rollback
    /// await client.Mutation("messages:send")
    ///     .WithArgs(new { text = "Hello!" })
    ///     .OptimisticWithAutoRollback(
    ///         getter: () => _messages,
    ///         setter: value => _messages = value,
    ///         update: messages => messages.Append(newMessage).ToList()
    ///     )
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IMutationBuilder<TResult> OptimisticWithAutoRollback<TState>(
        Func<TState> getter,
        Action<TState> setter,
        Func<TState, TState> update);

    /// <summary>
    /// Registers a rollback action to undo an optimistic update if the server mutation fails.
    /// Call this when using the value-based <see cref="Optimistic(TResult, Action{TResult})"/> variant.
    /// The rollback action is only called if the mutation fails after the optimistic update was applied.
    /// </summary>
    /// <param name="rollback">An action that restores the UI/model to its pre-optimistic state. Must not be null.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rollback"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// For automatic rollback without manual implementation, use <see cref="OptimisticWithAutoRollback{TState}"/> instead.
    /// </para>
    /// <para>
    /// The rollback action should restore the UI to exactly the state it was in before the optimistic update.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mutation with manual rollback
    /// var originalState = _todos.ToList();
    /// await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///     .WithArgs(new { text = "New todo" })
    ///     .Optimistic(result => {
    ///         _todos.Add(result); // Optimistic update
    ///     })
    ///     .WithRollback(() => {
    ///         _todos = originalState; // Restore original state on failure
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    /// <seealso cref="OptimisticWithAutoRollback{TState}"/>
    IMutationBuilder<TResult> WithRollback(Action rollback);

    /// <summary>
    /// Registers a callback to invoke when the mutation succeeds.
    /// </summary>
    /// <param name="onSuccess">The success callback that receives the server result.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> OnSuccess(Action<TResult> onSuccess);

    /// <summary>
    /// Registers a callback to invoke when the mutation fails.
    /// </summary>
    /// <param name="onError">The error callback that receives the exception.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> OnError(Action<Exception> onError);

    /// <summary>
    /// Specifies that optimistic updates should be rolled back only for specific exception types.
    /// By default, all errors cause rollback.
    /// </summary>
    /// <typeparam name="TException">The type of exception that should trigger rollback.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> RollbackOn<TException>() where TException : Exception;

    /// <summary>
    /// Bypasses the mutation queue, executing this mutation immediately.
    /// By default, mutations are queued and executed sequentially to ensure ordering guarantees.
    /// This matches convex-js behavior where mutations are queued by default.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this when ordering is not important and you want the mutation to execute immediately
    /// without waiting for other queued mutations to complete.
    /// </para>
    /// <para>
    /// Skipping the queue can improve perceived performance for mutations that don't depend on
    /// the order of execution, but may lead to race conditions if mutations depend on each other.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute mutation immediately without queuing
    /// await client.Mutate&lt;AnalyticsEvent&gt;("functions/trackEvent")
    ///     .WithArgs(new { eventName = "button_click", timestamp = DateTime.UtcNow })
    ///     .SkipQueue()
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IMutationBuilder<TResult> SkipQueue();

    /// <summary>
    /// Configures a retry policy for the mutation.
    /// If the mutation fails, it will be retried according to the configured policy.
    /// Note: Optimistic updates are NOT re-applied during retries to avoid duplicate UI updates.
    /// </summary>
    /// <param name="configure">A function to configure the retry policy.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> WithRetry(Action<RetryPolicyBuilder> configure);

    /// <summary>
    /// Uses a predefined retry policy for the mutation.
    /// </summary>
    /// <param name="policy">The retry policy to use.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> WithRetry(RetryPolicy policy);

    /// <summary>
    /// Executes the mutation and returns the result.
    /// This is the final step in the mutation builder pattern - call this to execute the configured mutation.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the mutation operation.</param>
    /// <returns>A task that completes with the mutation result of type <typeparamref name="TResult"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via <paramref name="cancellationToken"/>.</exception>
    /// <exception cref="ConvexException">Thrown when the mutation fails. Use <see cref="OnError(Action{Exception})"/> to handle errors, or <see cref="ExecuteWithResultAsync(CancellationToken)"/> to avoid exceptions.</exception>
    /// <remarks>
    /// <para>
    /// This method executes the mutation immediately (or queues it if not skipped). If the mutation fails,
    /// an exception is thrown and any optimistic updates are automatically rolled back.
    /// </para>
    /// <para>
    /// To handle errors without exceptions, use <see cref="ExecuteWithResultAsync(CancellationToken)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute mutation
    /// var newTodo = await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///     .WithArgs(new { text = "Learn Convex .NET" })
    ///     .ExecuteAsync();
    ///
    /// // With cancellation support
    /// using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    /// try
    /// {
    ///     var result = await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///         .WithArgs(new { text = "New todo" })
    ///         .ExecuteAsync(cts.Token);
    /// }
    /// catch (OperationCanceledException)
    /// {
    ///     Console.WriteLine("Mutation was cancelled");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ExecuteWithResultAsync(CancellationToken)"/>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the mutation and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// Note: Optimistic updates are still applied, but rollback is only triggered for exceptions.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token. Can be used to cancel the mutation operation.</param>
    /// <returns>A task that completes with a <see cref="ConvexResult{TResult}"/> containing either the mutation result or an error.</returns>
    /// <remarks>
    /// <para>
    /// The result object has an <see cref="ConvexResult{TResult}.IsSuccess"/> property to check if the mutation succeeded.
    /// If successful, access the value via <see cref="ConvexResult{TResult}.Value"/>.
    /// If failed, access the error via <see cref="ConvexResult{TResult}.Error"/>.
    /// </para>
    /// <para>
    /// Optimistic updates are still applied even when using this method. However, rollback only occurs
    /// if an exception would have been thrown (i.e., for error results, optimistic updates remain).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Execute with result handling
    /// var result = await client.Mutate&lt;Todo&gt;("functions/createTodo")
    ///     .WithArgs(new { text = "New todo" })
    ///     .ExecuteWithResultAsync();
    ///
    /// if (result.IsSuccess)
    /// {
    ///     var todo = result.Value;
    ///     Console.WriteLine($"Created todo: {todo.Text}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Mutation failed: {result.Error.Message}");
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="ConvexResult{TResult}"/>
    /// <seealso cref="ExecuteAsync(CancellationToken)"/>
    Task<ConvexResult<TResult>> ExecuteWithResultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables query-focused optimistic updates using an OptimisticLocalStore.
    /// This matches convex-js's query-focused optimistic update pattern.
    /// The update function receives a local store to read and modify query results,
    /// and the mutation arguments. The update is automatically rolled back if the mutation fails.
    /// </summary>
    /// <typeparam name="TArgs">The type of the mutation arguments.</typeparam>
    /// <param name="updateFn">A function that performs optimistic updates using the local store.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// This API allows you to update query results directly, matching convex-js's pattern.
    /// Query results should be treated as immutable - always create new copies when updating.
    /// </remarks>
    /// <example>
    /// <code>
    /// await client.Mutation&lt;Message&gt;("messages:send")
    ///     .WithArgs(new { text = "Hello!" })
    ///     .WithOptimisticUpdate((localStore, args) =>
    ///     {
    ///         var currentMessages = localStore.GetQuery&lt;List&lt;Message&gt;&gt;("messages:list") ?? new List&lt;Message&gt;();
    ///         var newMessage = new Message { Text = args.text, Id = Guid.NewGuid().ToString() };
    ///         localStore.SetQuery("messages:list", currentMessages.Append(newMessage).ToList());
    ///     })
    ///     .ExecuteAsync();
    /// </code>
    /// </example>
    IMutationBuilder<TResult> WithOptimisticUpdate<TArgs>(Action<IOptimisticLocalStore, TArgs> updateFn) where TArgs : notnull;

    /// <summary>
    /// Optimistically updates a cached query result before the mutation executes.
    /// If the mutation fails, the cache update is automatically rolled back.
    /// </summary>
    /// <typeparam name="TCache">The type of the cached query result.</typeparam>
    /// <param name="queryName">The name of the query to update (e.g., "todos:list").</param>
    /// <param name="updateFn">A function that transforms the cached value optimistically.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <remarks>
    /// The cache update happens immediately when the mutation is executed, before the server responds.
    /// If the mutation fails, the original cached value is restored automatically.
    /// If the query is not cached, the update function is not called.
    /// </remarks>
    IMutationBuilder<TResult> UpdateCache<TCache>(string queryName, Func<TCache, TCache> updateFn);

    /// <summary>
    /// Tracks a pending mutation in a collection, removing it when the mutation completes (success or error).
    /// Useful for preventing subscription updates from overwriting optimistic updates while mutations are pending.
    /// </summary>
    /// <param name="tracker">The collection to track the pending mutation in (e.g., HashSet&lt;string&gt;).</param>
    /// <param name="key">The key to track (e.g., messageId, userId).</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// private readonly HashSet&lt;string&gt; _pendingMutations = new();
    ///
    /// await client.Mutation("messages:send")
    ///     .WithArgs(new { text = "Hello" })
    ///     .TrackPending(_pendingMutations, messageId)
    ///     .ExecuteAsync();
    /// // messageId is automatically removed from _pendingMutations when mutation completes
    /// </code>
    /// </example>
    IMutationBuilder<TResult> TrackPending(ISet<string> tracker, string key);

    /// <summary>
    /// Registers a cleanup action to execute when the mutation completes (success or error).
    /// Useful for common cleanup patterns like removing from pending sets, disposing resources, etc.
    /// </summary>
    /// <param name="cleanup">The cleanup action to execute.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <example>
    /// <code>
    /// await client.Mutation("messages:send")
    ///     .WithArgs(new { text = "Hello" })
    ///     .WithCleanup(() => _pendingMutations.Remove(messageId))
    ///     .ExecuteAsync();
    /// // Cleanup is called whether mutation succeeds or fails
    /// </code>
    /// </example>
    IMutationBuilder<TResult> WithCleanup(Action cleanup);
}

