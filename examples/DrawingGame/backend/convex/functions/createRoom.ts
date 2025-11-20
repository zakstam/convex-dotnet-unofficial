// Mutation: Create new game room

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { generateRoomCode } from "../lib/words";

export default mutation({
  args: {
    name: v.string(),
    hostUsername: v.string(),
    maxPlayers: v.number(),
    roundDuration: v.number(),
    difficulty: v.union(
      v.literal("easy"),
      v.literal("medium"),
      v.literal("hard"),
      v.literal("mixed")
    ),
  },
  handler: async (ctx, args) => {
    const code = generateRoomCode();

    const roomId = await ctx.db.insert("rooms", {
      name: args.name,
      code,
      hostUsername: args.hostUsername,
      maxPlayers: args.maxPlayers,
      status: "waiting",
      currentRound: 0,
      totalRounds: args.maxPlayers,
      currentDrawer: undefined,
      currentWord: undefined,
      wordOptions: undefined,
      roundStartTime: undefined,
      roundDuration: args.roundDuration,
      difficulty: args.difficulty,
      allowHints: true,
      createdAt: Date.now(),
      finishedAt: undefined,
    });

    // Add host as first player
    await ctx.db.insert("players", {
      roomId,
      username: args.hostUsername,
      score: 0,
      hasGuessedCorrectly: false,
      drawingTurn: 0,
      status: "connected",
      lastSeen: Date.now(),
    });

    return { roomId, code };
  },
});
