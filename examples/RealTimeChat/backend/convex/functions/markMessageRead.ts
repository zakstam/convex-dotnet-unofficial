// Mutation to mark a message as read by a user
// Uses upsert pattern - updates if exists, inserts if not

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    messageId: v.string(),
    username: v.string(),
  },
  handler: async (ctx, { messageId, username }) => {
    if (!messageId.trim() || !username.trim()) {
      throw new Error("Message ID and username are required");
    }

    // Check if message exists
    const message = await ctx.db.get(messageId as any);
    if (!message) {
      throw new Error("Message not found");
    }

    // Don't allow users to mark their own messages as read
    if (message.username === username.trim()) {
      throw new Error("Cannot mark own message as read");
    }

    // Find existing read receipt
    const existing = await ctx.db
      .query("messageReads")
      .withIndex("by_messageId_username", (q) =>
        q.eq("messageId", messageId.trim()).eq("username", username.trim())
      )
      .first();

    const now = Date.now();

    if (existing) {
      // Update existing read receipt timestamp
      await ctx.db.patch(existing._id, {
        readAt: now,
      });
    } else {
      // Create new read receipt
      await ctx.db.insert("messageReads", {
        messageId: messageId.trim(),
        username: username.trim(),
        readAt: now,
      });
    }

    return { success: true };
  },
});

