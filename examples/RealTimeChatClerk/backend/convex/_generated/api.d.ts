/* eslint-disable */
/**
 * Generated `api` utility.
 *
 * THIS CODE IS AUTOMATICALLY GENERATED.
 *
 * To regenerate, run `npx convex dev`.
 * @module
 */

import type * as functions_deleteMessage from "../functions/deleteMessage.js";
import type * as functions_editMessage from "../functions/editMessage.js";
import type * as functions_getMessageReads from "../functions/getMessageReads.js";
import type * as functions_getMessages from "../functions/getMessages.js";
import type * as functions_getMessagesByUser from "../functions/getMessagesByUser.js";
import type * as functions_getOnlineUsers from "../functions/getOnlineUsers.js";
import type * as functions_getReactions from "../functions/getReactions.js";
import type * as functions_getReplies from "../functions/getReplies.js";
import type * as functions_getTypingUsers from "../functions/getTypingUsers.js";
import type * as functions_markMessageRead from "../functions/markMessageRead.js";
import type * as functions_searchMessages from "../functions/searchMessages.js";
import type * as functions_sendMessage from "../functions/sendMessage.js";
import type * as functions_sendReply from "../functions/sendReply.js";
import type * as functions_setTyping from "../functions/setTyping.js";
import type * as functions_toggleReaction from "../functions/toggleReaction.js";
import type * as functions_updatePresence from "../functions/updatePresence.js";
import type * as lib_auth from "../lib/auth.js";
import type * as storage from "../storage.js";

import type {
  ApiFromModules,
  FilterApi,
  FunctionReference,
} from "convex/server";

/**
 * A utility for referencing Convex functions in your app's API.
 *
 * Usage:
 * ```js
 * const myFunctionReference = api.myModule.myFunction;
 * ```
 */
declare const fullApi: ApiFromModules<{
  "functions/deleteMessage": typeof functions_deleteMessage;
  "functions/editMessage": typeof functions_editMessage;
  "functions/getMessageReads": typeof functions_getMessageReads;
  "functions/getMessages": typeof functions_getMessages;
  "functions/getMessagesByUser": typeof functions_getMessagesByUser;
  "functions/getOnlineUsers": typeof functions_getOnlineUsers;
  "functions/getReactions": typeof functions_getReactions;
  "functions/getReplies": typeof functions_getReplies;
  "functions/getTypingUsers": typeof functions_getTypingUsers;
  "functions/markMessageRead": typeof functions_markMessageRead;
  "functions/searchMessages": typeof functions_searchMessages;
  "functions/sendMessage": typeof functions_sendMessage;
  "functions/sendReply": typeof functions_sendReply;
  "functions/setTyping": typeof functions_setTyping;
  "functions/toggleReaction": typeof functions_toggleReaction;
  "functions/updatePresence": typeof functions_updatePresence;
  "lib/auth": typeof lib_auth;
  storage: typeof storage;
}>;
declare const fullApiWithMounts: typeof fullApi;

export declare const api: FilterApi<
  typeof fullApiWithMounts,
  FunctionReference<any, "public">
>;
export declare const internal: FilterApi<
  typeof fullApiWithMounts,
  FunctionReference<any, "internal">
>;

export declare const components: {};
