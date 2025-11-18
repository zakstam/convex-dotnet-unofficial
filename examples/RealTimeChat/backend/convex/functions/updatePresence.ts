// Mutation to update user presence (heartbeat)

import { mutation } from "../_generated/server";
import { v } from "convex/values";

const PRESENCE_TIMEOUT_MS = 30000; // 30 seconds

export default mutation({
  args: {
    username: v.string(),
  },
  handler: async (ctx, { username }) => {
    if (!username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const now = Date.now();

    // Check if user already has presence record
    const existing = await ctx.db
      .query("userPresence")
      .withIndex("by_username", (q) => q.eq("username", username.trim()))
      .first();

    if (existing) {
      // Update existing presence
      await ctx.db.patch(existing._id, {
        lastSeen: now,
      });
    } else {
      // Create new presence record
      await ctx.db.insert("userPresence", {
        username: username.trim(),
        lastSeen: now,
      });
    }

    return { success: true };
  },
});

