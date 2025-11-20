// Internal mutation: Update drawing with storage ID

import { internalMutation } from "../_generated/server";
import { v } from "convex/values";

export default internalMutation({
  args: {
    drawingId: v.id("drawings"),
    storageId: v.id("_storage"),
  },
  handler: async (ctx, { drawingId, storageId }) => {
    await ctx.db.patch(drawingId, {
      imageStorageId: storageId,
    });
  },
});
