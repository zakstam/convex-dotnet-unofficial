// Authentication helper functions for Convex functions

import { Auth } from "convex/server";

/**
 * Requires authentication and returns the user identity.
 * Throws an error if the user is not authenticated.
 */
export async function requireAuth(auth: Auth) {
  const identity = await auth.getUserIdentity();
  if (!identity) {
    throw new Error("Authentication required");
  }
  return identity;
}

/**
 * Gets the authenticated user's identifier.
 * For Clerk, this will be the user's Clerk user ID (subject).
 */
export async function getAuthenticatedUserId(auth: Auth): Promise<string> {
  const identity = await requireAuth(auth);
  // Use subject (Clerk user ID) as the user identifier
  return identity.subject;
}

/**
 * Gets the authenticated user's email if available.
 */
export async function getAuthenticatedUserEmail(auth: Auth): Promise<string | undefined> {
  const identity = await requireAuth(auth);
  return identity.email;
}

/**
 * Gets the authenticated user's name if available.
 */
export async function getAuthenticatedUserName(auth: Auth): Promise<string | undefined> {
  const identity = await requireAuth(auth);
  return identity.name;
}

