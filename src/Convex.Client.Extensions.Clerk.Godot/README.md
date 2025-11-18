# Convex.Client.Extensions.Clerk.Godot

Godot desktop application support for Convex Clerk authentication. Provides device code flow with manual token entry fallback for desktop applications.

## Features

- ✅ **Device Code Flow** - OAuth2 device code flow for desktop authentication
- ✅ **Manual Token Entry** - Fallback option for manual token paste
- ✅ **Godot UI Dialog** - Built-in authentication dialog for Godot
- ✅ **Token Caching** - Automatic token caching and refresh
- ✅ **Easy Integration** - Simple extension methods for setup

## Installation

```bash
dotnet add package Convex.Client.Extensions.Clerk.Godot
```

Or add a project reference:

```xml
<ProjectReference Include="..\..\src\Convex.Client.Extensions.Clerk.Godot\Convex.Client.Extensions.Clerk.Godot.csproj" />
```

## Quick Start

### 1. Add Configuration

Add your Clerk configuration to `appsettings.json`:

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

### 2. Initialize in ConvexManager

In your `ConvexManager.cs`:

```csharp
using Convex.Client.Extensions.Clerk;
using Convex.Client.Extensions.Clerk.Godot;

public partial class ConvexManager : Node
{
    private GodotClerkTokenService? _clerkTokenService;
    private ClerkAuthDialog? _authDialog;

    public override void _Ready()
    {
        // Load configuration
        var config = ChatConfiguration.Load(...);
        
        // Initialize Convex client
        Client = config.CreateClientBuilder().Build();
        
        // Set up Clerk authentication
        var clerkOptions = new ClerkOptions
        {
            PublishableKey = config.ClerkPublishableKey, // Load from config
            TokenTemplate = "convex"
        };
        
        _clerkTokenService = new GodotClerkTokenService(clerkOptions);
        
        // Configure Convex client with Clerk auth
        await Client.AddClerkAuthToConvexClientAsync(_clerkTokenService, clerkOptions);
        
        // Show auth dialog if not authenticated
        if (!_clerkTokenService.IsAuthenticated)
        {
            ShowAuthDialog();
        }
    }
    
    private void ShowAuthDialog()
    {
        // Load and show the auth dialog scene
        var dialogScene = GD.Load<PackedScene>("res://ClerkAuthDialog.tscn");
        _authDialog = dialogScene.Instantiate<ClerkAuthDialog>();
        AddChild(_authDialog);
        
        _authDialog.AuthenticationSucceeded += OnAuthenticationSucceeded;
        _authDialog.Initialize(_clerkTokenService!);
        _authDialog.PopupCentered();
    }
    
    private void OnAuthenticationSucceeded()
    {
        GD.Print("Authentication successful!");
        // Continue with your app logic
    }
}
```

### 3. Use in Your Scenes

In your chat scene or other components:

```csharp
using Convex.Client.Extensions.Clerk.Godot;

public partial class ChatScene : Control
{
    private GodotClerkTokenService? _clerkTokenService;
    
    private void OnSignInPressed()
    {
        // Show auth dialog
        var dialogScene = GD.Load<PackedScene>("res://ClerkAuthDialog.tscn");
        var dialog = dialogScene.Instantiate<ClerkAuthDialog>();
        AddChild(dialog);
        dialog.AuthenticationSucceeded += () => {
            GD.Print("Signed in!");
            // Update UI, load data, etc.
        };
        dialog.Initialize(_clerkTokenService!);
        dialog.PopupCentered();
    }
    
    private void OnSignOutPressed()
    {
        _clerkTokenService?.SignOut();
        // Update UI, clear data, etc.
    }
}
```

## How It Works

1. **Device Code Flow**: When authentication is needed, the app requests a device code from Clerk.
2. **User Code Display**: The dialog displays a user code (e.g., "ABC-123") and verification URL.
3. **Browser Authentication**: User opens the URL in their browser and enters the code.
4. **Polling**: The app polls Clerk's API until authentication completes.
5. **Token Retrieval**: Once authenticated, the app retrieves a JWT token for Convex.

## Manual Token Entry

If device code flow fails or the user prefers manual entry:

1. Click "Enter Token Manually" in the dialog
2. Paste your Clerk token from the Clerk dashboard
3. Click "Sign In"

## Configuration Options

### ClerkOptions

- `PublishableKey` - Your Clerk publishable key (required)
- `TokenTemplate` - JWT template name (default: "convex")
- `EnableTokenCaching` - Enable token caching (default: true)
- `TokenCacheExpiration` - Cache expiration time (default: 5 minutes)

## Error Handling

The package handles various error scenarios:

- **Network errors**: Shows error message, allows retry
- **Expired device code**: Automatically restarts flow
- **User cancellation**: Allows manual token entry
- **Invalid token**: Clears auth state, allows restart

## Notes

- Clerk's device code flow API endpoints may need verification against Clerk's actual API
- If Clerk doesn't support standard OAuth2 device code flow, the implementation may need adjustment
- The `ClerkAuthDialog.tscn` scene file should be added to your Godot project
- GodotSharp package is required (automatically included when using Godot.NET.Sdk)

## Requirements

- .NET 8.0 or later
- Godot 4.x with .NET support
- Convex.Client.Extensions.Clerk (automatically included)

## License

MIT

