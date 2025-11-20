// Mutation: Join existing room by code

import { mutation } from "../_generated/server";
import { v } from "convex/values";

export default mutation({
  args: {
    code: v.string(),
    username: v.string(),
  },
  handler: async (ctx, { code, username }) => {
    const room = await ctx.db
      .query("rooms")
      .withIndex("by_code", (q) => q.eq("code", code))
      .first();

    if (!room) {
      throw new Error("Room not found");
    }

    if (room.status !== "waiting") {
      throw new Error("Game already started");
    }

    const players = await ctx.db
      .query("players")
      .withIndex("by_roomId", (q) => q.eq("roomId", room._id))
      .collect();

    if (players.length >= room.maxPlayers) {
      throw new Error("Room is full");
    }

    if (players.some((p) => p.username === username)) {
      throw new Error("Username already taken in this room");
    }

    await ctx.db.insert("players", {
      roomId: room._id,
      username,
      score: 0,
      hasGuessedCorrectly: false,
      drawingTurn: players.length,
      status: "connected",
      lastSeen: Date.now(),
    });

    return room._id;
  },
});
