// Query: Get room state with players

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: { roomId: v.id("rooms") },
  handler: async (ctx, { roomId }) => {
    const room = await ctx.db.get(roomId);
    if (!room) return null;

    // Include players
    const players = await ctx.db
      .query("players")
      .withIndex("by_roomId", (q) => q.eq("roomId", roomId))
      .collect();

    return { ...room, players };
  },
});
