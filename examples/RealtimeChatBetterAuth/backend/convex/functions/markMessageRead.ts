// Mutation to mark a message as read by a user
// Uses upsert pattern - updates if exists, inserts if not
// Requires authentication via Better Auth.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthUser } from "../auth";

export default mutation({
  args: {
    messageId: v.string(),
    username: v.string(), // Still accepted for backwards compatibility, but not used
  },
  handler: async (ctx, { messageId }) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    const userId = user._id;
    const username = user.name || user.email?.split("@")[0] || "Anonymous";

    if (!messageId.trim()) {
      throw new Error("Message ID is required");
    }

    // Check if message exists
    const message = await ctx.db.get(messageId as any);
    if (!message) {
      throw new Error("Message not found");
    }

    // Don't allow users to mark their own messages as read
    if (message.userId === userId) {
      throw new Error("Cannot mark own message as read");
    }

    // Find existing read receipt using userId index
    const existing = await ctx.db
      .query("messageReads")
      .withIndex("by_messageId_userId", (q) =>
        q.eq("messageId", messageId.trim()).eq("userId", userId)
      )
      .first();

    const now = Date.now();

    if (existing) {
      // Update existing read receipt timestamp
      await ctx.db.patch(existing._id, {
        readAt: now,
        username, // Update username in case it changed
      });
    } else {
      // Create new read receipt
      await ctx.db.insert("messageReads", {
        messageId: messageId.trim(),
        userId,
        username,
        readAt: now,
      });
    }

    return { success: true };
  },
});

