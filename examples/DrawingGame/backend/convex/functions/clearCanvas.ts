// Mutation: Drawer clears canvas (delete all strokes)

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    roomId: v.id("rooms"),
    username: v.string(),
  },
  handler: async (ctx, { roomId, username }) => {
    const room = await ctx.db.get(roomId);

    if (!room || room.currentDrawer !== username) {
      throw new Error("Not authorized");
    }

    const batches = await ctx.db
      .query("strokeBatches")
      .withIndex("by_roomId_round", (q) =>
        q.eq("roomId", roomId).eq("round", room.currentRound))
      .collect();

    for (const batch of batches) {
      await ctx.db.delete(batch._id);
    }

    return { success: true };
  },
});
