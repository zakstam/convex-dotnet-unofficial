import { httpRouter } from "convex/server";
import { authComponent, createAuth } from "./auth";

const http = httpRouter();

// Register Better Auth routes - handles /api/auth/* endpoints
// Enable CORS for cross-origin requests from Blazor frontend
authComponent.registerRoutes(http, createAuth, {
  cors: {
    allowedOrigins: [
      "http://localhost:5004",
      "https://localhost:7135",
    ],
    allowedHeaders: ["Content-Type", "Authorization"],
    exposedHeaders: ["Content-Length"],
  },
});

export default http;
