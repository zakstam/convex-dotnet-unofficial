// Storage actions for file upload/download.
// These wrappers are authenticated and only allow callers to access attachments
// referenced by their own messages.

import { api } from "./_generated/api";
import { action, query } from "./_generated/server";
import { v } from "convex/values";
import { getAuthenticatedUserId } from "./lib/auth";

export const assertStorageAccess = query({
  args: {
    storageId: v.string(),
    userId: v.string(),
  },
  handler: async (ctx, { storageId, userId }) => {
    const messages = await ctx.db.query("messages").collect();
    const ownsAttachment = messages.some(
      (message) =>
        message.userId === userId &&
        message.attachments?.some((attachment) => attachment.storageId === storageId),
    );

    if (!ownsAttachment) {
      throw new Error("You can only access attachments from your own messages");
    }

    return true;
  },
});

// Generate an upload URL for file uploads
export const generateUploadUrl = action({
  args: {
    filename: v.optional(v.string()),
  },
  handler: async (ctx) => {
    await getAuthenticatedUserId(ctx.auth);

    const uploadUrl = await ctx.storage.generateUploadUrl();
    return {
      uploadUrl,
      storageId: "pending",
    };
  },
});

// Get download URL for a file
export const getUrl = action({
  args: {
    storageId: v.string(),
  },
  handler: async (ctx, { storageId }) => {
    const userId = await getAuthenticatedUserId(ctx.auth);
    await ctx.runQuery(api.storage.assertStorageAccess, { storageId, userId });

    const url = await ctx.storage.getUrl(storageId);
    return {
      url: url ?? null,
    };
  },
});

// Get file metadata
export const getMetadata = action({
  args: {
    storageId: v.string(),
  },
  handler: async (ctx, { storageId }) => {
    const userId = await getAuthenticatedUserId(ctx.auth);
    await ctx.runQuery(api.storage.assertStorageAccess, { storageId, userId });

    const fileMetadata = await ctx.storage.getMetadata(storageId);
    if (!fileMetadata) {
      return {
        storageId,
        filename: null,
        contentType: null,
        size: 0,
        uploadedAt: 0,
        sha256: null,
      };
    }

    return {
      storageId,
      filename: null,
      contentType: fileMetadata.contentType ?? null,
      size: fileMetadata.size ?? 0,
      uploadedAt: 0,
      sha256: fileMetadata.sha256 ?? null,
    };
  },
});

// Delete a file
export const deleteFile = action({
  args: {
    storageId: v.string(),
  },
  handler: async (ctx, { storageId }) => {
    const userId = await getAuthenticatedUserId(ctx.auth);
    await ctx.runQuery(api.storage.assertStorageAccess, { storageId, userId });

    await ctx.storage.delete(storageId as any);
    return {
      deleted: true,
    };
  },
});
