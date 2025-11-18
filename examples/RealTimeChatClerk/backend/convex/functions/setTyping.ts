// Mutation to set typing indicator

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId, getAuthenticatedUserEmail, getAuthenticatedUserName } from "../lib/auth";

const TYPING_TIMEOUT_MS = 3000; // 3 seconds

export default mutation({
  args: {
    isTyping: v.boolean(),
  },
  handler: async (ctx, { isTyping }) => {
    // Require authentication and get user info
    const userId = await getAuthenticatedUserId(ctx.auth);
    const userEmail = await getAuthenticatedUserEmail(ctx.auth);
    const userName = await getAuthenticatedUserName(ctx.auth);
    
    // Use email as username, fallback to name, fallback to userId
    const username = userEmail || userName || userId;
    const trimmedUsername = username.trim();
    const now = Date.now();

    // Find existing typing indicator for this user
    const existing = await ctx.db
      .query("typingIndicators")
      .withIndex("by_userId", (q) => q.eq("userId", userId))
      .first();

    if (isTyping) {
      if (existing) {
        // Update timestamp
        await ctx.db.patch(existing._id, {
          username: trimmedUsername,
          timestamp: now,
        });
      } else {
        // Create new typing indicator
        await ctx.db.insert("typingIndicators", {
          userId,
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

