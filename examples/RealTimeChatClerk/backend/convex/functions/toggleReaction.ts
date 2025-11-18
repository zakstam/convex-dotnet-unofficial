// Mutation to add or remove a reaction to a message

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId, getAuthenticatedUserEmail, getAuthenticatedUserName } from "../lib/auth";

export default mutation({
  args: {
    messageId: v.string(),
    emoji: v.string(),
    add: v.boolean(), // true to add, false to remove
  },
  handler: async (ctx, { messageId, emoji, add }) => {
    // Require authentication and get user info
    const userId = await getAuthenticatedUserId(ctx.auth);
    const userEmail = await getAuthenticatedUserEmail(ctx.auth);
    const userName = await getAuthenticatedUserName(ctx.auth);
    
    // Use email as username, fallback to name, fallback to userId
    const username = userEmail || userName || userId;

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
      .withIndex("by_messageId_userId", (q) => 
        q.eq("messageId", messageId).eq("userId", userId)
      )
      .filter((q) => q.eq(q.field("emoji"), emoji.trim()))
      .first();

    if (add) {
      if (existing) {
        // Reaction already exists - do nothing (idempotent)
        // Optionally update timestamp to refresh it
        await ctx.db.patch(existing._id, {
          username: username.trim(),
          timestamp: Date.now(),
        });
      } else {
        // Create new reaction for this emoji
        await ctx.db.insert("messageReactions", {
          messageId: messageId.trim(),
          userId,
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

