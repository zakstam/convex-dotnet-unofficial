using Convex.Client;
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using Convex.Client.Extensions.Gaming.Presets;
using Convex.Client.Extensions.Gaming.Sync;
using DrawingGame.Shared.Models;

namespace DrawingGame.Shared.Helpers;

/// <summary>
/// Batches stroke points using the time-based batching extension.
/// Uses <see cref="GamePresets.ForDrawing"/> settings for optimal drawing performance.
/// </summary>
/// <remarks>
/// <para>
/// This batcher uses gaming-optimized settings from <see cref="GamePresets.ForDrawing"/>:
/// </para>
/// <list type="bullet">
/// <item>33ms sampling (~30fps input capture for drawing)</item>
/// <item>500ms batch interval (2 batches/sec - 94% cost reduction)</item>
/// <item>2px spatial filtering (reduces micro-jitter)</item>
/// <item>50ms interpolation delay (smooth stroke rendering on receivers)</item>
/// </list>
/// <para>
/// For smooth stroke rendering on the receiving end, use <see cref="InterpolatedState{T}"/>
/// with the same <see cref="GameSyncOptions"/> settings.
/// </para>
/// </remarks>
public class StrokeBatcher : IDisposable
{
    private readonly TimeBasedBatcher<StrokePointData> _batcher;
    private readonly string _roomId;
    private readonly double _round;
    private readonly string _drawer;
    private bool _isDisposed;

    private string _color = "#000000";
    private double _thickness = 2;
    private string _tool = "pencil";

    /// <summary>
    /// Initializes a new StrokeBatcher for a specific room and drawer.
    /// </summary>
    /// <param name="client">The Convex client.</param>
    /// <param name="roomId">The room identifier.</param>
    /// <param name="round">The current round number.</param>
    /// <param name="drawer">The drawer's username.</param>
    /// <param name="options">Optional batching configuration. If null, uses ForDrawing() preset.</param>
    public StrokeBatcher(IConvexClient client, string roomId, double round, string drawer, BatchingOptions? options = null)
    {
        _roomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
        _round = round;
        _drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));

        // Use provided options or default preset optimized for drawing
        // GamePresets.ForDrawing() provides consistent settings for both
        // input batching and subscription throttling
        var drawingOptions = GamePresets.ForDrawing();
        options ??= drawingOptions.InputBatching ?? BatchingOptions.ForDrawing();

        // Include default style in initial metadata (required by backend)
        var metadata = new Dictionary<string, object>
        {
            { "roomId", roomId },
            { "round", round },
            { "drawer", drawer },
            { "color", _color },
            { "thickness", _thickness },
            { "tool", _tool }
        };

        _batcher = client.CreateTimeBasedBatcher<StrokePointData>(
            options,
            storeMutation: "functions/strokeBatches:store",
            metadata: metadata
        );
    }

    /// <summary>
    /// Add a point to the current batch.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="pressure">Optional stylus pressure for variable-width strokes.</param>
    public void AddPoint(double x, double y, double? pressure = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var pointData = new StrokePointData
        {
            X = x,
            Y = y,
            Pressure = pressure
        };

        _batcher.AddEvent(pointData, (data, timeSinceBatchStart) => new BatchableStrokePoint
        {
            EventData = data,
            TimeSinceBatchStart = timeSinceBatchStart
        });
    }

    /// <summary>
    /// Set the drawing style (color, thickness, tool).
    /// Flushes current batch if style changes.
    /// </summary>
    public async Task SetStyleAsync(string color, double thickness, string tool)
    {
        if (_color != color || _thickness != thickness || _tool != tool)
        {
            // Flush current batch before changing style
            await _batcher.FlushAsync();

            _color = color;
            _thickness = thickness;
            _tool = tool;

            // Update metadata with new style
            var metadata = new Dictionary<string, object>
            {
                { "roomId", _roomId },
                { "round", _round },
                { "drawer", _drawer },
                { "color", color },
                { "thickness", thickness },
                { "tool", tool }
            };
            _batcher.SetMetadata(metadata);
        }
    }

    /// <summary>
    /// Manually flush the current batch (normally automatic every 500ms).
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
