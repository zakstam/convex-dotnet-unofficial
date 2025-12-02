# Time-Based Batching Extension

A reusable extension for the Convex .NET client that provides efficient time-based batching for high-frequency event streams. This extension implements the pattern from `convex-realtime-cursors`, enabling efficient batching with PATCH mutations, time-relative actions, sampling, and smooth replay.

## Overview

The time-based batching extension is designed for scenarios where you need to send high-frequency events (like drawing strokes, cursor movements, etc.) to a Convex backend efficiently. Instead of sending each event individually, events are:

1. **Sampled** - Events are sampled at a configurable interval to reduce density
2. **Batched** - Events are collected into batches with time-relative timestamps
3. **PATCHed** - Batches are updated using PATCH mutations instead of creating new records
4. **Replayed** - Remote clients replay batches at the correct timing for smooth playback

## Key Features

- **Generic event type**: Works with any event type (strokes, cursor positions, etc.)
- **Time-relative actions**: Each event stores `timeSinceBatchStart` for accurate replay
- **Sampling**: Configurable sampling interval to reduce event density
- **Periodic batching**: Configurable batch interval for sending updates
- **PATCH mutations**: Updates existing batch records instead of creating new ones
- **Smooth replay**: Uses time-based delays for accurate playback on remote clients
- **Batch size limits**: Prevents oversized batches
- **Spatial filtering**: Optional minimum distance filtering for spatial events
- **Automatic cleanup**: Manages batch lifecycle and disposal

## Installation

The extension is included in the `Convex.Client.Extensions` package:

```bash
dotnet add package Convex.Client.Extensions
```

## Usage

### Basic Setup

```csharp
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using Convex.Client;

// Configure batching options
var options = new BatchingOptions
{
    SamplingIntervalMs = 10,        // Sample events every 10ms
    BatchIntervalMs = 500,          // Send batches every 500ms
    MaxBatchSize = 200,             // Maximum 200 events per batch
    MinEventDistance = 2.0,         // Optional: skip events closer than 2.0 units
    EnableSampling = true            // Enable sampling
};

// Create batcher
var batcher = client.CreateTimeBasedBatcher<StrokePoint>(
    options,
    storeMutation: "strokeBatches:store",
    metadata: new Dictionary<string, object> 
    { 
        { "userId", currentUserId },
        { "roomId", roomId }
    }
);
```

### Adding Events

```csharp
// Define a batchable event wrapper
public class BatchableStrokePoint : IBatchableEvent<StrokePoint>
{
    public double TimeSinceBatchStart { get; set; }
    public StrokePoint EventData { get; set; } = null!;
}

// Add events
batcher.AddEvent(
    new StrokePoint { X = 100, Y = 200, Pressure = 0.5 },
    (data, timeSinceBatchStart) => new BatchableStrokePoint
    {
        EventData = data,
        TimeSinceBatchStart = timeSinceBatchStart
    }
);
```

### Replaying Batches on Remote Clients

```csharp
// Create replay manager
var replayManager = client.CreateBatchReplayManager<StrokePoint>(options);

// Subscribe to batches
var batches = client.ObserveBatches<Batch<StrokePoint>>(
    "strokeBatches:list", 
    new { roomId = roomId }
);

// Flatten batches and subscribe
var individualBatches = batches.SelectMany(batchList => batchList);
replayManager.SubscribeToBatches(individualBatches);

// Consume replayed events
replayManager.ReplayedEvents.Subscribe(point =>
{
    // Render point at correct timing
    RenderPoint(point);
});

// Optional: Adjust replay speed
replayManager.ReplaySpeedMultiplier = 2.0; // 2x speed
```

### Complete Example: Drawing Application

