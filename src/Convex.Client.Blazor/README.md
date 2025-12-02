# Convex.Client.Blazor

Blazor WebAssembly and Server extensions for the Convex .NET Client.

## Features

- **StateHasChanged Integration** - Automatic UI updates when Convex data changes
- **Clerk Authentication** - Seamless Clerk authentication with automatic JS injection
- **Blazor-specific Helpers** - Reactive extensions optimized for Blazor components

## Installation

```bash
dotnet add package Convex.Client.Blazor
```

## Quick Start

### Basic Setup

```csharp
// In Program.cs
builder.Services.AddConvex(options =>
{
    options.DeploymentUrl = "https://your-deployment.convex.cloud";
});

// With Clerk authentication
builder.Services.AddConvexWithClerk(options =>
{
    options.DeploymentUrl = "https://your-deployment.convex.cloud";
});
```

### Using in Components

```razor
@inject IConvexClient ConvexClient

<button @onclick="LoadData">Load</button>

@code {
    private IDisposable? _subscription;

    protected override void OnInitialized()
    {
        _subscription = ConvexClient
            .Subscribe<List<Message>>("messages:list")
            .WithStateHasChanged(this)
            .Subscribe(messages => _messages = messages);
    }

    public void Dispose() => _subscription?.Dispose();
}
```

## License

MIT
