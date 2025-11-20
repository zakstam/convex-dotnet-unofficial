using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Convex.Client;

namespace Convex.Client.Extensions.Batching.TimeBasedBatching;

/// <summary>
/// Extension methods for IConvexClient to create time-based batching components.
/// </summary>
public static class TimeBasedBatchingExtensions
{
    /// <summary>
    /// Creates a time-based batcher for high-frequency events.
    /// </summary>
    /// <typeparam name="TEvent">The type of events to batch.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="options">Batching configuration options.</param>
    /// <param name="storeMutation">The name of the Convex mutation function to store batches (e.g., "strokeBatches:store").</param>
    /// <param name="metadata">Optional metadata for identifying batches (e.g., userId, sessionId, roomId).</param>
    /// <returns>A configured time-based batcher.</returns>
    /// <example>
    /// <code>
    /// var options = new BatchingOptions
    /// {
    ///     SamplingIntervalMs = 10,
    ///     BatchIntervalMs = 500,
    ///     MaxBatchSize = 200
    /// };
    ///
    /// var batcher = client.CreateTimeBasedBatcher&lt;StrokePoint&gt;(
    ///     options,
    ///     storeMutation: "strokeBatches:store",
    ///     metadata: new Dictionary&lt;string, object&gt; { { "userId", "user123" } }
    /// );
    ///
    /// batcher.AddEvent(new StrokePoint { X = 100, Y = 200 }, (data, time) => 
    ///     new BatchableStrokePoint { EventData = data, TimeSinceBatchStart = time });
    /// </code>
    /// </example>
    public static TimeBasedBatcher<TEvent> CreateTimeBasedBatcher<TEvent>(
        this IConvexClient client,
        BatchingOptions options,
        string storeMutation,
        Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(storeMutation);

        return new TimeBasedBatcher<TEvent>(client, options, storeMutation, metadata);
    }

    /// <summary>
    /// Creates a batch replay manager for replaying batches on remote clients.
    /// </summary>
    /// <typeparam name="TEvent">The type of events to replay.</typeparam>
    /// <param name="client">The Convex client (used for consistency, but not directly used).</param>
    /// <param name="options">Batching configuration options (used for timing).</param>
    /// <returns>A configured batch replay manager.</returns>
    /// <example>
    /// <code>
    /// var options = new BatchingOptions { BatchIntervalMs = 500 };
    /// var replayManager = client.CreateBatchReplayManager&lt;StrokePoint&gt;(options);
    ///
    /// var batches = client.ObserveBatches&lt;Batch&lt;StrokePoint&gt;&gt;("strokeBatches:list", new { roomId });
    /// replayManager.SubscribeToBatches(batches);
    ///
    /// replayManager.ReplayedEvents.Subscribe(point => {
    ///     RenderPoint(point);
    /// });
    /// </code>
    /// </example>
    public static BatchReplayManager<TEvent> CreateBatchReplayManager<TEvent>(
        this IConvexClient client,
        BatchingOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return new BatchReplayManager<TEvent>(options);
    }

    /// <summary>
    /// Creates an observable stream of batches from a Convex query.
    /// </summary>
    /// <typeparam name="TEvent">The type of events in the batches.</typeparam>
    /// <param name="client">The Convex client.</param>
    /// <param name="queryName">The name of the Convex query function (e.g., "strokeBatches:list").</param>
    /// <param name="args">Optional query arguments (e.g., new { roomId = "room123" }).</param>
    /// <returns>An observable stream of batch lists. Use SelectMany to flatten if needed.</returns>
    /// <example>
    /// <code>
    /// // Get all batches for a room
    /// var batches = client.ObserveBatches&lt;Batch&lt;StrokePoint&gt;&gt;("strokeBatches:list", new { roomId });
    ///
    /// // Flatten to individual batches
    /// var individualBatches = batches.SelectMany(batchList => batchList);
    ///
    /// replayManager.SubscribeToBatches(individualBatches);
    /// </code>
    /// </example>
    public static IObservable<List<Batch<TEvent>>> ObserveBatches<TEvent>(
        this IConvexClient client,
        string queryName,
        object? args = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(queryName);

        if (args == null)
        {
            return client.Observe<List<Batch<TEvent>>>(queryName);
        }

        return client.Observe<List<Batch<TEvent>>, object>(queryName, args);
    }
}

