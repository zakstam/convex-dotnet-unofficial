// Mutation to add or remove a reaction to a message

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    messageId: v.string(),
    username: v.string(),
    emoji: v.string(),
    add: v.boolean(), // true to add, false to remove
  },
  handler: async (ctx, { messageId, username, emoji, add }) => {
    if (!messageId.trim() || !username.trim() || !emoji.trim()) {
      throw new Error("Message ID, username, and emoji are required");
    }

    // Check if message exists
    const message = await ctx.db.get(messageId as any);
    if (!message) {
      throw new Error("Message not found");
    }

    // Find existing reaction for this specific emoji from this user
    // Query by messageId and filter by username and emoji
    const existing = await ctx.db
      .query("messageReactions")
      .withIndex("by_messageId", (q) => q.eq("messageId", messageId))
      .filter((q) =>
        q.and(
          q.eq(q.field("username"), username.trim()),
          q.eq(q.field("emoji"), emoji.trim())
        )
      )
      .first();

    if (add) {
      if (existing) {
        // Reaction already exists - do nothing (idempotent)
        // Optionally update timestamp to refresh it
        await ctx.db.patch(existing._id, {
          timestamp: Date.now(),
        });
      } else {
        // Create new reaction for this emoji
        await ctx.db.insert("messageReactions", {
          messageId: messageId.trim(),
          username: username.trim(),
          emoji: emoji.trim(),
          timestamp: Date.now(),
        });
      }
    } else {
      // Remove reaction - only remove if it exists
      if (existing) {
        await ctx.db.delete(existing._id);
      }
    }

    return { success: true };
  },
});

