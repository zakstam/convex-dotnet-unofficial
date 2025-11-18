// Function to insert a chat message into the "messages" table.

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    username: v.optional(v.string()),
    text: v.string(),
    parentMessageId: v.optional(v.id("messages")),
    attachments: v.optional(v.array(v.object({
      storageId: v.string(),
      filename: v.string(),
      contentType: v.string(),
      size: v.float64(),
    }))),
  },
  handler: async (ctx, { username, text, parentMessageId, attachments }) => {
    // Basic validation
    if (!username) {
      throw new Error("Username cannot be empty");
    }
    if (!username.trim()) {
      throw new Error("Username cannot be empty");
    }
    // Allow empty text if there are attachments
    if (!text.trim() && (!attachments || attachments.length === 0)) {
      throw new Error("Message text cannot be empty unless there are attachments");
    }
    if (text.length > 1000) {
      throw new Error("Message text cannot exceed 1000 characters");
    }
    if (username.length > 50) {
      throw new Error("Username cannot exceed 50 characters");
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
      username: username.trim(),
      text: text.trim(),
      timestamp: Date.now(),
      parentMessageId,
      attachments: attachments && attachments.length > 0 ? attachments : undefined,
    };

    const id = await ctx.db.insert("messages", message);
    return { id, ...message };
  },
});
