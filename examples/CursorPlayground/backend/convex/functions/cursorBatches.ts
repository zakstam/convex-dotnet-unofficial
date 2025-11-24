// Cursor batch mutations and queries
import { mutation, query } from "../_generated/server";
import { v } from "convex/values";

// Store cursor position batch (REPLACE mode - each flush replaces the previous batch)
export const store = mutation({
  args: {
    events: v.array(
      v.object({
        timeSinceBatchStart: v.number(),
        eventData: v.object({
          x: v.number(),
          y: v.number(),
          velocity: v.optional(v.number()),
        }),
      })
    ),
    batchStartTime: v.number(),
    metadata: v.optional(
      v.object({
        userId: v.id("users"),
      })
    ),
  },
  handler: async (ctx, args) => {
    if (!args.metadata) {
      throw new Error("Metadata with userId is required");
    }

    const { userId } = args.metadata;

    // Verify user exists
    const user = await ctx.db.get(userId);
    if (!user) {
      throw new Error("User not found");
    }

    // Update user's lastSeen timestamp
    await ctx.db.patch(userId, {
      lastSeen: Date.now(),
    });

    // Find existing batch for this user
    const existing = await ctx.db
      .query("cursorBatches")
      .withIndex("by_userId", (q) => q.eq("userId", userId))
      .first();

    // Keep nested structure to match C# model (BatchableCursorEvent)
    const transformedEvents = args.events.map((event) => ({
      timeSinceBatchStart: event.timeSinceBatchStart,
      eventData: {
        x: event.eventData.x,
        y: event.eventData.y,
        velocity: event.eventData.velocity,
      },
    }));

    if (existing) {
      // REPLACE existing batch with new events (cursor tracking replaces, doesn't append)
      await ctx.db.patch(existing._id, {
        events: transformedEvents,
        batchStartTime: args.batchStartTime,
      });
      return existing._id;
    } else {
      // INSERT new batch
      return await ctx.db.insert("cursorBatches", {
        userId,
        events: transformedEvents,
        batchStartTime: args.batchStartTime,
      });
    }
  },
});

// List all cursor batches (for rendering all users' cursors)
export const list = query({
  args: {},
  handler: async (ctx) => {
    return await ctx.db.query("cursorBatches").collect();
  },
});
