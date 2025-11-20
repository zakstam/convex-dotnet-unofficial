// Action: Convert strokes to PNG and save to storage

import { action } from "../_generated/server";
import { v } from "convex/values";
import { internal } from "../_generated/api";

export default action({
  args: {
    drawingId: v.id("drawings"),
    imageData: v.string(), // Base64 PNG
  },
  handler: async (ctx, { drawingId, imageData }) => {
    // Convert base64 to Blob
    const base64Data = imageData.replace(/^data:image\/png;base64,/, "");
    const binaryData = Buffer.from(base64Data, "base64");

    // Upload to Convex storage
    const blob = new Blob([binaryData], { type: "image/png" });
    const storageId = await ctx.storage.store(blob);

    // Update drawing record with storage ID
    await ctx.runMutation(internal.functions.updateDrawingStorage.default, {
      drawingId,
      storageId,
    });

    return storageId;
  },
});
