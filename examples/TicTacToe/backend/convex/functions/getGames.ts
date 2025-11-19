// Query to retrieve all active games (waiting or playing status)

import { query } from "../_generated/server";

export default query({
  args: {},
  handler: async (ctx) => {
    const games = await ctx.db
      .query("games")
      .filter((q) =>
        q.or(
          q.eq(q.field("status"), "waiting"),
          q.eq(q.field("status"), "playing")
        )
      )
      .order("desc")
      .take(20);

    return games;
  },
});

