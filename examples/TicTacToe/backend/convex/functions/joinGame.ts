// Mutation to join an existing game as player O

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    gameId: v.id("games"),
    username: v.string(),
  },
  handler: async (ctx, { gameId, username }) => {
    if (!username || !username.trim()) {
      throw new Error("Username cannot be empty");
    }

    const game = await ctx.db.get(gameId);

    if (!game) {
      throw new Error("Game not found");
    }

    if (game.status !== "waiting") {
      throw new Error("Game already started or finished");
    }

    if (game.playerX === username.trim()) {
      throw new Error("Cannot play against yourself");
    }

    await ctx.db.patch(gameId, {
      playerO: username.trim(),
      status: "playing",
    });

    return gameId;
  },
});

