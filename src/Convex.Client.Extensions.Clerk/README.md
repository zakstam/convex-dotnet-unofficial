# Convex.Client.Extensions.Clerk

Clerk authentication integration for the Convex .NET Client.

## Overview

This package provides seamless Clerk authentication integration for Convex .NET clients, similar to the JavaScript SDK's `convex/react-clerk` package. It includes dependency injection support, token providers, and helper methods for easy integration.

## Installation

```bash
dotnet add package Convex.Client.Extensions.Clerk
```

## Requirements

- .NET 8.0 or .NET 9.0
- Convex .NET Client (`Convex.Client`)
- Clerk account with Convex integration configured
- Clerk JWT template named "convex" configured in your Clerk dashboard

## Quick Start

### Basic Usage with Dependency Injection

```csharp
using Convex.Client.Extensions.Clerk;
using Convex.Client.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register Convex with Clerk authentication
builder.Services.AddConvexWithClerk(
    clerkOptions => {
        clerkOptions.PublishableKey = "pk_test_..."; // For client-side
        // OR
        clerkOptions.SecretKey = "sk_test_..."; // For server-side
        clerkOptions.TokenTemplate = "convex";
    },
    convexOptions => {
        convexOptions.DeploymentUrl = "https://your-app.convex.cloud";
    }
);

var app = builder.Build();
```

### Using Configuration Files

Add configuration to `appsettings.json`:

```json
{
  "Clerk": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "TokenTemplate": "convex",
    "EnableTokenCaching": true,
    "TokenCacheExpiration": "00:05:00"
  },
  "Convex": {
    "DeploymentUrl": "https://your-app.convex.cloud"
  }
}
```

Then register services:

```csharp
builder.Services.AddConvexWithClerk(
    builder.Configuration.GetSection("Clerk"),
    builder.Configuration.GetSection("Convex")
);
```

### Manual Configuration

If you prefer to configure the client manually:

```csharp
using Convex.Client.Extensions.Clerk;

// Create Clerk token provider
var clerkOptions = new ClerkOptions
{
    PublishableKey = "pk_test_...",
    TokenTemplate = "convex"
};

// Implement IClerkTokenService based on your Clerk SDK
var clerkTokenService = new MyClerkTokenService(clerkOptions);
var tokenProvider = new ClerkAuthTokenProvider(clerkTokenService, clerkOptions);

// Configure Convex client
var client = new ConvexClient("https://your-app.convex.cloud");
await client.UseClerkAuthAsync(tokenProvider);
```

## Features

### Token Provider

The `ClerkAuthTokenProvider` implements `IAuthTokenProvider` and handles:

- Automatic token retrieval from Clerk
- Token caching to reduce API calls
- Token expiration and refresh
- Error handling and logging

### Dependency Injection Support

Extension methods for easy integration:

- `AddConvexWithClerk()` - Full integration with both Clerk and Convex
- `AddClerkAuthTokenProvider()` - Just the token provider

### Helper Methods

Convenience extensions for `IConvexClient`:

- `UseClerkAuthAsync()` - Configure client with Clerk authentication

## Configuration Options

### ClerkOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `PublishableKey` | `string?` | `null` | Clerk publishable key (client-side) |
| `SecretKey` | `string?` | `null` | Clerk secret key (server-side) |
| `TokenTemplate` | `string` | `"convex"` | JWT template name |
| `EnableTokenCaching` | `bool` | `true` | Enable token caching |
| `TokenCacheExpiration` | `TimeSpan` | `5 minutes` | Token cache expiration |
| `CustomTokenRetriever` | `Func<CancellationToken, Task<string?>>?` | `null` | Custom token retrieval function |

## Implementing IClerkTokenService

Since Clerk doesn't have an official .NET SDK, you need to implement `IClerkTokenService` based on your Clerk integration approach:

### Option 1: Using CustomTokenRetriever

```csharp
var clerkOptions = new ClerkOptions
{
    CustomTokenRetriever = async (ct) =>
    {
        // Use your Clerk SDK or API client here
        var token = await GetTokenFromClerkAsync("convex", ct);
        return token;
    }
};
```

### Option 2: Custom IClerkTokenService Implementation

```csharp
public class MyClerkTokenService : IClerkTokenService
{
    private readonly MyClerkClient _clerkClient;

    public MyClerkTokenService(MyClerkClient clerkClient)
    {
        _clerkClient = clerkClient;
    }

    public bool IsAuthenticated => _clerkClient.IsSignedIn;
    public bool IsLoading => !_clerkClient.IsLoaded;

    public async Task<string?> GetTokenAsync(
        string template = "convex",
        bool skipCache = false,
        CancellationToken cancellationToken = default)
    {
        return await _clerkClient.GetTokenAsync(template, skipCache, cancellationToken);
    }
}

// Register your implementation
builder.Services.AddScoped<IClerkTokenService, MyClerkTokenService>();
builder.Services.AddClerkAuthTokenProvider();
```

