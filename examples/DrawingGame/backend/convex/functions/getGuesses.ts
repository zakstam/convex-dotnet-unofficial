// Query: Get guesses for current round

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: {
    roomId: v.id("rooms"),
    round: v.number(),
  },
  handler: async (ctx, { roomId, round }) => {
    return await ctx.db
      .query("guesses")
      .withIndex("by_roomId_round", (q) =>
        q.eq("roomId", roomId).eq("round", round))
      .order("asc")
      .collect();
  },
});
