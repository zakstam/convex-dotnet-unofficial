// User management mutations and queries
import { mutation, query } from "../_generated/server";
import { v } from "convex/values";

// Join the playground - create or update user
export const join = mutation({
  args: {
    name: v.string(),
    emoji: v.string(),
    color: v.string(),
  },
  handler: async (ctx, args) => {
    const userId = await ctx.db.insert("users", {
      name: args.name,
      emoji: args.emoji,
      color: args.color,
      lastSeen: Date.now(),
    });

    return userId;
  },
});

// Heartbeat - update lastSeen timestamp
export const heartbeat = mutation({
  args: {
    userId: v.id("users"),
  },
  handler: async (ctx, args) => {
    await ctx.db.patch(args.userId, {
      lastSeen: Date.now(),
    });
    return null; // Return null for C# deserialization compatibility
  },
});

// List all active users (seen in last 10 seconds)
export const listActive = query({
  args: {},
  handler: async (ctx) => {
    const tenSecondsAgo = Date.now() - 10_000;

    const users = await ctx.db
      .query("users")
      .withIndex("by_lastSeen", (q) =>
        q.gte("lastSeen", tenSecondsAgo)
      )
      .collect();

    return users;
  },
});

// Clean up stale users (optional maintenance mutation)
export const cleanup = mutation({
  args: {},
  handler: async (ctx) => {
    const tenSecondsAgo = Date.now() - 10_000;

    const staleUsers = await ctx.db
      .query("users")
      .withIndex("by_lastSeen", (q) =>
        q.lt("lastSeen", tenSecondsAgo)
      )
      .collect();

    // Delete stale users and their cursor batches
    for (const user of staleUsers) {
      // Delete user's cursor batch
      const batch = await ctx.db
        .query("cursorBatches")
        .withIndex("by_userId", (q) => q.eq("userId", user._id))
        .first();

      if (batch) {
        await ctx.db.delete(batch._id);
      }

      // Delete user
      await ctx.db.delete(user._id);
    }

    return { deleted: staleUsers.length };
  },
});
