using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.OptimisticUpdates;
using Convex.Client.Infrastructure.Resilience;

namespace Convex.Client.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating and configuring Convex mutations.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the mutation.</typeparam>
public interface IMutationBuilder<TResult>
{
    /// <summary>
    /// Sets the arguments to pass to the Convex function.
    /// </summary>
    /// <typeparam name="TArgs">The type of the arguments object.</typeparam>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The builder for method chaining.</returns>
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
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Enables optimistic update that executes immediately before the server responds.
    /// If the server mutation fails, the optimistic update is automatically rolled back.
    ///
    /// Note: This uses a state-focused approach (operates on arbitrary state), which differs
    /// from convex-js's query-focused optimistic updates (operates on query results).
    /// Both patterns are functionally equivalent but use different API styles.
    /// </summary>
    /// <param name="optimisticUpdate">A callback that performs the optimistic UI update.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> Optimistic(Action<TResult> optimisticUpdate);

    /// <summary>
    /// Enables optimistic update with a custom value to use before the server responds.
    /// If the server mutation fails, the optimistic update is automatically rolled back.
    ///
    /// Note: This uses a state-focused approach (operates on arbitrary state), which differs
    /// from convex-js's query-focused optimistic updates (operates on query results).
    /// Both patterns are functionally equivalent but use different API styles.
    /// </summary>
    /// <param name="optimisticValue">The value to use for the optimistic update.</param>
    /// <param name="apply">A callback that applies the optimistic value to the UI.</param>
    /// <returns>The builder for method chaining.</returns>
    IMutationBuilder<TResult> Optimistic(TResult optimisticValue, Action<TResult> apply);

    /// <summary>
    /// Enables optimistic update with automatic rollback on failure.
    /// Captures the current state before applying the update, and automatically restores it if the mutation fails.
    /// This is the recommended approach as it eliminates the need to manually implement rollback logic.
    ///
    /// Note: This uses a state-focused approach (operates on arbitrary state), which differs
    /// from convex-js's query-focused optimistic updates (operates on query results).
    /// Both patterns are functionally equivalent but use different API styles.
    /// </summary>
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
    /// </summary>
    /// <param name="rollback">An action that restores the UI/model to its pre-optimistic state.</param>
    /// <returns>The builder for method chaining.</returns>
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
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with the mutation result.</returns>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the mutation and returns a Result object containing either the result or an error.
    /// This method never throws exceptions - all errors are captured in the Result.
    /// Note: Optimistic updates are still applied, but rollback is only triggered for exceptions.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task that completes with a Result containing either the mutation result or an error.</returns>
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

