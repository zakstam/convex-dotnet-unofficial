// Query to retrieve messages with pagination support.
// Returns pagination result with messages ordered by timestamp desc (most recent first).

import { query } from "../_generated/server";
import { v } from "convex/values";
import { requireAuth } from "../lib/auth";

export default query({
  args: {
    paginationOpts: v.optional(v.any()),
    limit: v.optional(v.number()),
  },
  handler: async (ctx, { paginationOpts, limit = 50 }) => {
    // Require authentication
    await requireAuth(ctx.auth);

    console.log("[getMessages] Query called - limit:", limit, "paginationOpts:", paginationOpts);

    const results = await ctx.db
      .query("messages")
      .withIndex("by_timestamp")
      .order("desc")
      .paginate(paginationOpts ?? { numItems: limit });

    console.log("[getMessages] Returning", results.page.length, "messages, isDone:", results.isDone);

    // Return format that matches GetMessagesResponse C# model
    return {
      messages: results.page,  // C# expects "messages" property (case-insensitive)
      isDone: results.isDone,
      continueCursor: results.continueCursor,
    };
  },
});
