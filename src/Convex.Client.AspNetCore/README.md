# Convex.Client.AspNetCore

ASP.NET Core extensions for the Convex .NET Client.

## Features

- **Authentication Middleware** - Automatic token validation and user context
- **Health Checks** - Monitor Convex connection status in your health endpoints
- **Server-side Integration** - Optimized for ASP.NET Core applications

## Installation

```bash
dotnet add package Convex.Client.AspNetCore
```

## Quick Start

### Basic Setup

```csharp
// In Program.cs
builder.Services.AddConvex(options =>
{
    options.DeploymentUrl = "https://your-deployment.convex.cloud";
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddConvexHealthCheck();

// Configure the HTTP request pipeline
app.UseConvexAuth(); // Optional: validates auth tokens
app.MapHealthChecks("/health");
```

### Health Checks

```csharp
// Register Convex health check
builder.Services.AddHealthChecks()
    .AddConvexHealthCheck(
        name: "convex",
        tags: new[] { "ready", "live" });
```

### Authentication Middleware

```csharp
// Add authentication middleware
app.UseConvexAuth();

// Access user context in controllers
[ApiController]
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId = HttpContext.Items["ConvexUserId"];
        // ...
    }
}
```

## License

MIT
