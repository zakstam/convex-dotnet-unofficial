# Convex.Client.Extensions.DependencyInjection

Dependency Injection extensions for the Convex .NET Client, providing simplified configuration and registration patterns for .NET applications.

## Installation

```bash
dotnet add package Convex.Client.Extensions.DependencyInjection
```

## Features

- **Simple Registration**: `AddConvex()` extension methods for `IServiceCollection`
- **Configuration Support**: Bind configuration from `appsettings.json` or environment variables
- **Named Clients**: Support for multiple Convex deployments with `IConvexClientFactory`
- **Typed Clients**: Strongly-typed client configuration patterns
- **Auto-Configuration**: Automatic client builder setup with sensible defaults

## Quick Start

### Basic Usage

```csharp
using Convex.Client.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Simple registration with inline options
builder.Services.AddConvex(options =>
{
    options.DeploymentUrl = "https://happy-animal-123.convex.cloud";
    options.EnableAutoReconnect = true;
    options.MaxReconnectAttempts = 5;
});

var app = builder.Build();
```

### Configuration from appsettings.json

```json
{
  "Convex": {
    "DeploymentUrl": "https://happy-animal-123.convex.cloud",
    "EnableAutoReconnect": true,
    "MaxReconnectAttempts": 5,
    "ReconnectDelayMs": 1000
  }
}
```

```csharp
builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));
```

### Using IConvexClient in Your Services

```csharp
public class ChatService
{
    private readonly IConvexClient _client;

    public ChatService(IConvexClient client)
    {
        _client = client;
    }

    public async Task<List<Message>> GetMessagesAsync()
    {
        return await _client.Query<List<Message>>("messages:list")
            .ExecuteAsync();
    }
}
```

### Named Clients

```csharp
// Registration
builder.Services.AddConvex("primary", options =>
{
    options.DeploymentUrl = "https://primary-deployment.convex.cloud";
});

builder.Services.AddConvex("secondary", options =>
{
    options.DeploymentUrl = "https://secondary-deployment.convex.cloud";
});

// Usage
public class MultiDeploymentService
{
    private readonly IConvexClient _primaryClient;
    private readonly IConvexClient _secondaryClient;

    public MultiDeploymentService(IConvexClientFactory factory)
    {
        _primaryClient = factory.CreateClient("primary");
        _secondaryClient = factory.CreateClient("secondary");
    }
}
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DeploymentUrl` | `string` | *required* | Convex deployment URL |
| `EnableAutoReconnect` | `bool` | `true` | Enable automatic reconnection |
| `MaxReconnectAttempts` | `int` | `5` | Maximum reconnection attempts |
| `ReconnectDelayMs` | `int` | `1000` | Delay between reconnection attempts (ms) |
| `EnableLogging` | `bool` | `true` | Enable logging integration |

## Benefits vs. Manual Registration

### Before (Manual Registration)

```csharp
services.AddSingleton<IConvexClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ConvexClient>>();
    var deploymentUrl = configuration["Convex:DeploymentUrl"];

    return new ConvexClientBuilder()
        .UseDeployment(deploymentUrl!)
        .WithLogging(logger)
        .WithAutoReconnect(maxAttempts: 5, delayMs: 1000)
        .Build();
});
```

### After (With Extensions)

```csharp
services.AddConvex(configuration.GetSection("Convex"));
```

**Result:** 50% less boilerplate code, improved readability, and standardized configuration patterns.

## License

MIT
