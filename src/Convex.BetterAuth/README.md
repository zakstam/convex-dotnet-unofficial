# Convex.BetterAuth

Better Auth integration for Convex .NET client. Provides out-of-box authentication support with email/password sign-in, sign-up, and session management.

## Installation

```bash
dotnet add package Convex.BetterAuth
```

## Quick Start

### 1. Configure your backend

Set up Better Auth in your Convex backend following the [Better Auth Convex guide](https://github.com/get-convex/better-auth).

### 2. Add services in Program.cs

```csharp
using Convex.Client.Extensions.DependencyInjection;
using Convex.BetterAuth.Extensions;

// Add Convex client
builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));

// Add Better Auth - automatically wires up the token provider
builder.Services.AddConvexBetterAuth(builder.Configuration.GetSection("BetterAuth"));
```

### 3. Configure appsettings.json

```json
{
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud"
  },
  "BetterAuth": {
    "SiteUrl": "https://your-deployment.convex.site"
  }
}
```

> **Note:** `SiteUrl` must use HTTPS. The library enforces this for security.

### 4. Use in your components

```csharp
@inject IBetterAuthService AuthService

@if (!AuthService.IsAuthenticated)
{
    <button @onclick="SignIn">Sign In</button>
}
else
{
    <p>Welcome, @AuthService.CurrentUser?.Name!</p>
    <button @onclick="SignOut">Sign Out</button>
}

@code {
    private async Task SignIn()
    {
        var result = await AuthService.SignInAsync("user@example.com", "password");
        if (!result.IsSuccess)
        {
            Console.WriteLine($"Sign in failed: {result.ErrorMessage}");
        }
    }

    private async Task SignOut()
    {
        await AuthService.SignOutAsync();
    }
}
```

## Blazor WebAssembly Setup

For Blazor WebAssembly, you'll want to persist sessions in browser storage:

```csharp
// Create a localStorage-based session storage
public class LocalStorageSessionStorage : ISessionStorage
{
    private const string StorageKey = "better_auth_session";
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageSessionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task StoreTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
    }

    public async Task RemoveTokenAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}

// Register in Program.cs (before AddConvexBetterAuth)
builder.Services.AddBetterAuthSessionStorage<LocalStorageSessionStorage>();
builder.Services.AddConvexBetterAuth(builder.Configuration.GetSection("BetterAuth"));
```

Don't forget to restore the session on app startup:

```csharp
// In App.razor or a root component
@inject IBetterAuthService AuthService

protected override async Task OnInitializedAsync()
{
    await AuthService.TryRestoreSessionAsync();
}
```

## Features

- **Email/Password Authentication**: Sign up and sign in with email and password
- **Session Management**: Automatic session persistence and restoration
- **Auto-Wired Token Provider**: Token provider is automatically configured with the Convex client - no manual setup required
- **JWT Exchange**: Automatically exchanges Better Auth session tokens for Convex JWTs
- **Event-Driven**: Subscribe to auth state changes via `OnAuthStateChanged` event
- **Pluggable Storage**: Implement `ISessionStorage` for custom token storage

## Security Features

This library includes several security hardening measures:

- **HTTPS Enforced**: `SiteUrl` must use HTTPS - credentials are never sent over unencrypted connections
- **Input Validation**: Email and password are validated before sending to the server
- **Rate Limiting**: Built-in client-side rate limiting prevents rapid repeated auth attempts
- **No Sensitive Logging**: Session tokens and credentials are never logged
- **Generic Error Messages**: Internal exceptions return user-friendly messages without exposing implementation details

## API Reference

### IBetterAuthService

| Method | Description |
|--------|-------------|
| `SignUpAsync(email, password, name?)` | Register a new user |
| `SignInAsync(email, password)` | Sign in an existing user |
| `SignOutAsync()` | Sign out the current user |
| `TryRestoreSessionAsync()` | Restore session from storage |
| `GetSessionToken()` | Get current session token |

### Properties

| Property | Description |
|----------|-------------|
| `IsAuthenticated` | Whether a user is currently signed in |
| `CurrentUser` | The currently authenticated user |
| `CurrentSession` | The current session info |

### Events

| Event | Description |
|-------|-------------|
| `OnAuthStateChanged` | Fired when authentication state changes |

## License

MIT
