// Mutation: Player submits word guess

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    roomId: v.id("rooms"),
    username: v.string(),
    guess: v.string(),
  },
  handler: async (ctx, { roomId, username, guess }) => {
    const room = await ctx.db.get(roomId);

    if (!room || !room.currentWord) {
      throw new Error("Invalid game state");
    }

    if (room.currentDrawer === username) {
      throw new Error("Drawer cannot guess");
    }

    const player = await ctx.db
      .query("players")
      .withIndex("by_roomId_username", (q) =>
        q.eq("roomId", roomId).eq("username", username))
      .first();

    if (!player) {
      throw new Error("Player not found");
    }

    if (player.hasGuessedCorrectly) {
      throw new Error("Already guessed correctly");
    }

    const isCorrect =
      guess.toLowerCase().trim() === room.currentWord.toLowerCase().trim();

    let pointsAwarded = 0;

    if (isCorrect) {
      // Calculate points based on time elapsed
      const elapsed = Date.now() - (room.roundStartTime || Date.now());
      const timeRatio = 1 - elapsed / (room.roundDuration * 1000);
      pointsAwarded = Math.max(100, Math.floor(1000 * timeRatio));

      // Update player
      await ctx.db.patch(player._id, {
        score: player.score + pointsAwarded,
        hasGuessedCorrectly: true,
      });
    }

    // Record guess
    await ctx.db.insert("guesses", {
      roomId,
      round: room.currentRound,
      username,
      guess,
      isCorrect,
      pointsAwarded,
      timestamp: Date.now(),
    });

    return { isCorrect, pointsAwarded };
  },
});
