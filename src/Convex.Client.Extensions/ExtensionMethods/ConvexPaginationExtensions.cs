using Convex.Client.Features.RealTime.Pagination;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for creating paginated queries with automatic subscription support.
/// </summary>
public static class ConvexPaginationExtensions
{
    /// <summary>
    /// Creates a simplified paginated query builder with convention-based defaults.
    /// This is the recommended entry point for pagination when your DTOs follow conventions.
    /// </summary>
    /// <typeparam name="T">
    /// The type of items being paginated. For automatic ID/sort key extraction,
    /// implement <see cref="IHasId"/> and/or <see cref="IHasSortKey"/>, or ensure your type
    /// has properties named <c>Id</c> and <c>Timestamp</c>.
    /// </typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the Convex query function.</param>
    /// <param name="pageSize">Optional page size (default: 25).</param>
    /// <returns>A builder for configuring the paginated query.</returns>
    /// <example>
    /// <code>
    /// // Simplest usage - DTOs implement IHasId (or have an Id property)
    /// var paginator = await client.Paginate&lt;MessageDto&gt;("messages:list")
    ///     .InitializeAsync();
    ///
    /// // With custom page size
    /// var paginator = await client.Paginate&lt;MessageDto&gt;("messages:list", pageSize: 50)
    ///     .WithArgs(new { channel = "general" })
    ///     .InitializeAsync();
    ///
    /// // Access items and load more
    /// var items = paginator.CurrentItems;
    /// if (paginator.HasMore)
    /// {
    ///     await paginator.LoadNextAsync();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// This method uses convention-based extraction for ID and sort keys:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>ID extraction:</b> Uses <see cref="IHasId.Id"/> if implemented, otherwise looks for <c>Id</c>, <c>_id</c>, or <c>id</c> properties</description></item>
    /// <item><description><b>Sort key extraction:</b> Uses <see cref="IHasSortKey.SortKey"/> if implemented, otherwise looks for <c>Timestamp</c> or <c>CreatedAt</c> properties</description></item>
    /// </list>
    /// <para>
    /// For custom ID/sort key extraction, use <c>WithIdExtractor()</c> or <c>WithSortKey()</c> on the builder.
    /// </para>
    /// </remarks>
    public static PaginatedQueryHelperBuilder<T> Paginate<T>(
        this IConvexClient client,
        string functionName,
        int pageSize = 25)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return PaginatedQueryHelper<T>.Create(client, functionName)
            .WithPageSize(pageSize);
    }

    /// <summary>
    /// Creates a simplified paginated query builder with convention-based defaults and arguments.
    /// </summary>
    /// <typeparam name="T">The type of items being paginated.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the query.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the Convex query function.</param>
    /// <param name="args">Arguments to pass to the query function.</param>
    /// <param name="pageSize">Optional page size (default: 25).</param>
    /// <returns>A builder for configuring the paginated query.</returns>
    /// <example>
    /// <code>
    /// // With typed arguments
    /// var paginator = await client.Paginate&lt;MessageDto, GetMessagesArgs&gt;(
    ///     "messages:list",
    ///     new GetMessagesArgs { Channel = "general", Limit = 100 },
    ///     pageSize: 25)
    ///     .InitializeAsync();
    /// </code>
    /// </example>
    public static PaginatedQueryHelperBuilder<T> Paginate<T, TArgs>(
        this IConvexClient client,
        string functionName,
        TArgs args,
        int pageSize = 25)
        where TArgs : notnull
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return PaginatedQueryHelper<T>.Create(client, functionName)
            .WithPageSize(pageSize)
            .WithArgs(args);
    }

    /// <summary>
    /// Creates a paginated query and immediately initializes it with real-time subscription support.
    /// This is the most concise way to start paginating when using convention-based DTOs.
    /// </summary>
    /// <typeparam name="T">The type of items being paginated (must follow ID/sort conventions).</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the Convex query function.</param>
    /// <param name="pageSize">Optional page size (default: 25).</param>
    /// <param name="enableSubscription">Whether to enable real-time updates (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized paginated query helper ready for use.</returns>
    /// <example>
    /// <code>
    /// // One-liner to get started
    /// var paginator = await client.PaginateAsync&lt;MessageDto&gt;("messages:list");
    ///
    /// // Iterate over all items (auto-loads pages)
    /// foreach (var item in paginator.CurrentItems)
    /// {
    ///     Console.WriteLine(item);
    /// }
    ///
    /// // Load more if available
    /// if (paginator.HasMore)
    /// {
    ///     await paginator.LoadNextAsync();
    /// }
    /// </code>
    /// </example>
    public static async Task<PaginatedQueryHelper<T>> PaginateAsync<T>(
        this IConvexClient client,
        string functionName,
        int pageSize = 25,
        bool enableSubscription = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return await PaginatedQueryHelper<T>.Create(client, functionName)
            .WithPageSize(pageSize)
            .InitializeAsync(enableSubscription, cancellationToken);
    }

    /// <summary>
    /// Creates a paginated query with arguments and immediately initializes it.
    /// </summary>
    /// <typeparam name="T">The type of items being paginated.</typeparam>
    /// <typeparam name="TArgs">The type of arguments to pass to the query.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the Convex query function.</param>
    /// <param name="args">Arguments to pass to the query function.</param>
    /// <param name="pageSize">Optional page size (default: 25).</param>
    /// <param name="enableSubscription">Whether to enable real-time updates (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An initialized paginated query helper ready for use.</returns>
    public static async Task<PaginatedQueryHelper<T>> PaginateAsync<T, TArgs>(
        this IConvexClient client,
        string functionName,
        TArgs args,
        int pageSize = 25,
        bool enableSubscription = true,
        CancellationToken cancellationToken = default)
        where TArgs : notnull
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return await PaginatedQueryHelper<T>.Create(client, functionName)
            .WithPageSize(pageSize)
            .WithArgs(args)
            .InitializeAsync(enableSubscription, cancellationToken);
    }
}

