using Convex.Client;
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using Convex.Client.Extensions.Gaming.Presets;
using Convex.Client.Extensions.Gaming.Sync;
using CursorPlayground.Shared.Models;

namespace CursorPlayground.Shared.Helpers;

/// <summary>
/// Batches cursor position events using the time-based batching extension.
/// Uses <see cref="GamePresets.ForCursorTracking"/> settings for optimal cursor tracking performance.
/// </summary>
/// <remarks>
/// <para>
/// This batcher uses gaming-optimized settings from <see cref="GamePresets.ForCursorTracking"/>:
/// </para>
/// <list type="bullet">
/// <item>16ms sampling (~60fps input capture)</item>
/// <item>200ms batch interval (5 batches/sec - 92% cost reduction)</item>
/// <item>5px spatial filtering (reduces jitter)</item>
/// <item>100ms interpolation delay (smooth cursor rendering on receivers)</item>
/// </list>
/// <para>
/// For smooth cursor rendering on the receiving end, use <see cref="InterpolatedState{T}"/>
/// with the same <see cref="GameSyncOptions"/> settings.
/// </para>
/// </remarks>
public class CursorBatcher : IDisposable, IAsyncDisposable
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
        // GamePresets.ForCursorTracking() provides consistent settings for both
        // input batching and subscription throttling
        var cursorOptions = GamePresets.ForCursorTracking();
        options ??= cursorOptions.InputBatching ?? BatchingOptions.ForCursorTracking();

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

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        if (_batcher != null)
        {
            await _batcher.DisposeAsync();
        }
        GC.SuppressFinalize(this);
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
