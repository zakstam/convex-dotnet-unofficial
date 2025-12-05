// Query to retrieve messages by a specific user.

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: {
    username: v.string(),
    limit: v.optional(v.number()),
  },
  handler: async (ctx, { username, limit = 20 }) => {
    const results = await ctx.db
      .query("messages")
      .withIndex("by_username", (q) => q.eq("username", username))
      .order("desc")
      .take(limit);

    return results;
  },
});
