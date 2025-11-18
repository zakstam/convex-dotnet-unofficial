// Query to get replies for a specific message

import { query } from "../_generated/server";
import { v } from "convex/values";
import { requireAuth } from "../lib/auth";

export default query({
  args: {
    parentMessageId: v.id("messages"),
  },
  handler: async (ctx, { parentMessageId }) => {
    // Require authentication
    await requireAuth(ctx.auth);

    // Verify parent message exists
    const parentMessage = await ctx.db.get(parentMessageId);
    if (!parentMessage) {
      throw new Error("Parent message not found");
    }

    // Get all replies to this message, ordered by timestamp (oldest first)
    const replies = await ctx.db
      .query("messages")
      .withIndex("by_parentMessageId", (q) => q.eq("parentMessageId", parentMessageId))
      .order("asc")
      .collect();

    return replies;
  },
});

