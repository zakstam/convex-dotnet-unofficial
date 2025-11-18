// Mutation to update user presence (heartbeat)

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId, getAuthenticatedUserEmail, getAuthenticatedUserName } from "../lib/auth";

const PRESENCE_TIMEOUT_MS = 30000; // 30 seconds

export default mutation({
  handler: async (ctx) => {
    // Require authentication and get user info
    const userId = await getAuthenticatedUserId(ctx.auth);
    const userEmail = await getAuthenticatedUserEmail(ctx.auth);
    const userName = await getAuthenticatedUserName(ctx.auth);
    
    // Use email as username, fallback to name, fallback to userId
    const username = userEmail || userName || userId;

    const now = Date.now();

    // Check if user already has presence record
    const existing = await ctx.db
      .query("userPresence")
      .withIndex("by_userId", (q) => q.eq("userId", userId))
      .first();

    if (existing) {
      // Update existing presence
      await ctx.db.patch(existing._id, {
        username: username.trim(),
        lastSeen: now,
      });
    } else {
      // Create new presence record
      await ctx.db.insert("userPresence", {
        userId,
        username: username.trim(),
        lastSeen: now,
      });
    }

    return { success: true };
  },
});

