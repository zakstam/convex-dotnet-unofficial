import { query } from "./_generated/server";
import { v } from "convex/values";

export const list = query({
  args: { testRunId: v.string() },
  handler: async (ctx, args) => {
    return await ctx.db
      .query("testItems")
      .withIndex("by_testRunId", (q) => q.eq("testRunId", args.testRunId))
      .collect();
  },
});

export const get = query({
  args: { id: v.id("testItems") },
  handler: async (ctx, args) => {
    return await ctx.db.get(args.id);
  },
});

export const listAll = query({
  args: {},
  handler: async (ctx) => {
    return await ctx.db.query("testItems").collect();
  },
});

export const count = query({
  args: { testRunId: v.string() },
  handler: async (ctx, args) => {
    const items = await ctx.db
      .query("testItems")
      .withIndex("by_testRunId", (q) => q.eq("testRunId", args.testRunId))
      .collect();
    return items.length;
  },
});
