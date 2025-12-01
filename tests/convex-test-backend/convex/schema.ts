import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  testItems: defineTable({
    testRunId: v.string(),
    name: v.string(),
    value: v.number(),
  }).index("by_testRunId", ["testRunId"]),
});
