// Action: Validate word against dictionary API

import { action } from "../_generated/server";
import { v } from "convex/values";

export default action({
  args: { word: v.string() },
  handler: async (ctx, { word }) => {
    try {
      // Call external dictionary API
      const response = await fetch(
        `https://api.dictionaryapi.dev/api/v2/entries/en/${word}`
      );

      if (!response.ok) {
        return { valid: false };
      }

      const data = await response.json();
      return {
        valid: true,
        definition: data[0]?.meanings[0]?.definitions[0]?.definition,
      };
    } catch (error) {
      return { valid: false };
    }
  },
});
