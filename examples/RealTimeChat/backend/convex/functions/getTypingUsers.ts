// Query to get users who are currently typing

import { query } from "../_generated/server";
import { v } from "convex/values";

const TYPING_TIMEOUT_MS = 3000; // 3 seconds

export default query({
  args: {
    excludeUsername: v.optional(v.string()),
  },
  handler: async (ctx, { excludeUsername }) => {
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
        (indicator) =>
          !excludeUsername || indicator.username !== excludeUsername
      )
      .map((indicator) => indicator.username);

    // Return unique usernames
    return Array.from(new Set(typingUsers));
  },
});

