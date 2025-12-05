// Storage actions for file upload/download
// These are wrapper functions that expose Convex storage APIs as actions

import { action } from "./_generated/server";
import { v } from "convex/values";

// Generate an upload URL for file uploads
export const generateUploadUrl = action({
  args: {
    filename: v.optional(v.string()),
  },
  handler: async (ctx, { filename }) => {
    // Generate upload URL using Convex storage API
    // Note: Convex's generateUploadUrl() only returns the URL string.
    // The actual storage ID is returned AFTER the file is uploaded to this URL.
    const uploadUrl = await ctx.storage.generateUploadUrl();
    
    // Return a placeholder storage ID to satisfy the .NET client's validation.
    // The actual storage ID will be returned in the upload POST response.
    return {
      uploadUrl,
      storageId: "pending", // Placeholder - actual storage ID comes from upload response
    };
  },
});

// Get download URL for a file
export const getUrl = action({
  args: {
    storageId: v.string(),
  },
  handler: async (ctx, { storageId }) => {
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
    // Get metadata using the storage API
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
      filename: null, // Storage metadata doesn't include filename
      contentType: fileMetadata.contentType ?? null,
      size: fileMetadata.size ?? 0,
      uploadedAt: 0, // Storage metadata doesn't include creation time
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
    await ctx.storage.delete(storageId as any);
    return {
      deleted: true,
    };
  },
});

