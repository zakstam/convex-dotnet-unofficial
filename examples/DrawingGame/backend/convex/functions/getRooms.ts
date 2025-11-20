// Query: Get list of active rooms

import { query } from "../_generated/server";

export default query({
  args: {},
  handler: async (ctx) => {
    const allRooms = await ctx.db
      .query("rooms")
      .order("desc")
      .take(50);

    // Filter out finished rooms
    return allRooms.filter(room => room.status !== "finished").slice(0, 20);
  },
});
