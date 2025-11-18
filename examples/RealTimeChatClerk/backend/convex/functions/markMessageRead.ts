// Mutation to mark a message as read by a user
// Uses upsert pattern - updates if exists, inserts if not

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId, getAuthenticatedUserEmail, getAuthenticatedUserName } from "../lib/auth";

export default mutation({
  args: {
    messageId: v.string(),
  },
  handler: async (ctx, { messageId }) => {
    // Require authentication and get user info
    const userId = await getAuthenticatedUserId(ctx.auth);
    const userEmail = await getAuthenticatedUserEmail(ctx.auth);
    const userName = await getAuthenticatedUserName(ctx.auth);
    
    // Use email as username, fallback to name, fallback to userId
    const username = userEmail || userName || userId;

    if (!messageId.trim()) {
      throw new Error("Message ID is required");
    }

    // Check if message exists
    const message = await ctx.db.get(messageId as any);
    if (!message) {
      throw new Error("Message not found");
    }

    // Allow users to mark their own messages as read (for read receipt badges)

    // Find existing read receipt
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
        username: username.trim(),
        readAt: now,
      });
    } else {
      // Create new read receipt
      await ctx.db.insert("messageReads", {
        messageId: messageId.trim(),
        userId,
        username: username.trim(),
        readAt: now,
      });
    }

    return { success: true };
  },
});

