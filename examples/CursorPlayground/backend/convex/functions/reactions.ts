// Reaction mutations and queries
import { mutation, query } from "../_generated/server";
import { v } from "convex/values";

// Create a reaction at cursor position
export const create = mutation({
  args: {
    userId: v.id("users"),
    emoji: v.string(),
    x: v.number(),
    y: v.number(),
  },
  handler: async (ctx, args) => {
    return await ctx.db.insert("reactions", {
      userId: args.userId,
      emoji: args.emoji,
      x: args.x,
      y: args.y,
      timestamp: Date.now(),
    });
  },
});

// List recent reactions (last 5 seconds)
export const listRecent = query({
  args: {},
  handler: async (ctx) => {
    const fiveSecondsAgo = Date.now() - 5_000;

    return await ctx.db
      .query("reactions")
      .withIndex("by_timestamp", (q) =>
        q.gte("timestamp", fiveSecondsAgo)
      )
      .collect();
  },
});

// Clean up old reactions (optional maintenance mutation)
export const cleanup = mutation({
  args: {},
  handler: async (ctx) => {
    const fiveSecondsAgo = Date.now() - 5_000;

    const oldReactions = await ctx.db
      .query("reactions")
      .withIndex("by_timestamp", (q) =>
        q.lt("timestamp", fiveSecondsAgo)
      )
      .collect();

    for (const reaction of oldReactions) {
      await ctx.db.delete(reaction._id);
    }

    return { deleted: oldReactions.length };
  },
});
