// Mutation to set typing indicator
// Requires authentication via Better Auth.

import { mutation } from "../_generated/server";
import { v } from "convex/values";
import { getAuthUser } from "../auth";

export default mutation({
  args: {
    isTyping: v.boolean(),
  },
  handler: async (ctx, { isTyping }) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    const userId = user._id;
    const username = user.name || user.email?.split("@")[0] || "Anonymous";
    const now = Date.now();

    // Find existing typing indicator for this user
    const existing = await ctx.db
      .query("typingIndicators")
      .withIndex("by_userId", (q) => q.eq("userId", userId))
      .first();

    if (isTyping) {
      if (existing) {
        // Update timestamp
        await ctx.db.patch(existing._id, {
          timestamp: now,
          username, // Update username in case it changed
        });
      } else {
        // Create new typing indicator
        await ctx.db.insert("typingIndicators", {
          userId,
          username,
          timestamp: now,
        });
      }
    } else {
      // Remove typing indicator
      if (existing) {
        await ctx.db.delete(existing._id);
      }
    }

    return { success: true };
  },
});
