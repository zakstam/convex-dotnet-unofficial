// Scheduled function: Automatically end rounds that exceed time limit

import { cronJobs } from "convex/server";
import { internalMutation } from "./_generated/server";
import { internal, api } from "./_generated/api";

// Internal mutation to check and end timed-out rounds
export const checkRoundTimeout = internalMutation({
  args: {},
  handler: async (ctx) => {
    const activeRooms = await ctx.db
      .query("rooms")
      .withIndex("by_status", (q) => q.eq("status", "playing"))
      .collect();

    const now = Date.now();

    for (const room of activeRooms) {
      if (room.roundStartTime) {
        const elapsed = now - room.roundStartTime;
        const timeout = room.roundDuration * 1000;

        if (elapsed >= timeout) {
          // End the round
          await ctx.scheduler.runAfter(0, api.functions.endRound.default, {
            roomId: room._id,
          });
        }
      }
    }
  },
});

const crons = cronJobs();

// Check for round timeouts every 5 seconds
crons.interval(
  "check round timeouts",
  { seconds: 5 },
  internal.crons.checkRoundTimeout
);

export default crons;
