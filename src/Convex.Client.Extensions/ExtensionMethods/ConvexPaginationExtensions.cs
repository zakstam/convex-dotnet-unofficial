using Convex.Client.Slices.Pagination;

namespace Convex.Client.Extensions.ExtensionMethods;

/// <summary>
/// Extension methods for creating paginated queries with automatic subscription support.
/// </summary>
public static class ConvexPaginationExtensions
{
    /// <summary>
    /// Creates a paginated query with automatic subscription support.
    /// Handles loading pages and merging real-time updates automatically.
    /// </summary>
    /// <typeparam name="T">The type of items being paginated.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the Convex query function.</param>
    /// <returns>A builder for configuring the paginated query.</returns>
    /// <example>
    /// <code>
    /// var paginatedMessages = client.CreatePaginatedQuery&lt;MessageDto&gt;("messages:get")
    ///     .WithPageSize(25)
    ///     .WithArgs(new { limit = 50 })
    ///     .WithIdExtractor(msg => msg.Id)
    ///     .WithSortKey(msg => msg.Timestamp)
    ///     .Build();
    ///
    /// paginatedMessages.ItemsUpdated += (items, boundaries) => UpdateUI(items);
    /// await paginatedMessages.InitializeAsync();
    /// </code>
    /// </example>
    public static PaginatedQueryHelperBuilder<T> CreatePaginatedQuery<T>(
        this IConvexClient client,
        string functionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return PaginatedQueryHelper<T>.Create(client, functionName);
    }
}

