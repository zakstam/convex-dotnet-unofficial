// Mutation: Store stroke batch (time-based batching)
// Query: List stroke batches for a room/round

import { mutation, query } from "../_generated/server";
import { v } from "convex/values";

export const store = mutation({
  args: {
    events: v.array(
      v.object({
        timeSinceBatchStart: v.number(),
        eventData: v.object({
          x: v.number(),
          y: v.number(),
          pressure: v.optional(v.number()),
        }),
      })
    ),
    batchStartTime: v.number(),
    metadata: v.optional(
      v.object({
        roomId: v.id("rooms"),
        round: v.number(),
        drawer: v.string(),
        color: v.string(),
        thickness: v.number(),
        tool: v.union(v.literal("pencil"), v.literal("eraser")),
      })
    ),
  },
  handler: async (ctx, args) => {
    if (!args.metadata) {
      throw new Error("Metadata is required");
    }

    const { roomId, round, drawer, color, thickness, tool } = args.metadata;

    // Verify room exists and game is in progress
    const room = await ctx.db.get(roomId);
    if (!room || room.status !== "playing") {
      throw new Error("Invalid game state");
    }

    if (!room.currentDrawer || room.currentDrawer !== drawer) {
      throw new Error("Invalid drawer");
    }

    // Find existing batch for this drawer/round
    const existing = await ctx.db
      .query("strokeBatches")
      .withIndex("by_roomId_round_drawer", (q) =>
        q.eq("roomId", roomId).eq("round", round).eq("drawer", drawer)
      )
      .first();

    if (existing) {
      // PATCH existing batch - append new events
      await ctx.db.patch(existing._id, {
        events: [...existing.events, ...args.events],
        lastUpdated: Date.now(),
        // Update style if it changed
        color,
        thickness,
        tool,
      });
      return existing._id;
    } else {
      // INSERT new batch
      return await ctx.db.insert("strokeBatches", {
        roomId,
        round,
        drawer,
        events: args.events,
        batchStartTime: args.batchStartTime,
        color,
        thickness,
        tool,
        lastUpdated: Date.now(),
      });
    }
  },
});

export const list = query({
  args: {
    roomId: v.id("rooms"),
    round: v.number(),
    excludeDrawer: v.optional(v.string()),
  },
  handler: async (ctx, args) => {
    const batches = await ctx.db
      .query("strokeBatches")
      .withIndex("by_roomId_round", (q) =>
        q.eq("roomId", args.roomId).eq("round", args.round)
      )
      .collect();

    // Filter out batches from the excluded drawer if specified
    if (args.excludeDrawer != null && args.excludeDrawer !== undefined && args.excludeDrawer !== "") {
      return batches.filter((batch) => batch.drawer !== args.excludeDrawer);
    }

    return batches;
  },
});

