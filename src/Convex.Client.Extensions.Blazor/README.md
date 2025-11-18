# Convex.Client.Extensions.Blazor

Blazor-specific extensions for the Convex .NET Client. Provides seamless integration between Convex reactive observables and Blazor's component lifecycle.

## Overview

This package contains Blazor-specific extension methods that make it easy to work with Convex in Blazor WebAssembly and Blazor Server applications. It provides automatic `StateHasChanged` integration, form binding, and async enumerable support.

**Prerequisites**: This package requires `Convex.Client.Extensions` package, which will be automatically installed as a dependency.

## Installation

```bash
dotnet add package Convex.Client.Extensions.Blazor
```

## Features

### StateHasChanged Integration

Automatically trigger UI updates when Convex observables emit new values:

```csharp
using Convex.Client.Extensions.ExtensionMethods;

public partial class MessagesComponent : ComponentBase
{
    [Inject] private IConvexClient Client { get; set; } = default!;
    private Message[] _messages = Array.Empty<Message>();

    protected override void OnInitialized()
    {
        // Automatically calls StateHasChanged when new messages arrive
        Client.Observe<Message[]>("messages:list")
            .SubscribeWithStateHasChanged(this, messages =>
            {
                _messages = messages;
                StateHasChanged();
            });
    }
}
```

**With error handling:**

```csharp
Client.Observe<Message[]>("messages:list")
    .SubscribeWithStateHasChanged(
        this,
        messages => { _messages = messages; StateHasChanged(); },
        error => { _errorMessage = error.Message; StateHasChanged(); });
```

### Async Enumerable Support

Convert Convex observables to `IAsyncEnumerable<T>` for use with `@foreach await` in Blazor:

```razor
@code {
    [Inject] private IConvexClient Client { get; set; } = default!;
    private IAsyncEnumerable<Message> _messages = default!;

    protected override void OnInitialized()
    {
        _messages = Client.Observe<Message[]>("messages:list")
            .SelectMany(messages => messages)
            .ToAsyncEnumerable();
    }
}

<ul>
    @await foreach (var message in _messages)
    {
        <li>@message.Text</li>
    }
</ul>
```

### Form Binding

Create two-way bindings between Convex observables and Blazor forms:

**Simple one-way binding:**

```csharp
using Convex.Client.Extensions.ExtensionMethods;
using System.Reactive.Subjects;

public partial class UserProfileComponent : ComponentBase
{
    [Inject] private IConvexClient Client { get; set; } = default!;
    private User _user = new();

    protected override void OnInitialized()
    {
        // Bind observable to form, automatically updates UI
        Client.Observe<User>("users:current")
            .BindToForm(this, user => _user = user);
    }
}
```

**Two-way binding with save:**

```csharp
using System.Reactive.Subjects;

private User _user = new();
private Subject<User> _formChanges = new();

protected override void OnInitialized()
{
    // Two-way binding: updates form when data changes, saves when user edits
    var binding = Client.Observe<User>("users:current")
        .BindToForm(
            user => _user = user,
            _formChanges,
            async updatedUser => await Client.MutateAsync("users:update", updatedUser));
}

private void OnUserChanged(ChangeEventArgs e)
{
    _user.Name = e.Value?.ToString() ?? "";
    _formChanges.OnNext(_user); // Triggers auto-save with 500ms debounce
}
```

## Platform Support

This package works with:

- ✅ **Blazor WebAssembly** (browser-wasm)
- ✅ **Blazor Server**
- ✅ **MAUI Blazor Hybrid** apps

## Dependencies

This package uses:
- `Microsoft.AspNetCore.Components.Web` (PackageReference for WASM compatibility)
- `Convex.Client.Extensions` (automatic dependency)

## Why a Separate Package?

Blazor-specific extensions are in a separate package to:
1. Keep the core `Convex.Client.Extensions` package WASM-compatible and lightweight
2. Avoid forcing Blazor dependencies on non-Blazor projects (WPF, Console, MAUI non-Blazor)
3. Follow the same pattern as other libraries (ReactiveUI.Blazor, Avalonia.Blazor)

## Related Packages

- [`Convex.Client`](../Convex.Client/README.md) - Core Convex client
- [`Convex.Client.Extensions`](../Convex.Client.Extensions/README.md) - Core extensions (DI, Rx patterns, testing)
- [`Convex.Client.Extensions.AspNetCore`](../Convex.Client.Extensions.AspNetCore/README.md) - ASP.NET Core middleware and health checks

## Requirements

- .NET 8.0 or .NET 9.0
- Blazor WebAssembly or Blazor Server application
- Convex.Client package (via Convex.Client.Extensions dependency)

## Examples

See the [RealtimeChat example](../../examples/RealtimeChat) for a complete Blazor WebAssembly application using these extensions.

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Support

- 📖 [Documentation](https://github.com/zakstam/convex-dotnet)
- 🐛 [Issue Tracker](https://github.com/zakstam/convex-dotnet/issues)
- 💬 [Discord Community](https://convex.dev/community)
