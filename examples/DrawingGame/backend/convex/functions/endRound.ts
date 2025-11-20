// Mutation: End current round and transition to next

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { selectWords } from "../lib/words";

export default mutation({
  args: {
    roomId: v.id("rooms"),
  },
  handler: async (ctx, { roomId }) => {
    const room = await ctx.db.get(roomId);

    if (!room) {
      throw new Error("Room not found");
    }

    // Save drawing
    const batches = await ctx.db
      .query("strokeBatches")
      .withIndex("by_roomId_round", (q) =>
        q.eq("roomId", roomId).eq("round", room.currentRound))
      .collect();

    const guesses = await ctx.db
      .query("guesses")
      .withIndex("by_roomId_round", (q) =>
        q.eq("roomId", roomId).eq("round", room.currentRound))
      .collect();

    const correctGuesses = guesses.filter((g) => g.isCorrect);
    const totalGuessers = correctGuesses.length;

    let averageGuessTime = 0;
    if (totalGuessers > 0 && room.roundStartTime) {
      const totalTime = correctGuesses.reduce(
        (sum, g) => sum + (g.timestamp - room.roundStartTime!),
        0
      );
      averageGuessTime = totalTime / totalGuessers;
    }

    await ctx.db.insert("drawings", {
      roomId,
      round: room.currentRound,
      drawer: room.currentDrawer || "unknown",
      word: room.currentWord || "unknown",
      imageStorageId: undefined,
      strokesData: JSON.stringify(batches),
      totalGuessers,
      averageGuessTime,
      createdAt: Date.now(),
    });

    // Reset players' guess status
    const players = await ctx.db
      .query("players")
      .withIndex("by_roomId", (q) => q.eq("roomId", roomId))
      .collect();

    for (const player of players) {
      await ctx.db.patch(player._id, {
        hasGuessedCorrectly: false,
      });
    }

    // Check if game is finished
    if (room.currentRound >= room.totalRounds) {
      await ctx.db.patch(roomId, {
        status: "finished",
        finishedAt: Date.now(),
      });
      return { gameFinished: true };
    }

    // Next round
    const nextRound = room.currentRound + 1;
    const nextDrawer = players.find((p) => p.drawingTurn === nextRound - 1);
    const wordOptions = selectWords(room.difficulty, 3);

    await ctx.db.patch(roomId, {
      currentRound: nextRound,
      currentDrawer: nextDrawer?.username,
      currentWord: undefined,
      wordOptions,
      roundStartTime: undefined,
    });

    return { gameFinished: false, nextRound };
  },
});