## Platform-Specific Guides

### ASP.NET Core

```csharp
using Convex.Client.Extensions.Clerk;
using Convex.Client.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConvexWithClerk(
    clerkOptions => {
        // Configure Clerk
        clerkOptions.SecretKey = builder.Configuration["Clerk:SecretKey"];
    },
    convexOptions => {
        // Configure Convex
        convexOptions.DeploymentUrl = builder.Configuration["Convex:DeploymentUrl"];
    }
);

var app = builder.Build();

// Use Convex client in your services
app.MapGet("/api/data", async (IConvexClient client) =>
{
    var result = await client.Query<List<MyData>>("data:list").ExecuteAsync();
    return Results.Ok(result);
});

app.Run();
```

### Blazor Server

```csharp
using Convex.Client.Extensions.Clerk;
using Convex.Client.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddConvexWithClerk(
    clerkOptions => {
        clerkOptions.PublishableKey = builder.Configuration["Clerk:PublishableKey"];
    },
    convexOptions => {
        convexOptions.DeploymentUrl = builder.Configuration["Convex:DeploymentUrl"];
    }
);

var app = builder.Build();
```

### Blazor WebAssembly

For Blazor WASM, you'll need to implement token retrieval from the browser:

```csharp
// In your Blazor component or service
public class ClerkTokenService : IClerkTokenService
{
    private readonly IJSRuntime _jsRuntime;

    public ClerkTokenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated { get; private set; }
    public bool IsLoading { get; private set; }

    public async Task<string?> GetTokenAsync(
        string template = "convex",
        bool skipCache = false,
        CancellationToken cancellationToken = default)
    {
        // Call JavaScript Clerk SDK
        return await _jsRuntime.InvokeAsync<string>(
            "clerk.getToken",
            cancellationToken,
            template,
            skipCache);
    }
}
```

### Console Applications

```csharp
using Convex.Client.Extensions.Clerk;

var clerkOptions = new ClerkOptions
{
    SecretKey = Environment.GetEnvironmentVariable("CLERK_SECRET_KEY"),
    TokenTemplate = "convex"
};

var clerkTokenService = new MyClerkTokenService(clerkOptions);
var tokenProvider = new ClerkAuthTokenProvider(clerkTokenService, clerkOptions);

var client = new ConvexClient("https://your-app.convex.cloud");
await client.UseClerkAuthAsync(tokenProvider);

// Use the client
var result = await client.Query<List<Data>>("data:list").ExecuteAsync();
```

## Token Caching

Token caching is enabled by default to reduce Clerk API calls. Tokens are cached for 5 minutes by default.

To disable caching:

```csharp
clerkOptions.EnableTokenCaching = false;
```

To customize cache expiration:

```csharp
clerkOptions.TokenCacheExpiration = TimeSpan.FromMinutes(10);
```

To clear the cache manually:

```csharp
var tokenProvider = serviceProvider.GetRequiredService<ClerkAuthTokenProvider>();
tokenProvider.ClearCache();
```

## Error Handling

The token provider handles errors gracefully:

- Returns `null` if user is not authenticated
- Returns `null` if authentication is still loading
- Throws exceptions for actual errors (network failures, etc.)

Monitor authentication state:

```csharp
client.AuthenticationSlice.AuthenticationStateChanged += (sender, args) =>
{
    Console.WriteLine($"Auth state: {args.State}");
    if (args.ErrorMessage != null)
    {
        Console.WriteLine($"Error: {args.ErrorMessage}");
    }
};
```

## Troubleshooting

### Token Template Not Found

Ensure you've created a JWT template named "convex" in your Clerk dashboard:

1. Go to Clerk Dashboard → JWT Templates
2. Create a new template named "convex"
3. Configure it according to Convex requirements

### Authentication Not Working

1. Verify Clerk keys are correct
2. Check that `TokenTemplate` matches your Clerk template name
3. Ensure `IClerkTokenService` is properly implemented
4. Check logs for authentication errors

### Token Caching Issues

If tokens seem stale:

1. Disable caching temporarily: `EnableTokenCaching = false`
2. Clear cache manually: `tokenProvider.ClearCache()`
3. Adjust `TokenCacheExpiration` if needed

## Examples

See the [examples directory](../../examples) for complete working examples.

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Links

- [GitHub Repository](https://github.com/zakstam/convex-dotnet-unofficial)
- [Convex Documentation](https://docs.convex.dev)
- [Clerk Documentation](https://clerk.com/docs)
- [Convex Clerk Integration Guide](https://docs.convex.dev/auth/clerk)

