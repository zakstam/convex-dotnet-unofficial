// Function to delete a chat message.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { Id } from "../_generated/dataModel";

export default mutation({
  args: {
    id: v.string(),
  },
  handler: async (ctx, { id }) => {
    const existing = await ctx.db.get(id as Id<"messages">);
    if (!existing) {
      throw new Error(`Message with id ${id} not found`);
    }

    await ctx.db.delete(id as Id<"messages">);
    return { success: true };
  },
});
