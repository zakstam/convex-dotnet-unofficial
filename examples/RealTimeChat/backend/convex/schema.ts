// Minimal schema/types for the Realtime Chat example

import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

// A chat message
export type Message = {
  id?: string; // Convex will assign ids
  username: string;
  text: string;
  timestamp: number; // epoch ms
  editedAt?: number; // epoch ms - when message was edited
};

// User presence tracking
export type UserPresence = {
  username: string;
  lastSeen: number; // epoch ms
};

// Typing indicator
export type TypingIndicator = {
  username: string;
  timestamp: number; // epoch ms
};

// Define the schema
export default defineSchema({
  messages: defineTable({
    username: v.string(),
    text: v.string(),
    timestamp: v.number(),
    editedAt: v.optional(v.number()),
    parentMessageId: v.optional(v.id("messages")), // For thread replies
    attachments: v.optional(v.array(v.object({
      storageId: v.string(),
      filename: v.string(),
      contentType: v.string(),
      size: v.float64(),
    }))),
  })
    .index("by_username", ["username"])
    .index("by_timestamp", ["timestamp"])
    .index("by_parentMessageId", ["parentMessageId"]), // For querying replies

  // User presence - tracks who's online
  userPresence: defineTable({
    username: v.string(),
    lastSeen: v.number(),
  })
    .index("by_username", ["username"])
    .index("by_lastSeen", ["lastSeen"]),

  // Typing indicators
  typingIndicators: defineTable({
    username: v.string(),
    timestamp: v.number(),
  })
    .index("by_username", ["username"])
    .index("by_timestamp", ["timestamp"]),

  // Message reactions
  messageReactions: defineTable({
    messageId: v.string(),
    username: v.string(),
    emoji: v.string(), // single emoji character
    timestamp: v.number(),
  })
    .index("by_messageId", ["messageId"])
    .index("by_messageId_username", ["messageId", "username"]),

  // Message read receipts - tracks which users have read which messages
  messageReads: defineTable({
    messageId: v.string(),
    username: v.string(),
    readAt: v.number(), // epoch ms
  })
    .index("by_messageId", ["messageId"])
    .index("by_messageId_username", ["messageId", "username"]),
});

// NOTE: This file is intentionally small. Expand with indexing or
// additional entities (rooms, users) as needed.
