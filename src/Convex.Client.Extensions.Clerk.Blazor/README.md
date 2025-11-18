# Convex.Client.Extensions.Clerk.Blazor

Blazor WebAssembly support for Convex Clerk authentication. Zero-configuration setup with automatic JavaScript injection.

## Features

- ✅ **Zero Configuration** - No manual JavaScript files needed
- ✅ **Automatic Injection** - Clerk JavaScript interop code is automatically injected
- ✅ **One-Line Setup** - Single method call to configure everything
- ✅ **Default Implementation** - `BlazorClerkTokenService` included out of the box

## Installation

```bash
dotnet add package Convex.Client.Extensions.Clerk.Blazor
```

## Quick Start

### 1. Add Configuration

Add your Clerk and Convex configuration to `appsettings.json`:

```json
{
  "Clerk": {
    "PublishableKey": "pk_test_YOUR_CLERK_PUBLISHABLE_KEY_HERE",
    "TokenTemplate": "convex"
  },
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud"
  }
}
```

### 2. Register Services

In your `Program.cs`, add one line:

```csharp
using Convex.Client.Extensions.Clerk.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// That's it! One line to set up everything
builder.Services.AddConvexWithClerkForBlazor(
    builder.Configuration.GetSection("Clerk"),
    builder.Configuration.GetSection("Convex")
);

await builder.Build().RunAsync();
```

### 3. Use in Components

Inject `BlazorClerkTokenService` in your components:

```csharp
@inject BlazorClerkTokenService ClerkTokenService

@code {
    protected override async Task OnInitializedAsync()
    {
        // Initialize Clerk (automatically injects JavaScript)
        await ClerkTokenService.InitializeAsync();
        
        // Check authentication state
        if (ClerkTokenService.IsAuthenticated)
        {
            var userId = await ClerkTokenService.GetUserIdAsync();
            var email = await ClerkTokenService.GetUserEmailAsync();
        }
    }
    
    private async Task SignIn()
    {
        await ClerkTokenService.OpenSignInAsync();
    }
    
    private async Task SignOut()
    {
        await ClerkTokenService.SignOutAsync();
    }
}
```

## How It Works

1. **Automatic JavaScript Injection**: When `BlazorClerkTokenService` is first used, it automatically injects the required Clerk JavaScript interop code from embedded resources.

2. **No Manual Setup**: You don't need to:
   - Copy JavaScript files to `wwwroot`
   - Add script tags to `index.html`
   - Manually configure JavaScript interop

3. **Seamless Integration**: The service handles all the complexity of loading and initializing the Clerk SDK.

## Advanced Usage

### Using Action-Based Configuration

Instead of configuration sections, you can use actions:

```csharp
builder.Services.AddConvexWithClerkForBlazor(
    clerkOptions => {
        clerkOptions.PublishableKey = "pk_test_...";
        clerkOptions.TokenTemplate = "convex";
    },
    convexOptions => {
        convexOptions.DeploymentUrl = "https://your-app.convex.cloud";
    }
);
```

### Custom Token Service

If you need a custom implementation, you can still register your own `IClerkTokenService`:

```csharp
builder.Services.AddScoped<IClerkTokenService, MyCustomClerkTokenService>();
builder.Services.AddConvexWithClerkForBlazor(...);
```

## Migration from Manual Setup

If you're currently using the manual setup with `clerk.js`:

1. **Remove** the `clerk.js` file from `wwwroot/js/`
2. **Remove** the script tag from `index.html`
3. **Remove** your custom `BlazorClerkTokenService` implementation
4. **Replace** service registration with `AddConvexWithClerkForBlazor`

That's it! Everything else stays the same.

## Requirements

- .NET 8.0 or later
- Blazor WebAssembly
- Convex.Client.Extensions.Clerk (automatically included)

## License

MIT

