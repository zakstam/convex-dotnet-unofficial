// Mutation: Drawer picks word to start round

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    roomId: v.id("rooms"),
    username: v.string(),
    word: v.string(),
  },
  handler: async (ctx, { roomId, username, word }) => {
    const room = await ctx.db.get(roomId);

    if (!room) {
      throw new Error("Room not found");
    }

    if (room.currentDrawer !== username) {
      throw new Error("Not your turn to draw");
    }

    if (!room.wordOptions?.includes(word)) {
      throw new Error("Invalid word selection");
    }

    await ctx.db.patch(roomId, {
      currentWord: word,
      wordOptions: undefined,
      roundStartTime: Date.now(),
    });

    return { success: true };
  },
});
