// Click effect mutations and queries
import { mutation, query } from "../_generated/server";
import { v } from "convex/values";

// Create a click effect (particle burst) at cursor position
export const create = mutation({
  args: {
    userId: v.id("users"),
    x: v.number(),
    y: v.number(),
    color: v.string(),
  },
  handler: async (ctx, args) => {
    return await ctx.db.insert("clickEffects", {
      userId: args.userId,
      x: args.x,
      y: args.y,
      color: args.color,
      timestamp: Date.now(),
    });
  },
});

// List recent click effects (last 2 seconds - shorter than reactions since they're quick bursts)
export const listRecent = query({
  args: {},
  handler: async (ctx) => {
    const twoSecondsAgo = Date.now() - 2_000;

    return await ctx.db
      .query("clickEffects")
      .withIndex("by_timestamp", (q) =>
        q.gte("timestamp", twoSecondsAgo)
      )
      .collect();
  },
});

// Clean up old click effects (optional maintenance mutation)
export const cleanup = mutation({
  args: {},
  handler: async (ctx) => {
    const twoSecondsAgo = Date.now() - 2_000;

    const oldEffects = await ctx.db
      .query("clickEffects")
      .withIndex("by_timestamp", (q) =>
        q.lt("timestamp", twoSecondsAgo)
      )
      .collect();

    for (const effect of oldEffects) {
      await ctx.db.delete(effect._id);
    }

    return { deleted: oldEffects.length };
  },
});
