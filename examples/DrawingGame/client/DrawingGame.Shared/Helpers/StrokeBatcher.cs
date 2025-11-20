using Convex.Client;
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using DrawingGame.Shared.Models;
using System.Collections.Generic;

namespace DrawingGame.Shared.Helpers;

/// <summary>
/// Batches stroke points using the time-based batching extension.
/// Automatically flushes after a timeout or when style changes.
/// </summary>
public class StrokeBatcher : IDisposable
{
    private readonly TimeBasedBatcher<StrokePointData> _batcher;
    private readonly string _roomId;
    private readonly double _round;
    private readonly string _drawer;
    
    private string _color = "#000000";
    private double _thickness = 2;
    private string _tool = "pencil";

    // Performance optimization constants
    private const int DefaultFlushIntervalMs = 500;

    public StrokeBatcher(IConvexClient client, string roomId, double round, string drawer, int flushIntervalMs = DefaultFlushIntervalMs)
    {
        _roomId = roomId;
        _round = round;
        _drawer = drawer;

        // Use preset optimized for drawing applications with custom flush interval
        var options = BatchingOptions.ForDrawing();
        options.BatchIntervalMs = flushIntervalMs;

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
    /// Add a point to the current batch
    /// </summary>
    public void AddPoint(double x, double y, double? pressure = null)
    {
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
    /// Manually flush the current batch
    /// </summary>
    public async Task FlushAsync()
    {
        await _batcher.FlushAsync();
    }

    public void Dispose()
    {
        _batcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}
