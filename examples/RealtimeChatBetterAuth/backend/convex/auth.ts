import { createClient, type GenericCtx } from "@convex-dev/better-auth";
import { convex, crossDomain } from "@convex-dev/better-auth/plugins";
import { components } from "./_generated/api";
import { DataModel } from "./_generated/dataModel";
import { betterAuth } from "better-auth";

const siteUrl = process.env.SITE_URL!;

// Frontend origins for CORS (localhost for development)
const frontendOrigins = [
  "http://localhost:5004",
  "https://localhost:7135",
];

// The component client has methods needed for integrating Convex with Better Auth
export const authComponent = createClient<DataModel>(components.betterAuth);

/**
 * Creates the Better Auth instance with Convex adapter.
 *
 * @param ctx - The Convex context
 * @param options - Configuration options
 * @returns The Better Auth instance
 */
export const createAuth = (
  ctx: GenericCtx<DataModel>,
  { optionsOnly } = { optionsOnly: false },
) => {
  return betterAuth({
    logger: { disabled: optionsOnly },
    baseURL: siteUrl,
    database: authComponent.adapter(ctx),

    // Email and password authentication
    emailAndPassword: {
      enabled: true,
      // Require email verification before login (set to false for simpler demo)
      requireEmailVerification: false,
    },

    // Configure trusted origins for CORS (include frontend dev servers)
    trustedOrigins: [siteUrl, ...frontendOrigins],

    // Configure plugins
    plugins: [
      // Required for client-side frameworks (like Blazor WebAssembly)
      crossDomain({ siteUrl }),
      // Convex plugin for Convex compatibility
      convex(),
    ],

    // Session configuration
    session: {
      // Session expiry in seconds (7 days)
      expiresIn: 60 * 60 * 24 * 7,
      // Update session on each request
      updateAge: 60 * 60 * 24, // 1 day
    },
  });
};

/**
 * Get the current authenticated user from the session token.
 * Use this in your Convex functions to verify authentication.
 */
export const getAuthUser = authComponent.getAuthUser;
