// Query to retrieve messages with pagination support.
// Returns pagination result with messages ordered by timestamp desc (most recent first).

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: {
    paginationOpts: v.optional(v.any()),
    limit: v.optional(v.number()),
  },
  handler: async (ctx, { paginationOpts, limit = 50 }) => {
    const results = await ctx.db
      .query("messages")
      .order("desc")
      .paginate(paginationOpts ?? { numItems: limit });

    // Return format that matches GetMessagesResponse C# model
    return {
      messages: results.page,  // C# expects "messages" property (case-insensitive)
      isDone: results.isDone,
      continueCursor: results.continueCursor,
    };
  },
});
