# Convex.Client.Extensions.AspNetCore

ASP.NET Core integration for the Convex .NET Client.

## Overview

This package provides ASP.NET Core-specific extensions for the Convex .NET Client, including:

- **Authentication Middleware**: Automatically extract JWT tokens from HTTP requests
- **Health Checks**: Monitor Convex client connectivity and performance
- **HttpContext Integration**: Seamless integration with ASP.NET Core request pipeline

## Installation

```bash
dotnet add package Convex.Client.Extensions.AspNetCore
```

**Note**: This package automatically includes `Convex.Client.Extensions` as a dependency.

## Requirements

- .NET 8.0 or .NET 9.0
- ASP.NET Core server runtime (not compatible with Blazor WebAssembly)

## Features

### Authentication Middleware

Automatically extract and set Convex authentication tokens from HTTP requests:

```csharp
// Program.cs
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Extensions.AspNetCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Register Convex client
builder.Services.AddConvex("https://your-deployment.convex.cloud");

var app = builder.Build();

// Add Convex authentication middleware
app.UseConvexAuth(options =>
{
    options.LogAuthenticationChanges = true;
    options.RejectInvalidTokens = false;
    options.ClearAuthIfNoToken = false;
});

app.Run();
```

**Middleware Options**:
- `LogAuthenticationChanges`: Log when auth tokens are set/cleared (default: false)
- `RejectInvalidTokens`: Return 401 for invalid tokens (default: false)
- `ClearAuthIfNoToken`: Clear auth when no token present (default: false)
- `CustomTokenHeader`: Extract token from custom header
- `CustomAuthScheme`: Support custom authentication schemes
- `CustomTokenExtractor`: Custom token extraction logic

### Health Checks

Monitor Convex client connectivity:

```csharp
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Extensions.AspNetCore.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConvex("https://your-deployment.convex.cloud");

// Add Convex health check
builder.Services.AddHealthChecks()
    .AddConvexCheck(
        name: "convex",
        options: new ConvexHealthCheckOptions
        {
            HealthCheckFunctionName = "health:ping", // Optional: custom health check function
            HealthCheckArgs = null
        });

var app = builder.Build();

app.MapHealthChecks("/health");
app.Run();
```

## Usage with Blazor WebAssembly

**Important**: This package is for ASP.NET Core **server** projects only. For Blazor WebAssembly projects, use only the `Convex.Client.Extensions` package:

```bash
# Blazor WASM - DO NOT use AspNetCore package
dotnet add package Convex.Client.Extensions
```

## Migration from v1.x

If you were using `Convex.Client.Extensions` v1.x with ASP.NET Core features:

1. Install the AspNetCore package:
   ```bash
   dotnet add package Convex.Client.Extensions.AspNetCore --version 2.0.0
   ```

2. Update namespace imports:
   ```csharp
   // OLD
   using Convex.Client.Extensions.Middleware;
   using Convex.Client.Extensions.HealthChecks;

   // NEW
   using Convex.Client.Extensions.AspNetCore.Middleware;
   using Convex.Client.Extensions.AspNetCore.HealthChecks;
   ```

3. Keep using `Convex.Client.Extensions.DependencyInjection` for `AddConvex()` (unchanged)

## Example: Full Integration

```csharp
using Convex.Client.Extensions.DependencyInjection;
using Convex.Client.Extensions.AspNetCore.Middleware;
using Convex.Client.Extensions.AspNetCore.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Register Convex client
builder.Services.AddConvex(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks()
    .AddConvexCheck();

var app = builder.Build();

// Add authentication middleware (before authorization)
app.UseConvexAuth(options =>
{
    options.LogAuthenticationChanges = app.Environment.IsDevelopment();
    options.RejectInvalidTokens = true;
});

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
```

## Configuration

Add Convex configuration to `appsettings.json`:

```json
{
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud",
    "Timeout": "00:00:30"
  }
}
```

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Links

- [GitHub Repository](https://github.com/zakstam/convex-dotnet)
- [Core Extensions Package](https://www.nuget.org/packages/Convex.Client.Extensions)
- [Convex Documentation](https://docs.convex.dev)
