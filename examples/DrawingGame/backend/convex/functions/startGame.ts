// Mutation: Begin the game (host only)

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { selectWords } from "../lib/words";

export default mutation({
  args: {
    roomId: v.id("rooms"),
    username: v.string(),
  },
  handler: async (ctx, { roomId, username }) => {
    const room = await ctx.db.get(roomId);

    if (!room) {
      throw new Error("Room not found");
    }

    if (room.hostUsername !== username) {
      throw new Error("Only host can start game");
    }

    const players = await ctx.db
      .query("players")
      .withIndex("by_roomId", (q) => q.eq("roomId", roomId))
      .collect();

    if (players.length < 2) {
      throw new Error("Need at least 2 players");
    }

    // Select first drawer
    const firstDrawer = players[0];
    const wordOptions = selectWords(room.difficulty, 3);

    await ctx.db.patch(roomId, {
      status: "playing",
      currentRound: 1,
      currentDrawer: firstDrawer.username,
      wordOptions,
    });

    return { success: true };
  },
});
