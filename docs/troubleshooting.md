# Troubleshooting Guide

Common issues and solutions when using the Convex .NET SDK.

## Connection Issues

### Connection Failed

**Error:** `ConvexNetworkException - Failed to connect`

**Possible Causes:**
- Incorrect deployment URL format
- Deployment not running
- Network connectivity issues
- Firewall blocking WebSocket connections

**Solutions:**

1. **Check deployment URL format:**
   ```csharp
   // ✅ Correct
   "https://your-app.convex.cloud"
   
   // ❌ Wrong
   "https://your-app.convex.cloud/api"
   "http://your-app.convex.cloud"
   ```

2. **Verify deployment is running:**
   ```bash
   npx convex deployments
   ```

3. **Test connection:**
   ```bash
   curl https://your-deployment.convex.cloud/api/query
   ```

4. **Check WebSocket support:**
   - Ensure your environment supports WebSocket connections
   - Check firewall/proxy settings
   - Verify SSL/TLS certificates are valid

### Connection Drops Frequently

**Symptoms:** Connection disconnects and reconnects repeatedly

**Solutions:**

1. **Enable auto-reconnect:**
   ```csharp
   var client = new ConvexClientBuilder()
       .UseDeployment("https://your-deployment.convex.cloud")
       .WithAutoReconnect(maxAttempts: 10, delayMs: 2000)
       .Build();
   ```

2. **Monitor connection quality:**
   ```csharp
   client.ConnectionQualityChanges.Subscribe(quality =>
   {
       if (quality == ConnectionQuality.Poor)
       {
           // Reduce real-time updates
       }
   });
   ```

3. **Use resilient subscriptions:**
   ```csharp
   var subscription = client.CreateResilientSubscription<Todo>(
       "todos:subscribe",
       args: new { userId });
   ```

## Function Errors

### Function Not Found

**Error:** "Function not found" or similar

**Cause:** Function name format is incorrect

**Solution:** Function names use **path format** matching your file structure: `"functions/list"` not `"TodosList"`

```csharp
// ✅ Correct - matches file path (convex/functions/list.ts → "functions/list")
client.Query<List<Todo>>("functions/list")
client.Query<List<Todo>>("functions/getById")

// ❌ Wrong
client.Query<List<Todo>>("TodosList")
client.Query<List<Todo>>("todos.list")
```

### Function Not Deployed

**Error:** Function exists in TypeScript but not on server

**Solution:**

1. **Deploy functions:**
   ```bash
   npx convex dev
   # or
   npx convex deploy
   ```

2. **Verify deployment:**
   ```bash
   npx convex functions list
   ```

3. **Check function names:**
   - Ensure function is exported in your TypeScript file
   - Verify function name matches what you're calling in C# (e.g., `"functions/list"`)

## Source Generator Issues

### Constants Not Generating

**Symptoms:** Generated constants not appearing in `Convex.Generated` namespace

**Solutions:**

1. **Verify TypeScript files are included (NOT api.d.ts):**
   ```xml
   <!-- In your .csproj -->
   <ItemGroup>
     <!-- Include actual .ts files, NOT api.d.ts (generator excludes .d.ts files) -->
     <AdditionalFiles Include="../backend/convex/**/*.ts" Exclude="../backend/convex/_generated/**" />
   </ItemGroup>
   ```

2. **Check files exist:**
   - Ensure your TypeScript function files exist at the specified path
   - The generator reads actual `.ts` files, not generated `.d.ts` files

3. **Rebuild project:**
   ```bash
   dotnet clean
   dotnet build
   ```

4. **Check build output:**
   - Look for Convex generator warnings in build output
   - Generated files are in `obj/Debug/generated/Convex.SourceGenerator/`

5. **Enable stricter checks for CI:**
   ```xml
   <PropertyGroup>
     <ConvexDiagnosticMode>error</ConvexDiagnosticMode>
     <!-- or -->
     <ConvexFailOnGeneratorMisconfig>true</ConvexFailOnGeneratorMisconfig>
   </PropertyGroup>
   ```

### Wrong Function Type

**Symptoms:** Functions categorized incorrectly (Query vs Mutation vs Action)

**Solution:** The generator infers types from naming patterns. The constant value is still correct regardless of category. You can use it normally:

```csharp
// Even if categorized wrong, the constant value is correct
var result = await client.Query<List<Todo>>(ConvexFunctions.Mutations.List).ExecuteAsync();
```

See [Source Generator documentation](source-generator.md) for details.

### Model Class Not Found

**Symptoms:** `Note`, `User`, or other model classes not found

**Possible Causes:**

1. **Wrong class name** - Table names are **singularized**:
   - Table `notes` → Class `Note` (not `Notes`)
   - Table `users` → Class `User` (not `Users`)

2. **Missing using directive** - Add `using Convex.Generated;`

3. **Schema not included** - Ensure your schema.ts is in `<AdditionalFiles>`

### CS0246: DateTimeOffset Not Found in Generated `.g.cs` (Godot / ImplicitUsings Disabled)

**Symptoms:** Build fails in generated files with errors like:
- `CS0246: The type or namespace name 'DateTimeOffset' could not be found`

**Cause:** Older generator versions could emit timestamp model properties that relied on implicit global usings.

**Temporary Workarounds (older package versions):**
1. Enable implicit usings in your project:
   ```xml
   <PropertyGroup>
     <ImplicitUsings>enable</ImplicitUsings>
   </PropertyGroup>
   ```
2. Or add a global using:
   ```csharp
   global using System;
   ```

**Permanent Fix:** Upgrade to a version that includes globally qualified `DateTimeOffset` in generated schema code.

### Glob Pattern Including node_modules

