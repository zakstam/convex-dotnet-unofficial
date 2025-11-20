/* eslint-disable */
/**
 * Generated `api` utility.
 *
 * THIS CODE IS AUTOMATICALLY GENERATED.
 *
 * To regenerate, run `npx convex dev`.
 * @module
 */

import type * as crons from "../crons.js";
import type * as functions_clearCanvas from "../functions/clearCanvas.js";
import type * as functions_createRoom from "../functions/createRoom.js";
import type * as functions_endRound from "../functions/endRound.js";
import type * as functions_getGuesses from "../functions/getGuesses.js";
import type * as functions_getRoom from "../functions/getRoom.js";
import type * as functions_getRooms from "../functions/getRooms.js";
import type * as functions_joinRoom from "../functions/joinRoom.js";
import type * as functions_saveDrawingImage from "../functions/saveDrawingImage.js";
import type * as functions_selectWord from "../functions/selectWord.js";
import type * as functions_startGame from "../functions/startGame.js";
import type * as functions_strokeBatches from "../functions/strokeBatches.js";
import type * as functions_submitGuess from "../functions/submitGuess.js";
import type * as functions_updateDrawingStorage from "../functions/updateDrawingStorage.js";
import type * as functions_validateWord from "../functions/validateWord.js";
import type * as lib_words from "../lib/words.js";

import type {
  ApiFromModules,
  FilterApi,
  FunctionReference,
} from "convex/server";

declare const fullApi: ApiFromModules<{
  crons: typeof crons;
  "functions/clearCanvas": typeof functions_clearCanvas;
  "functions/createRoom": typeof functions_createRoom;
  "functions/endRound": typeof functions_endRound;
  "functions/getGuesses": typeof functions_getGuesses;
  "functions/getRoom": typeof functions_getRoom;
  "functions/getRooms": typeof functions_getRooms;
  "functions/joinRoom": typeof functions_joinRoom;
  "functions/saveDrawingImage": typeof functions_saveDrawingImage;
  "functions/selectWord": typeof functions_selectWord;
  "functions/startGame": typeof functions_startGame;
  "functions/strokeBatches": typeof functions_strokeBatches;
  "functions/submitGuess": typeof functions_submitGuess;
  "functions/updateDrawingStorage": typeof functions_updateDrawingStorage;
  "functions/validateWord": typeof functions_validateWord;
  "lib/words": typeof lib_words;
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
