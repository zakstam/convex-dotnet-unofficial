# Convex.Client Integration Tests

Integration tests for the Convex.Client library against a real Convex backend.

## Prerequisites

1. Node.js 18+ installed
2. A Convex account (free tier works)
3. .NET 9.0 SDK

## Setup

### 1. Initialize the Test Backend

Navigate to the test backend directory and initialize Convex:

```bash
cd tests/convex-test-backend
npm install
npx convex dev
```

This will:
- Create a new Convex project (or link to existing)
- Deploy the test functions
- Provide you with a deployment URL

### 2. Create Backend Functions

Create the following files in `tests/convex-test-backend/convex/`:

**schema.ts:**
```typescript
import { defineSchema, defineTable } from "convex/server";
import { v } from "convex/values";

export default defineSchema({
  testItems: defineTable({
    testRunId: v.string(),
    name: v.string(),
    value: v.number(),
  }).index("by_testRunId", ["testRunId"]),
});
```

**testQueries.ts:**
```typescript
import { query } from "./_generated/server";
import { v } from "convex/values";

export const list = query({
  args: { testRunId: v.string() },
  handler: async (ctx, args) => {
    return await ctx.db
      .query("testItems")
      .withIndex("by_testRunId", (q) => q.eq("testRunId", args.testRunId))
      .collect();
  },
});

export const get = query({
  args: { id: v.id("testItems") },
  handler: async (ctx, args) => {
    return await ctx.db.get(args.id);
  },
});
```

**testMutations.ts:**
```typescript
import { mutation } from "./_generated/server";
import { v } from "convex/values";

export const create = mutation({
  args: { testRunId: v.string(), name: v.string(), value: v.number() },
  handler: async (ctx, args) => {
    return await ctx.db.insert("testItems", args);
  },
});

export const update = mutation({
  args: {
    id: v.id("testItems"),
    name: v.optional(v.string()),
    value: v.optional(v.number())
  },
  handler: async (ctx, args) => {
    const { id, ...updates } = args;
    await ctx.db.patch(id, updates);
  },
});

export const deleteItem = mutation({
  args: { id: v.id("testItems") },
  handler: async (ctx, args) => {
    await ctx.db.delete(args.id);
  },
});

export const cleanup = mutation({
  args: { testRunId: v.string() },
  handler: async (ctx, args) => {
    const items = await ctx.db
      .query("testItems")
      .withIndex("by_testRunId", (q) => q.eq("testRunId", args.testRunId))
      .collect();
    for (const item of items) {
      await ctx.db.delete(item._id);
    }
    return items.length;
  },
});
```

### 3. Configure the Deployment URL

Choose one of these methods:

**Option A: Environment Variable (recommended for CI)**
```bash
export CONVEX_DEPLOYMENT_URL="https://your-deployment.convex.cloud"
```

**Option B: Local Configuration (recommended for development)**

Create `appsettings.Development.json` in this directory:
```json
{
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud"
  }
}
```

This file is gitignored and won't be committed.

## Running Tests

### Run All Integration Tests
```bash
dotnet test tests/Convex.Client.Tests.Integration
```

### Run Specific Test Class
```bash
dotnet test tests/Convex.Client.Tests.Integration --filter "FullyQualifiedName~QueryTests"
```

### Skip Integration Tests (Unit Tests Only)
```bash
dotnet test --filter "Category!=Integration"
```

## Test Isolation

Each test run uses a unique `TestRunId` (GUID) to isolate test data. The fixture automatically cleans up all items with the test run's ID after tests complete.

## Troubleshooting

### Tests fail with "Convex deployment URL not configured"
Ensure you've either:
- Set the `CONVEX_DEPLOYMENT_URL` environment variable, OR
- Created `appsettings.Development.json` with your deployment URL

### Tests fail with connection errors
1. Verify your Convex backend is deployed: `npx convex dev`
2. Check the deployment URL is correct
3. Ensure the test functions are deployed

### Tests hang or timeout
The default timeout is 30 seconds. If your Convex backend is slow to respond, you may need to increase the timeout in `ConvexFixture.cs`.
