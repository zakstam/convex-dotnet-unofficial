// Mutation to forfeit/leave a game

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

    const player = game.playerX === username.trim() ? "X" :
                   game.playerO === username.trim() ? "O" : null;

    if (!player) {
      throw new Error("Not a player in this game");
    }

    if (game.status === "finished") {
      throw new Error("Game already finished");
    }

    // If game is still waiting (no opponent), mark as abandoned (no winner)
    // Otherwise, the other player wins by forfeit
    const winner = game.status === "waiting"
      ? null
      : (player === "X" ? "O" : "X");

    await ctx.db.patch(gameId, {
      status: "finished",
      winner: winner,
      finishedAt: Date.now(),
    });

    return { success: true };
  },
});

