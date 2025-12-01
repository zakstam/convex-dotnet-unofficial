import { action } from "./_generated/server";
import { v } from "convex/values";

export const echo = action({
  args: { message: v.string() },
  handler: async (_ctx, args) => {
    return { echo: args.message, timestamp: Date.now() };
  },
});

export const compute = action({
  args: {
    a: v.number(),
    b: v.number(),
    operation: v.string(),
  },
  handler: async (_ctx, args) => {
    const { a, b, operation } = args;
    let result: number;

    switch (operation) {
      case "add":
        result = a + b;
        break;
      case "subtract":
        result = a - b;
        break;
      case "multiply":
        result = a * b;
        break;
      case "divide":
        if (b === 0) {
          throw new Error("Division by zero is not allowed");
        }
        result = a / b;
        break;
      default:
        throw new Error(`Unknown operation: ${operation}`);
    }

    return { result, operation };
  },
});

export const delay = action({
  args: { milliseconds: v.number() },
  handler: async (_ctx, args) => {
    const start = Date.now();
    await new Promise((resolve) => setTimeout(resolve, args.milliseconds));
    const actualDelay = Date.now() - start;
    return { requestedDelay: args.milliseconds, actualDelay };
  },
});

export const throwError = action({
  args: { errorMessage: v.string() },
  handler: async (_ctx, args) => {
    throw new Error(args.errorMessage);
  },
});
