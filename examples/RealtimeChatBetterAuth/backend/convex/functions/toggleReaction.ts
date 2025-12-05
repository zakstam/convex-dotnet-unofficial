// Mutation to add or remove a reaction to a message
// Requires authentication via Better Auth.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthUser } from "../auth";

export default mutation({
  args: {
    messageId: v.string(),
    emoji: v.string(),
    add: v.boolean(), // true to add, false to remove
  },
  handler: async (ctx, { messageId, emoji, add }) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    const userId = user._id;
    const username = user.name || user.email?.split("@")[0] || "Anonymous";

    if (!messageId.trim() || !emoji.trim()) {
      throw new Error("Message ID and emoji are required");
    }

    // Check if message exists
    const message = await ctx.db.get(messageId as any);
    if (!message) {
      throw new Error("Message not found");
    }

    // Find existing reaction for this specific emoji from this user
    const existing = await ctx.db
      .query("messageReactions")
      .withIndex("by_messageId", (q) => q.eq("messageId", messageId))
      .filter((q) =>
        q.and(
          q.eq(q.field("userId"), userId),
          q.eq(q.field("emoji"), emoji.trim())
        )
      )
      .first();

    if (add) {
      if (existing) {
        // Reaction already exists - update timestamp
        await ctx.db.patch(existing._id, {
          timestamp: Date.now(),
          username, // Update username in case it changed
        });
      } else {
        // Create new reaction for this emoji
        await ctx.db.insert("messageReactions", {
          messageId: messageId.trim(),
          userId,
          username,
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
