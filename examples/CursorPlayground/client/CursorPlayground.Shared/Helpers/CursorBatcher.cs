using Convex.Client;
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using CursorPlayground.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CursorPlayground.Shared.Helpers;

/// <summary>
/// Batches cursor position events using the time-based batching extension.
/// Uses BatchingOptions.ForCursorTracking() preset for optimal cursor tracking performance.
/// </summary>
public class CursorBatcher : IDisposable
{
    private readonly TimeBasedBatcher<CursorPosition> _batcher;
    private readonly string _userId;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new CursorBatcher for a specific user.
    /// </summary>
    /// <param name="client">The Convex client.</param>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="options">Optional batching configuration. If null, uses ForCursorTracking() preset.</param>
    public CursorBatcher(IConvexClient client, string userId, BatchingOptions? options = null)
    {
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));

        // Use provided options or default preset optimized for cursor tracking
        // - 16ms sampling (~60fps)
        // - 200ms batch interval (low latency)
        // - 5.0px min distance (cursor precision)
        // - Independent batches (replaces previous positions)
        options ??= BatchingOptions.ForCursorTracking();

        var metadata = new Dictionary<string, object>
        {
            { "userId", userId }
        };

        _batcher = client.CreateTimeBasedBatcher<CursorPosition>(
            options,
            storeMutation: "functions/cursorBatches:store",
            metadata: metadata
        );
    }

    /// <summary>
    /// Adds a cursor position to the current batch.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="velocity">Optional velocity for trail effects.</param>
    public void AddPosition(double x, double y, double? velocity = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var position = new CursorPosition
        {
            X = x,
            Y = y,
            Velocity = velocity
        };

        _batcher.AddEvent(position, (data, timeSinceBatchStart) => new BatchableCursorEvent
        {
            EventData = data,
            TimeSinceBatchStart = timeSinceBatchStart
        });
    }

    /// <summary>
    /// Manually flushes the current batch (normally automatic every 200ms).
    /// </summary>
    public async Task FlushAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await _batcher.FlushAsync();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _batcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
