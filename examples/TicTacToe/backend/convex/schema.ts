// Schema for Tic-Tac-Toe game

import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

// Define the schema
export default defineSchema({
  // Games table - stores game state
  games: defineTable({
    playerX: v.string(),           // Username of X player
    playerO: v.optional(v.string()), // Username of O player (null if waiting)
    board: v.array(v.string()),     // 9-element array: ["", "X", "O", "", ...]
    currentTurn: v.union(v.literal("X"), v.literal("O")), // Whose turn it is
    status: v.union(
      v.literal("waiting"),
      v.literal("playing"),
      v.literal("finished")
    ),
    winner: v.optional(v.union(
      v.literal("X"),
      v.literal("O"),
      v.literal("draw")
    )), // Winner: "X", "O", "draw", or null
    createdAt: v.number(),          // Timestamp when game was created
    finishedAt: v.optional(v.number()), // Timestamp when game finished
  })
    .index("by_status", ["status"])
    .index("by_playerX", ["playerX"])
    .index("by_playerO", ["playerO"]),

  // Moves table - optional history tracking
  moves: defineTable({
    gameId: v.id("games"),
    player: v.union(v.literal("X"), v.literal("O")),
    position: v.number(),            // 0-8 board position
    timestamp: v.number(),
  })
    .index("by_gameId", ["gameId"])
    .index("by_gameId_timestamp", ["gameId", "timestamp"]),

  // Presence table - optional online player tracking
  presence: defineTable({
    username: v.string(),
    status: v.union(
      v.literal("online"),
      v.literal("in_game"),
      v.literal("offline")
    ),
    lastSeen: v.number(),
    currentGameId: v.optional(v.id("games")),
  })
    .index("by_username", ["username"])
    .index("by_lastSeen", ["lastSeen"]),
});

