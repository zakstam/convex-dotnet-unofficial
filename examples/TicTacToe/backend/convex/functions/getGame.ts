// Query to retrieve a specific game by ID (for real-time subscription)

import { query } from "../_generated/server";
import { v } from "convex/values";

export default query({
  args: { gameId: v.id("games") },
  handler: async (ctx, { gameId }) => {
    return await ctx.db.get(gameId);
  },
});