```csharp
using Convex.Client.Extensions.Batching.TimeBasedBatching;
using Convex.Client;

public class DrawingService
{
    private readonly IConvexClient _client;
    private TimeBasedBatcher<StrokePoint>? _batcher;
    private BatchReplayManager<StrokePoint>? _replayManager;

    public DrawingService(IConvexClient client)
    {
        _client = client;
    }

    public void StartDrawing(string userId, string roomId)
    {
        var options = new BatchingOptions
        {
            SamplingIntervalMs = 10,
            BatchIntervalMs = 500,
            MaxBatchSize = 200,
            MinEventDistance = 2.0
        };

        // Create batcher for sending strokes
        _batcher = _client.CreateTimeBasedBatcher<StrokePoint>(
            options,
            storeMutation: "strokeBatches:store",
            metadata: new Dictionary<string, object>
            {
                { "userId", userId },
                { "roomId", roomId }
            }
        );

        // Create replay manager for receiving strokes
        _replayManager = _client.CreateBatchReplayManager<StrokePoint>(options);
        
        var batches = _client.ObserveBatches<Batch<StrokePoint>>(
            "strokeBatches:list",
            new { roomId }
        );

        var individualBatches = batches.SelectMany(batchList => batchList);
        _replayManager.SubscribeToBatches(individualBatches);

        _replayManager.ReplayedEvents.Subscribe(point =>
        {
            OnStrokePointReceived(point);
        });
    }

    public void AddStrokePoint(double x, double y, double? pressure = null)
    {
        _batcher?.AddEvent(
            new StrokePoint { X = x, Y = y, Pressure = pressure },
            (data, time) => new BatchableStrokePoint
            {
                EventData = data,
                TimeSinceBatchStart = time
            }
        );
    }

    private void OnStrokePointReceived(StrokePoint point)
    {
        // Render the point on the canvas
        RenderPoint(point);
    }

    public void Dispose()
    {
        _batcher?.Dispose();
        _replayManager?.Dispose();
    }
}
```

## Configuration Options

### BatchingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SamplingIntervalMs` | `int` | `10` | How often to sample events (milliseconds). Events arriving more frequently are skipped. |
| `BatchIntervalMs` | `int` | `500` | How often to send batches to the server (milliseconds). |
| `MaxBatchSize` | `int` | `200` | Maximum number of events per batch. When reached, batch is flushed immediately. |
| `MinEventDistance` | `double?` | `null` | Optional minimum distance between events for spatial filtering. Set to `null` to disable. |
| `EnableSampling` | `bool` | `true` | Whether to enable sampling. When disabled, all events are added (subject to spatial filtering). |

### Performance Tuning

- **High-frequency events** (e.g., mouse movements): Use `SamplingIntervalMs = 10-20` and `MinEventDistance = 2.0`
- **Lower-frequency events** (e.g., clicks): Use `SamplingIntervalMs = 50-100` and `MinEventDistance = null`
- **Network-constrained**: Increase `BatchIntervalMs` to 1000ms and `MaxBatchSize` to 500
- **Low-latency required**: Decrease `BatchIntervalMs` to 100-200ms

## Backend Requirements

The extension assumes your Convex backend follows this pattern:

### 1. Store Mutation (`strokeBatches:store`)

The mutation should accept a `Batch<TEvent>` and either PATCH an existing batch or INSERT a new one:

```typescript
// convex/functions/strokeBatches.ts
import { mutation } from "./_generated/server";
import { v } from "convex/values";

export const store = mutation({
  args: {
    events: v.array(v.any()),
    batchStartTime: v.number(),
    metadata: v.optional(v.object({
      userId: v.optional(v.string()),
      roomId: v.optional(v.string()),
      // ... other metadata fields
    })),
  },
  handler: async (ctx, args) => {
    // Find existing batch by metadata
    const existing = await ctx.db
      .query("strokeBatches")
      .withIndex("by_user_room", (q) => 
        q.eq("userId", args.metadata?.userId)
         .eq("roomId", args.metadata?.roomId)
      )
      .first();

    if (existing) {
      // PATCH existing batch
      await ctx.db.patch(existing._id, {
        events: [...existing.events, ...args.events],
        lastUpdated: Date.now(),
      });
      return existing._id;
    } else {
      // INSERT new batch
      return await ctx.db.insert("strokeBatches", {
        events: args.events,
        batchStartTime: args.batchStartTime,
        userId: args.metadata?.userId,
        roomId: args.metadata?.roomId,
        lastUpdated: Date.now(),
      });
    }
  },
});
```

