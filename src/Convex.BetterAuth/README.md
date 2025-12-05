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
using Convex.BetterAuth.Extensions;

// Add Convex client
builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));

// Add Better Auth - reads from "BetterAuth" configuration section
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

## Features

- **Email/Password Authentication**: Sign up and sign in with email and password
- **Session Management**: Automatic session persistence and restoration
- **Token Provider**: Seamlessly integrates with Convex client authentication
- **Event-Driven**: Subscribe to auth state changes via `OnAuthStateChanged` event

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
