// Query to get users who are currently typing

import { query } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId } from "../lib/auth";

const TYPING_TIMEOUT_MS = 3000; // 3 seconds

export default query({
  args: {},
  handler: async (ctx) => {
    // Require authentication and get current user ID
    const currentUserId = await getAuthenticatedUserId(ctx.auth);

    const now = Date.now();
    const cutoffTime = now - TYPING_TIMEOUT_MS;

    // Get all typing indicators that are recent
    const typingIndicators = await ctx.db
      .query("typingIndicators")
      .withIndex("by_timestamp")
      .filter((q) => q.gte(q.field("timestamp"), cutoffTime))
      .collect();

    // Filter out the current user and get unique usernames
    const typingUsers = typingIndicators
      .filter(
        (indicator) => indicator.userId !== currentUserId
      )
      .map((indicator) => indicator.username);

    // Return unique usernames
    return Array.from(new Set(typingUsers));
  },
});

