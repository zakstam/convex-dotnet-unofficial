// Query to get reactions for a list of message IDs

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

    const reactions: Record<string, Array<{ emoji: string; count: number; users: string[] }>> = {};

    // Get all reactions for these messages
    for (const messageId of messageIds) {
      const messageReactions = await ctx.db
        .query("messageReactions")
        .withIndex("by_messageId", (q) => q.eq("messageId", messageId))
        .collect();

      // Group reactions by emoji
      const emojiGroups: Record<string, string[]> = {};
      for (const reaction of messageReactions) {
        if (!emojiGroups[reaction.emoji]) {
          emojiGroups[reaction.emoji] = [];
        }
        emojiGroups[reaction.emoji].push(reaction.username);
      }

      // Convert to array format
      reactions[messageId] = Object.entries(emojiGroups).map(([emoji, usernames]) => ({
        emoji,
        count: usernames.length,
        users: usernames,
      }));
    }

    return reactions;
  },
});

