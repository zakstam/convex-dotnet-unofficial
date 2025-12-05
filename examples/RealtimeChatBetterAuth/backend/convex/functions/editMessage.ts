// Function to edit a chat message.
// Requires authentication via Better Auth.
// Only the message owner can edit their messages.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { Id } from "../_generated/dataModel";
import { getAuthUser } from "../auth";

export default mutation({
  args: {
    id: v.string(),
    text: v.string(),
  },
  handler: async (ctx, { id, text }) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    // Basic validation
    if (!text.trim()) {
      throw new Error("Message text cannot be empty");
    }
    if (text.length > 1000) {
      throw new Error("Message text cannot exceed 1000 characters");
    }

    const existing = await ctx.db.get(id as Id<"messages">);
    if (!existing) {
      throw new Error(`Message with id ${id} not found`);
    }

    // Verify ownership - only the author can edit
    if (existing.userId !== user._id) {
      throw new Error("You can only edit your own messages");
    }

    await ctx.db.patch(id as Id<"messages">, {
      text: text.trim(),
      editedAt: Date.now(),
    });

    const updated = await ctx.db.get(id as Id<"messages">);
    return updated;
  },
});
