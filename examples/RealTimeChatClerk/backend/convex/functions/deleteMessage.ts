// Function to delete a chat message.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { Id } from "../_generated/dataModel";
import { getAuthenticatedUserId } from "../lib/auth";

export default mutation({
  args: {
    id: v.string(),
  },
  handler: async (ctx, { id }) => {
    // Require authentication
    const userId = await getAuthenticatedUserId(ctx.auth);

    const existing = await ctx.db.get(id as Id<"messages">);
    if (!existing) {
      throw new Error(`Message with id ${id} not found`);
    }

    // Verify ownership - only the user who created the message can delete it
    if (existing.userId !== userId) {
      throw new Error("You can only delete your own messages");
    }

    await ctx.db.delete(id as Id<"messages">);
    return { success: true };
  },
});
