// Mutation to update player online status

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    username: v.string(),
    status: v.union(
      v.literal("online"),
      v.literal("in_game"),
      v.literal("offline")
    ),
    gameId: v.optional(v.id("games")),
  },
  handler: async (ctx, { username, status, gameId }) => {
    if (!username || !username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const existing = await ctx.db
      .query("presence")
      .withIndex("by_username", (q) => q.eq("username", username.trim()))
      .first();

    if (existing) {
      await ctx.db.patch(existing._id, {
        status,
        lastSeen: Date.now(),
        currentGameId: gameId ?? undefined,
      });
    } else {
      await ctx.db.insert("presence", {
        username: username.trim(),
        status,
        lastSeen: Date.now(),
        currentGameId: gameId ?? undefined,
      });
    }

    return { success: true };
  },
});

