// Cursor Playground Schema
import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  // Users in the playground
  users: defineTable({
    name: v.string(),
    emoji: v.string(),
    color: v.string(),
    lastSeen: v.number(), // Unix timestamp in milliseconds
  }).index("by_lastSeen", ["lastSeen"]),

  // Cursor position batches (one per user, gets replaced on update)
  cursorBatches: defineTable({
    userId: v.id("users"),
    events: v.array(
      v.object({
        timeSinceBatchStart: v.number(),
        eventData: v.object({
          x: v.number(),
          y: v.number(),
          velocity: v.optional(v.number()),
        }),
      })
    ),
    batchStartTime: v.number(),
  }).index("by_userId", ["userId"]),

  // Emoji reactions dropped by users
  reactions: defineTable({
    userId: v.id("users"),
    emoji: v.string(),
    x: v.number(),
    y: v.number(),
    timestamp: v.number(),
  }).index("by_timestamp", ["timestamp"]),

  // Click effects (particle bursts) from users
  clickEffects: defineTable({
    userId: v.id("users"),
    x: v.number(),
    y: v.number(),
    timestamp: v.number(),
    color: v.string(),
  }).index("by_timestamp", ["timestamp"]),
});
