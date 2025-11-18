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

**Solution:** Function names use **colon format**: `"todos:list"` not `"TodosList"`

```csharp
// ✅ Correct
client.Query<List<Todo>>("todos:list")
client.Query<List<Todo>>("users:getById")

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
   - Verify function name matches what you're calling in C# (e.g., `"functions/todos:list"`)

## Source Generator Issues

### Constants Not Generating

**Symptoms:** Generated constants not appearing in `Convex.Generated` namespace

**Solutions:**

1. **Verify `api.d.ts` path:**
   ```xml
   <!-- In your .csproj -->
   <ItemGroup>
     <AdditionalFiles Include="../backend/convex/_generated/api.d.ts" />
   </ItemGroup>
   ```

2. **Check file exists:**
   - Ensure `api.d.ts` exists at the specified path
   - Run `npx convex dev` in your backend directory to generate it

3. **Rebuild project:**
   ```bash
   dotnet clean
   dotnet build
   ```

4. **Check build output:**
   - Look for generator messages in build output
   - Generated files are in `obj/Debug/generated/Convex.FunctionGenerator/`

### Wrong Function Type

**Symptoms:** Functions categorized incorrectly (Query vs Mutation vs Action)

**Solution:** The generator infers types from naming patterns. The constant value is still correct regardless of category. You can use it normally:

```csharp
// Even if categorized wrong, the constant value is correct
var result = await client.Query<List<Todo>>(ConvexFunctions.Mutations.List).ExecuteAsync();
```

See [Source Generator documentation](source-generator.md) for details.

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
   var paginatedQuery = await client.CreatePaginatedQuery<Todo>("todos:list")
       .WithPageSize(20)
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
- Review [Transpiler Limitations](transpiler-limitations.md) for code generation issues
- [Report a bug](https://github.com/zakstam/convex-dotnet-unofficial/issues) with details:
  - Error message and stack trace
  - Code that reproduces the issue
  - .NET version and platform
  - Convex deployment URL (if safe to share)

