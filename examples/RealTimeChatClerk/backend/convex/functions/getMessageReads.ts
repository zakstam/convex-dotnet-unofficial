// Query to get read receipts for a list of message IDs
// Returns dictionary: messageId -> List<{username, readAt}>

import { query } from "../_generated/server";
import { v } from "convex/values";
import { requireAuth } from "../lib/auth";

export default query({
  args: {
    messageIds: v.array(v.string()),
  },
  handler: async (ctx, { messageIds }) => {
    // Require authentication
    await requireAuth(ctx.auth);

    if (messageIds.length === 0) {
      return {};
    }

    const reads: Record<string, Array<{ username: string; readAt: number }>> = {};

    // Get all read receipts for these messages
    for (const messageId of messageIds) {
      const messageReads = await ctx.db
        .query("messageReads")
        .withIndex("by_messageId", (q) => q.eq("messageId", messageId))
        .collect();

      // Convert to array format
      reads[messageId] = messageReads.map((read) => ({
        username: read.username,
        readAt: read.readAt,
      }));
    }

    return reads;
  },
});

