/* eslint-disable */
/**
 * Generated `api` utility.
 *
 * THIS CODE IS AUTOMATICALLY GENERATED.
 *
 * To regenerate, run `npx convex dev`.
 * @module
 */

import type * as functions_createGame from "../functions/createGame.js";
import type * as functions_forfeitGame from "../functions/forfeitGame.js";
import type * as functions_getGame from "../functions/getGame.js";
import type * as functions_getGames from "../functions/getGames.js";
import type * as functions_getJoinableGames from "../functions/getJoinableGames.js";
import type * as functions_joinGame from "../functions/joinGame.js";
import type * as functions_makeMove from "../functions/makeMove.js";
import type * as functions_updatePresence from "../functions/updatePresence.js";

import type {
  ApiFromModules,
  FilterApi,
  FunctionReference,
} from "convex/server";

declare const fullApi: ApiFromModules<{
  "functions/createGame": typeof functions_createGame;
  "functions/forfeitGame": typeof functions_forfeitGame;
  "functions/getGame": typeof functions_getGame;
  "functions/getGames": typeof functions_getGames;
  "functions/getJoinableGames": typeof functions_getJoinableGames;
  "functions/joinGame": typeof functions_joinGame;
  "functions/makeMove": typeof functions_makeMove;
  "functions/updatePresence": typeof functions_updatePresence;
}>;

/**
 * A utility for referencing Convex functions in your app's public API.
 *
 * Usage:
 * ```js
 * const myFunctionReference = api.myModule.myFunction;
 * ```
 */
export declare const api: FilterApi<
  typeof fullApi,
  FunctionReference<any, "public">
>;

/**
 * A utility for referencing Convex functions in your app's internal API.
 *
 * Usage:
 * ```js
 * const myFunctionReference = internal.myModule.myFunction;
 * ```
 */
export declare const internal: FilterApi<
  typeof fullApi,
  FunctionReference<any, "internal">
>;

export declare const components: {};
