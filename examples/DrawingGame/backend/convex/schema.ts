// Schema for Drawing Game (Pictionary-lite)

import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  // Rooms table - stores game room state
  rooms: defineTable({
    name: v.string(),
    code: v.string(),              // 6-char join code
    hostUsername: v.string(),
    maxPlayers: v.number(),        // 3-8

    // Game state
    status: v.union(
      v.literal("waiting"),
      v.literal("playing"),
      v.literal("finished")
    ),
    currentRound: v.number(),
    totalRounds: v.number(),
    currentDrawer: v.optional(v.string()),
    currentWord: v.optional(v.string()),
    wordOptions: v.optional(v.array(v.string())),

    // Timing
    roundStartTime: v.optional(v.number()),
    roundDuration: v.number(),     // seconds (60-90)

    // Settings
    difficulty: v.union(
      v.literal("easy"),
      v.literal("medium"),
      v.literal("hard"),
      v.literal("mixed")
    ),
    allowHints: v.boolean(),

    createdAt: v.number(),
    finishedAt: v.optional(v.number()),
  })
    .index("by_code", ["code"])
    .index("by_status", ["status"]),

  // Players table - stores player information
  players: defineTable({
    roomId: v.id("rooms"),
    username: v.string(),

    // Stats
    score: v.number(),
    hasGuessedCorrectly: v.boolean(),  // For current round
    drawingTurn: v.optional(v.number()), // Which round they draw

    status: v.union(
      v.literal("connected"),
      v.literal("disconnected")
    ),
    lastSeen: v.number(),
  })
    .index("by_roomId", ["roomId"])
    .index("by_roomId_username", ["roomId", "username"]),

  // Stroke batches table - stores batched stroke points with time-relative timestamps
  strokeBatches: defineTable({
    roomId: v.id("rooms"),
    round: v.number(),
    drawer: v.string(),

    // Batch data
    events: v.array(v.object({
      timeSinceBatchStart: v.number(),
      eventData: v.object({
        x: v.number(),
        y: v.number(),
        pressure: v.optional(v.number()),
      }),
    })),
    batchStartTime: v.number(),    // Unix milliseconds
    color: v.string(),              // Hex color "#FF0000"
    thickness: v.number(),          // 1-10
    tool: v.union(
      v.literal("pencil"),
      v.literal("eraser")
    ),

    lastUpdated: v.number(),
  })
    .index("by_roomId_round_drawer", ["roomId", "round", "drawer"])
    .index("by_roomId_round", ["roomId", "round"]),

  // Guesses table - stores player guesses
  guesses: defineTable({
    roomId: v.id("rooms"),
    round: v.number(),
    username: v.string(),
    guess: v.string(),

    isCorrect: v.boolean(),
    pointsAwarded: v.number(),
    timestamp: v.number(),
  })
    .index("by_roomId_round", ["roomId", "round"]),

  // Drawings table - completed drawings with storage
  drawings: defineTable({
    roomId: v.id("rooms"),
    round: v.number(),
    drawer: v.string(),
    word: v.string(),

    // Storage reference
    imageStorageId: v.optional(v.id("_storage")),  // PNG snapshot
    strokesData: v.string(),                        // JSON of all strokes

    // Stats
    totalGuessers: v.number(),
    averageGuessTime: v.number(),

    createdAt: v.number(),
  })
    .index("by_roomId", ["roomId"])
    .index("by_roomId_round", ["roomId", "round"]),
});
