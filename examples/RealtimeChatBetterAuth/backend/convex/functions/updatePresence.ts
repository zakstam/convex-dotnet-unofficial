// Mutation to update user presence (heartbeat)
// Requires authentication via Better Auth.

import { mutation } from "../_generated/server";
import { getAuthUser } from "../auth";

export default mutation({
  args: {},
  handler: async (ctx) => {
    // Get the authenticated user
    const user = await getAuthUser(ctx);
    if (!user) {
      throw new Error("Authentication required");
    }

    const userId = user._id;
    const username = user.name || user.email?.split("@")[0] || "Anonymous";
    const now = Date.now();

    // Check if user already has presence record
    const existing = await ctx.db
      .query("userPresence")
      .withIndex("by_userId", (q) => q.eq("userId", userId))
      .first();

    if (existing) {
      // Update existing presence
      await ctx.db.patch(existing._id, {
        lastSeen: now,
        username, // Update username in case it changed
      });
    } else {
      // Create new presence record
      await ctx.db.insert("userPresence", {
        userId,
        username,
        lastSeen: now,
      });
    }

    return { success: true };
  },
});
