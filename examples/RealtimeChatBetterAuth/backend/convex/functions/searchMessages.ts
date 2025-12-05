// Query to search messages by text

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: {
    searchText: v.string(),
    limit: v.optional(v.number()),
  },
  handler: async (ctx, { searchText, limit = 50 }) => {
    if (!searchText.trim()) {
      return [];
    }

    const searchLower = searchText.toLowerCase().trim();

    // Get all messages and filter client-side (Convex doesn't have full-text search built-in)
    // For production, consider using Convex search or external search service
    const allMessages = await ctx.db
      .query("messages")
      .order("desc")
      .take(limit * 2); // Get more to filter

    // Filter messages containing search text
    const matchingMessages = allMessages
      .filter(
        (msg) =>
          msg.text.toLowerCase().includes(searchLower) ||
          msg.username.toLowerCase().includes(searchLower)
      )
      .slice(0, limit);

    return matchingMessages;
  },
});

