import { mutation } from "./_generated/server";
import { v } from "convex/values";

export const create = mutation({
  args: { testRunId: v.string(), name: v.string(), value: v.number() },
  handler: async (ctx, args) => {
    return await ctx.db.insert("testItems", args);
  },
});

export const update = mutation({
  args: {
    id: v.id("testItems"),
    name: v.optional(v.string()),
    value: v.optional(v.number()),
  },
  handler: async (ctx, args) => {
    const { id, ...updates } = args;
    await ctx.db.patch(id, updates);
  },
});

export const deleteItem = mutation({
  args: { id: v.id("testItems") },
  handler: async (ctx, args) => {
    await ctx.db.delete(args.id);
  },
});

export const cleanup = mutation({
  args: { testRunId: v.string() },
  handler: async (ctx, args) => {
    const items = await ctx.db
      .query("testItems")
      .withIndex("by_testRunId", (q) => q.eq("testRunId", args.testRunId))
      .collect();
    for (const item of items) {
      await ctx.db.delete(item._id);
    }
    return items.length;
  },
});
