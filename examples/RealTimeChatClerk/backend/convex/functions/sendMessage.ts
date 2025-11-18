// Function to insert a chat message into the "messages" table.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId, getAuthenticatedUserEmail, getAuthenticatedUserName } from "../lib/auth";

export default mutation({
  args: {
    text: v.string(),
    parentMessageId: v.optional(v.id("messages")),
    attachments: v.optional(v.array(v.object({
      storageId: v.string(),
      filename: v.string(),
      contentType: v.string(),
      size: v.float64(),
    }))),
  },
  handler: async (ctx, { text, parentMessageId, attachments }) => {
    // Require authentication and get user info
    const userId = await getAuthenticatedUserId(ctx.auth);
    const userEmail = await getAuthenticatedUserEmail(ctx.auth);
    const userName = await getAuthenticatedUserName(ctx.auth);

    // Use email as username, fallback to name, fallback to userId
    const username = userEmail || userName || userId;

    // Basic validation
    // Allow empty text if there are attachments
    if (!text.trim() && (!attachments || attachments.length === 0)) {
      throw new Error("Message text cannot be empty unless there are attachments");
    }
    if (text.length > 1000) {
      throw new Error("Message text cannot exceed 1000 characters");
    }

    // Validate parent message exists if provided
    if (parentMessageId) {
      const parentMessage = await ctx.db.get(parentMessageId);
      if (!parentMessage) {
        throw new Error("Parent message not found");
      }
    }

    // Validate attachments if provided
    if (attachments && attachments.length > 0) {
      if (attachments.length > 10) {
        throw new Error("Cannot attach more than 10 files");
      }
      // Validate each attachment
      for (const attachment of attachments) {
        if (!attachment.storageId || !attachment.filename || !attachment.contentType) {
          throw new Error("Invalid attachment format");
        }
        if (attachment.size > 50 * 1024 * 1024) { // 50MB max per file
          throw new Error(`File ${attachment.filename} exceeds 50MB limit`);
        }
      }
    }

    const message = {
      userId,
      username: username.trim(),
      text: text.trim(),
      timestamp: Date.now(),
      parentMessageId,
      attachments: attachments && attachments.length > 0 ? attachments : undefined,
    };

    console.log("[sendMessage] Inserting message:", { username, text: text.substring(0, 20), userId });
    const id = await ctx.db.insert("messages", message);
    console.log("[sendMessage] Message inserted with ID:", id);

    return { id, ...message };
  },
});
