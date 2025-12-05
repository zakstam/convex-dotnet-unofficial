// Function to delete a chat message.
// Requires authentication via Better Auth.
// Only the message owner can delete their messages.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { Id } from "../_generated/dataModel";
import { getAuthUser } from "../auth";

export default mutation({
  args: {
    id: v.string(),
  },
  handler: async (ctx, { id }) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    const existing = await ctx.db.get(id as Id<"messages">);
    if (!existing) {
      throw new Error(`Message with id ${id} not found`);
    }

    // Verify ownership - only the author can delete
    if (existing.userId !== user._id) {
      throw new Error("You can only delete your own messages");
    }

    await ctx.db.delete(id as Id<"messages">);
    return { success: true };
  },
});