**Symptoms:** Build is slow or generator produces unexpected output

**Cause:** Using `**/*.ts` can accidentally include `node_modules` files

**Solution:** Use explicit paths or proper exclusions:

```xml
<!-- ❌ Bad - may include node_modules -->
<AdditionalFiles Include="../backend/**/*.ts" />

<!-- ✅ Good - explicit paths -->
<AdditionalFiles Include="../backend/convex/schema.ts" />
<AdditionalFiles Include="../backend/convex/functions/*.ts" />

<!-- ✅ Also good - proper exclusion -->
<AdditionalFiles Include="../backend/convex/**/*.ts"
                 Exclude="../backend/convex/_generated/**;../backend/node_modules/**" />
```

## Type Mismatch / Serialization Errors

### JsonException or InvalidCastException

**Common Issues:**

| Convex Type    | C# Type      | Fix                                     |
| -------------- | ------------ | --------------------------------------- |
| `number`       | `int`        | Use `double` (JSON numbers are doubles) |
| `bigint`       | `int`        | Use `long`                              |
| `Id<"table">`  | Custom       | Use `string`                            |
| Optional field | Non-nullable | Use `string?`                           |

**Example:**

```csharp
// ❌ Wrong
public record Todo(int Count);

// ✅ Right
public record Todo(double Count);
```

### Null Reference Exceptions

**Cause:** Optional fields in Convex schema not marked as nullable in C#

**Solution:** Use nullable types for optional fields:

```csharp
[ConvexTable]
public class User
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; } // Optional field
    public DateTime? LastLogin { get; set; } // Optional field
}
```

## IntelliSense Issues

### Generated Methods Not Appearing

**Symptoms:** Extension methods from generators not showing in IDE

**Solutions:**

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Restart IDE:**
   - Close and reopen Visual Studio / Rider / VS Code
   - Reload window in VS Code

3. **Clear build cache:**
   ```bash
   dotnet clean
   dotnet build
   ```

4. **Use manual fallback:**
   ```csharp
   // If generated methods don't appear, use manual API
   await client.Query<List<Todo>>("todos:list").ExecuteAsync();
   ```

### Type Inference Issues

**Symptoms:** Generic type parameters not inferred correctly

**Solution:** Explicitly specify types:

```csharp
// If type inference fails, be explicit
var todos = await client.Query<List<Todo>>("todos:list").ExecuteAsync();

// Or for observables
var observable = client.Observe<List<Todo>, object>("todos:search", new { term });
```

## Performance Issues

### Slow Query Execution

**Symptoms:** Queries take too long to execute

**Solutions:**

1. **Add indexes:**
   ```csharp
   [ConvexTable]
   public class Todo
   {
       public string Id { get; set; } = "";
       
       [ConvexIndex]
       public string UserId { get; set; } = ""; // Indexed for fast lookups
   }
   ```

2. **Use pagination for large datasets:**
   ```csharp
   var paginatedQuery = await client.Paginate<Todo>("todos:list", pageSize: 20)
       .InitializeAsync();
   ```

3. **Batch multiple queries:**
   ```csharp
   var (todos, user, stats) = await client.Batch()
       .Query<List<Todo>>("todos:list")
       .Query<User>("users:current")
       .Query<Stats>("dashboard:stats")
       .ExecuteAsync<List<Todo>, User, Stats>();
   ```

### High Memory Usage

**Symptoms:** Application using too much memory

**Solutions:**

1. **Dispose subscriptions properly:**
   ```csharp
   var subscription = client.Observe<List<Todo>>("todos:list")
       .Subscribe(todos => UpdateUI(todos));
   
   // Always dispose when done
   subscription.Dispose();
   ```

2. **Use `ShareReplayLatest()` for multiple subscribers:**
   ```csharp
   var sharedData = client.Observe<List<Todo>>("todos:list")
       .ShareReplayLatest();
   
   // Multiple subscribers share the same subscription
   sharedData.Subscribe(UpdateUI1);
   sharedData.Subscribe(UpdateUI2);
   ```

3. **Limit subscription data:**
   ```csharp
   // Only subscribe to what you need
   client.Observe<List<Todo>>("todos:list")
       .Select(todos => todos.Take(100)) // Limit results
       .Subscribe(UpdateUI);
   ```

## Authentication Issues

### Token Expired

**Error:** Authentication failed or token expired

**Solutions:**

1. **Use token provider for automatic refresh:**
   ```csharp
   public class MyAuthTokenProvider : IAuthTokenProvider
   {
       public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
       {
           // Refresh token if needed
           return await GetValidTokenAsync();
       }
   }
   
   await client.AuthenticationSlice.SetAuthTokenProviderAsync(new MyAuthTokenProvider());
   ```

2. **Check token expiration:**
   - Verify token is not expired
   - Ensure token refresh logic is working
   - Check token format is correct (JWT)

### Admin Auth Not Working

**Error:** Admin authentication fails

**Solutions:**

1. **Verify admin key:**
   ```csharp
   var adminKey = Environment.GetEnvironmentVariable("CONVEX_ADMIN_KEY");
   if (string.IsNullOrEmpty(adminKey))
   {
       throw new InvalidOperationException("CONVEX_ADMIN_KEY not set");
   }
   ```

2. **Only use on server-side:**
   - Never use admin auth in client applications
   - Admin auth is for backend services only

## Still Having Issues?

- Check the [API Reference](api-reference.md) for correct usage
- Review [Source Generator documentation](source-generator.md) for code generation details
- [Report a bug](https://github.com/zakstam/convex-dotnet-unofficial/issues) with details:
  - Error message and stack trace
  - Code that reproduces the issue
  - .NET version and platform
  - Convex deployment URL (if safe to share)
