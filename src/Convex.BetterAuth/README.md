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

## Unity & Godot Setup (Direct Instantiation)

For Unity, Godot, and other platforms without Microsoft.Extensions.DependencyInjection, instantiate the services directly:

```csharp
using Convex.BetterAuth;
using Convex.Client;

// 1. Create options
var options = new BetterAuthOptions
{
    SiteUrl = "https://your-deployment.convex.site"
};

// 2. Create session storage (implement ISessionStorage for your platform)
var sessionStorage = new InMemorySessionStorage(); // Or your custom storage

// 3. Create HttpClient
var httpClient = new HttpClient();

// 4. Create auth service (logger is optional)
var authService = new BetterAuthService(httpClient, sessionStorage, options);

// 5. Create token provider
var tokenProvider = new BetterAuthTokenProvider(authService, httpClient, options);

// 6. Create Convex client and wire up authentication
var client = new ConvexClientBuilder()
    .UseDeployment("https://your-deployment.convex.cloud")
    .Build();
await client.Auth.SetAuthTokenProviderAsync(tokenProvider);

// Now use authService for sign-in/sign-up and client for Convex operations
var result = await authService.SignInAsync("user@example.com", "password");
if (result.IsSuccess)
{
    // Convex client is now authenticated
    var data = await client.Query<List<Message>>("messages:list");
}
```

### Unity-Specific Notes

For Unity, create a persistent session storage using `PlayerPrefs`:

```csharp
public class UnitySessionStorage : ISessionStorage
{
    private const string StorageKey = "better_auth_session";

    public Task StoreTokenAsync(string token)
    {
        PlayerPrefs.SetString(StorageKey, token);
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
    {
        var token = PlayerPrefs.GetString(StorageKey, null);
        return Task.FromResult(string.IsNullOrEmpty(token) ? null : token);
    }

    public Task RemoveTokenAsync()
    {
        PlayerPrefs.DeleteKey(StorageKey);
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }
}
```

### Godot-Specific Notes

For Godot, use `ConfigFile` or `FileAccess` for token persistence:

```csharp
public class GodotSessionStorage : ISessionStorage
{
    private const string ConfigPath = "user://auth_session.cfg";

    public Task StoreTokenAsync(string token)
    {
        var config = new ConfigFile();
        config.SetValue("auth", "token", token);
        config.Save(ConfigPath);
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
    {
        var config = new ConfigFile();
        if (config.Load(ConfigPath) == Error.Ok)
        {
            return Task.FromResult((string?)config.GetValue("auth", "token").AsString());
        }
        return Task.FromResult<string?>(null);
    }

    public Task RemoveTokenAsync()
    {
        DirAccess.RemoveAbsolute(ConfigPath);
        return Task.CompletedTask;
    }
}
```

## Features

- **Multi-Platform Support**: Works on .NET 8+, Unity, Godot, Xamarin, and any netstandard2.1 platform
- **Email/Password Authentication**: Sign up and sign in with email and password
- **Session Management**: Automatic session persistence and restoration
- **Auto-Wired Token Provider**: Token provider is automatically configured with the Convex client - no manual setup required (DI-enabled platforms)
- **Direct Instantiation**: Create services directly without DI for Unity/Godot
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
