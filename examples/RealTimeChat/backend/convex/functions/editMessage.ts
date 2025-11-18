// Function to edit a chat message.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { Id } from "../_generated/dataModel";

export default mutation({
  args: {
    id: v.string(),
    text: v.string(),
  },
  handler: async (ctx, { id, text }) => {
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

    await ctx.db.patch(id as Id<"messages">, {
      text: text.trim(),
      editedAt: Date.now(),
    });

    const updated = await ctx.db.get(id as Id<"messages">);
    return updated;
  },
});
