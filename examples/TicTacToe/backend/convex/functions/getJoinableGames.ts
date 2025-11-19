// Query to retrieve games that can be joined (waiting status, no second player, playerX is present)

import { query } from "../_generated/server";

const PRESENCE_TIMEOUT_MS = 2 * 60 * 1000; // 2 minutes - consider player active if seen within this time

export default query({
  args: {},
  handler: async (ctx) => {
    // Get all waiting games
    const allWaitingGames = await ctx.db
      .query("games")
      .filter((q) => q.eq(q.field("status"), "waiting"))
      .order("desc")
      .take(50); // Get more to filter

    // Get all active presences (players seen within timeout period)
    const now = Date.now();
    const cutoffTime = now - PRESENCE_TIMEOUT_MS;
    
    const activePresences = await ctx.db
      .query("presence")
      .withIndex("by_lastSeen")
      .filter((q) => q.gte(q.field("lastSeen"), cutoffTime))
      .collect();

    // Create a set of active usernames for quick lookup
    const activeUsernames = new Set(
      activePresences
        .filter(p => p.status !== "offline")
        .map(p => p.username)
    );

    // Filter to only games that:
    // 1. Have a valid playerX (not empty/null)
    // 2. Don't have a second player yet (playerO is null/undefined)
    // 3. PlayerX is currently active/present (seen recently and not offline)
    const joinableGames = allWaitingGames.filter(game => {
      if (!game.playerX || game.playerX.trim().length === 0) {
        return false;
      }
      if (game.playerO) {
        return false;
      }
      // Check if playerX is active
      return activeUsernames.has(game.playerX);
    });

    // Return top 20 most recent
    return joinableGames.slice(0, 20);
  },
});