### 2. List Query (`strokeBatches:list`)

The query should return all batches matching criteria:

```typescript
export const list = query({
  args: {
    roomId: v.string(),
  },
  handler: async (ctx, args) => {
    return await ctx.db
      .query("strokeBatches")
      .withIndex("by_room", (q) => q.eq("roomId", args.roomId))
      .collect();
  },
});
```

### 3. Schema Definition

```typescript
// convex/schema.ts
import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  strokeBatches: defineTable({
    events: v.array(v.any()),
    batchStartTime: v.number(),
    userId: v.optional(v.string()),
    roomId: v.optional(v.string()),
    lastUpdated: v.number(),
  })
    .index("by_user_room", ["userId", "roomId"])
    .index("by_room", ["roomId"]),
});
```

## Advanced Usage

### Custom Event Types

You can use any event type as long as you provide a factory function:

```csharp
public class CursorPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class BatchableCursorPosition : IBatchableEvent<CursorPosition>
{
    public double TimeSinceBatchStart { get; set; }
    public CursorPosition EventData { get; set; } = null!;
}

var batcher = client.CreateTimeBasedBatcher<CursorPosition>(
    options,
    storeMutation: "cursorBatches:store"
);

batcher.AddEvent(
    new CursorPosition { X = 100, Y = 200 },
    (data, time) => new BatchableCursorPosition
    {
        EventData = data,
        TimeSinceBatchStart = time
    }
);
```

### Dynamic Metadata

Update batch metadata at runtime:

```csharp
batcher.SetMetadata(new Dictionary<string, object>
{
    { "userId", newUserId },
    { "sessionId", sessionId }
});
```

### Manual Flush

Force an immediate batch flush:

```csharp
await batcher.FlushAsync();
```

### Replay Speed Control

Adjust replay speed for different scenarios:

```csharp
replayManager.ReplaySpeedMultiplier = 0.5;  // Half speed (slow motion)
replayManager.ReplaySpeedMultiplier = 1.0;   // Normal speed
replayManager.ReplaySpeedMultiplier = 2.0;   // Double speed
```

## Performance Considerations

1. **Sampling**: Reduces event density significantly. For 60fps input (16.67ms), sampling at 10ms reduces events by ~40%.

2. **Spatial Filtering**: For drawing applications, `MinEventDistance = 2.0` pixels typically reduces events by 50-70% without noticeable quality loss.

3. **Batch Size**: Larger batches reduce mutation frequency but increase latency. Balance based on your use case.

4. **Replay Timing**: The replay manager uses `Task.Delay` for timing. For browser-based applications, consider using JavaScript interop with `requestAnimationFrame` for smoother playback.

## Limitations

- Spatial filtering only works for events with `X` and `Y` properties (detected via reflection)
- Replay timing uses `Task.Delay` which may not be as precise as `requestAnimationFrame` in browser contexts
- Batch metadata is used for identifying existing batches - ensure it's unique per batch source

## Troubleshooting

### Events are being skipped too aggressively

- Decrease `SamplingIntervalMs`
- Increase `MinEventDistance` or set to `null`
- Disable sampling: `EnableSampling = false`

### Batches are too large

- Decrease `MaxBatchSize`
- Decrease `BatchIntervalMs` to flush more frequently

### Replay is not smooth

- Ensure `BatchIntervalMs` matches your event frequency
- Consider using JavaScript interop for `requestAnimationFrame`-based replay in Blazor

### Events are missing on remote clients

- Verify backend mutation is correctly PATCHing/INSERTing batches
- Check that the list query returns all batches
- Ensure metadata is consistent between batcher and query

## See Also

- [Convex .NET Client Extensions](../README.md) - Main extensions documentation
- [BatchOperationBuilder](../BatchOperationBuilder.cs) - For batching multiple queries/mutations
- [convex-realtime-cursors](https://github.com/get-convex/convex-realtime-cursors) - Original JavaScript implementation

