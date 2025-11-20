using Convex.Client;
using Convex.Client.Shared.Builders;

namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Helper methods for creating batch-related mutations and queries.
/// </summary>
public static class BatchMutationHelpers
{
    /// <summary>
    /// Creates a mutation builder for storing batches.
    /// The mutation should accept a Batch&lt;TEvent&gt; and return the batch ID or result.
    /// </summary>
    /// <typeparam name="TEvent">The type of events in the batch.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the mutation function (e.g., "strokeBatches:store").</param>
    /// <returns>A mutation builder that can be configured and executed.</returns>
    public static IMutationBuilder<object> CreateBatchStoreMutation<TEvent>(
        IConvexClient client,
        string functionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return client.Mutate<object>(functionName);
    }

    /// <summary>
    /// Creates a query builder for finding a specific batch by metadata.
    /// The query should accept metadata and return a Batch&lt;TEvent&gt; or null.
    /// </summary>
    /// <typeparam name="TEvent">The type of events in the batch.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the query function (e.g., "strokeBatches:find").</param>
    /// <returns>A query builder that can be configured and executed.</returns>
    public static IQueryBuilder<Batch<TEvent>?> CreateBatchFindQuery<TEvent>(
        IConvexClient client,
        string functionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return client.Query<Batch<TEvent>?>(functionName);
    }

    /// <summary>
    /// Creates a query builder for listing all batches matching criteria.
    /// The query should accept filter criteria and return a list of Batch&lt;TEvent&gt;.
    /// </summary>
    /// <typeparam name="TEvent">The type of events in the batch.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="functionName">The name of the query function (e.g., "strokeBatches:list").</param>
    /// <returns>A query builder that can be configured and executed.</returns>
    public static IQueryBuilder<List<Batch<TEvent>>> CreateBatchListQuery<TEvent>(
        IConvexClient client,
        string functionName)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(functionName);

        return client.Query<List<Batch<TEvent>>>(functionName);
    }
}

