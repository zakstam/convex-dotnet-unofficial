// Mutation to set typing indicator

import { mutation } from "../_generated/server";
import { v } from "convex/values";

const TYPING_TIMEOUT_MS = 3000; // 3 seconds

export default mutation({
  args: {
    username: v.string(),
    isTyping: v.boolean(),
  },
  handler: async (ctx, { username, isTyping }) => {
    if (!username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const trimmedUsername = username.trim();
    const now = Date.now();

    // Find existing typing indicator for this user
    const existing = await ctx.db
      .query("typingIndicators")
      .withIndex("by_username", (q) => q.eq("username", trimmedUsername))
      .first();

    if (isTyping) {
      if (existing) {
        // Update timestamp
        await ctx.db.patch(existing._id, {
          timestamp: now,
        });
      } else {
        // Create new typing indicator
        await ctx.db.insert("typingIndicators", {
          username: trimmedUsername,
          timestamp: now,
        });
      }
    } else {
      // Remove typing indicator
      if (existing) {
        await ctx.db.delete(existing._id);
      }
    }

    return { success: true };
  },
});

