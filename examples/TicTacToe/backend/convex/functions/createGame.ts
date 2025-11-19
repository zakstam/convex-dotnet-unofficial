// Mutation to create a new game waiting for opponent

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    username: v.string(),
  },
  handler: async (ctx, { username }) => {
    if (!username || !username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const gameId = await ctx.db.insert("games", {
      playerX: username.trim(),
      playerO: undefined,
      board: ["", "", "", "", "", "", "", "", ""],
      currentTurn: "X",
      status: "waiting",
      winner: undefined,
      createdAt: Date.now(),
      finishedAt: undefined,
    });

    return gameId;
  },
});

