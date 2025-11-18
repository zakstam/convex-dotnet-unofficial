// Query to get online users (users active within last 30 seconds)

import { query } from "../_generated/server";
import { v } from "convex/values";
import { requireAuth } from "../lib/auth";

const PRESENCE_TIMEOUT_MS = 30000; // 30 seconds

export default query({
  handler: async (ctx) => {
    // Require authentication
    await requireAuth(ctx.auth);

    const now = Date.now();
    const cutoffTime = now - PRESENCE_TIMEOUT_MS;

    // Get all users active in the last 30 seconds
    const onlineUsers = await ctx.db
      .query("userPresence")
      .withIndex("by_lastSeen")
      .filter((q) => q.gte(q.field("lastSeen"), cutoffTime))
      .collect();

    // Extract unique users (in case of duplicates)
    const uniqueUsers = Array.from(
      new Set(onlineUsers.map((u) => u.userId))
    ).map((userId) => {
      const userPresence = onlineUsers.find((u) => u.userId === userId);
      return {
        userId,
        username: userPresence?.username ?? "",
        lastSeen: userPresence?.lastSeen ?? now,
      };
    });

    return uniqueUsers.sort((a, b) => a.username.localeCompare(b.username));
  },
});

