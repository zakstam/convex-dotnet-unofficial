/**
 * Convex Authentication Configuration
 *
 * For OAuth desktop applications, we configure Convex to validate OAuth id_tokens
 * by treating Clerk as a custom JWT issuer with OIDC discovery.
 *
 * Convex will automatically discover the JWKS endpoint from:
 * https://tough-foal-55.clerk.accounts.dev/.well-known/openid-configuration
 */

import { AuthConfig } from "convex/server";

export default {
  providers: [
    {
      // Custom JWT provider for Clerk session tokens (Blazor WebAssembly)
      // These tokens have aud="convex" (the template name)
      type: "customJwt" as const,
      applicationID: "convex",
      issuer: "https://tough-foal-55.clerk.accounts.dev",
      jwks: "https://tough-foal-55.clerk.accounts.dev/.well-known/jwks.json",
      algorithm: "RS256" as const,
    },
    {
      // Custom JWT provider for OAuth id_tokens (Godot desktop app)
      // These tokens have aud="Whih9OvNnVMAIceh" (the OAuth Client ID)
      type: "customJwt" as const,
      applicationID: "Whih9OvNnVMAIceh",
      issuer: "https://tough-foal-55.clerk.accounts.dev",
      jwks: "https://tough-foal-55.clerk.accounts.dev/.well-known/jwks.json",
      algorithm: "RS256" as const,
    },
  ],
} satisfies AuthConfig;
