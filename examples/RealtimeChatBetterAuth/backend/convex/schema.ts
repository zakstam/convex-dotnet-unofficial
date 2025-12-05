// Schema for Realtime Chat with Better Auth integration
//
// Better Auth manages its own tables (user, session, account, verification, etc.)
// via the betterAuth component. This schema defines app-specific tables that
// reference the Better Auth user table via userId.

import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

// A chat message
export type Message = {
  id?: string; // Convex will assign ids
  userId: string; // References Better Auth user._id
  username: string; // Display name (denormalized for performance)
  text: string;
  timestamp: number; // epoch ms
  editedAt?: number; // epoch ms - when message was edited
};

// User presence tracking
export type UserPresence = {
  userId: string;
  username: string;
  lastSeen: number; // epoch ms
};

// Typing indicator
export type TypingIndicator = {
  userId: string;
  username: string;
  timestamp: number; // epoch ms
};

// Define the schema
export default defineSchema({
  messages: defineTable({
    userId: v.string(), // References Better Auth user
    username: v.string(), // Display name (denormalized)
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
    .index("by_userId", ["userId"])
    .index("by_username", ["username"])
    .index("by_timestamp", ["timestamp"])
    .index("by_parentMessageId", ["parentMessageId"]), // For querying replies

  // User presence - tracks who's online
  userPresence: defineTable({
    userId: v.string(),
    username: v.string(),
    lastSeen: v.number(),
  })
    .index("by_userId", ["userId"])
    .index("by_username", ["username"])
    .index("by_lastSeen", ["lastSeen"]),

  // Typing indicators
  typingIndicators: defineTable({
    userId: v.string(),
    username: v.string(),
    timestamp: v.number(),
  })
    .index("by_userId", ["userId"])
    .index("by_username", ["username"])
    .index("by_timestamp", ["timestamp"]),

  // Message reactions
  messageReactions: defineTable({
    messageId: v.string(),
    userId: v.string(),
    username: v.string(),
    emoji: v.string(), // single emoji character
    timestamp: v.number(),
  })
    .index("by_messageId", ["messageId"])
    .index("by_messageId_userId", ["messageId", "userId"]),

  // Message read receipts - tracks which users have read which messages
  messageReads: defineTable({
    messageId: v.string(),
    userId: v.string(),
    username: v.string(),
    readAt: v.number(), // epoch ms
  })
    .index("by_messageId", ["messageId"])
    .index("by_messageId_userId", ["messageId", "userId"]),
});

// NOTE: Better Auth tables (user, session, account, verification, etc.) are
// managed by the betterAuth component and don't need to be defined here.
