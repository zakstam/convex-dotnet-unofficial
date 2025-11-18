# GodotRealtimeChatClerk - Clerk Authentication Integration

This example demonstrates how to integrate Clerk authentication with a Godot desktop application using Convex as the backend.

## Overview

This project implements **OAuth 2.0 Authorization Code Flow with PKCE** for secure authentication in a Godot desktop application. Users authenticate through their web browser, and the app receives a JWT token for use with Convex.

### Key Features

- ✅ Browser-based OAuth 2.0 authentication
- ✅ PKCE (Proof Key for Code Exchange) for security
- ✅ Local HTTP callback server for OAuth redirect
- ✅ Automatic browser launch
- ✅ Manual token entry fallback
- ✅ Cross-platform support (Windows, macOS, Linux)
- ✅ JWT token caching (5-minute TTL)

---

## Architecture

### Authentication Flow

```
1. User clicks "Sign In with Clerk" in Godot app
2. App starts local HTTP server on localhost:8080
3. App generates PKCE code_verifier and code_challenge
4. App opens system browser with Clerk authorization URL
5. User logs in via Clerk web UI
6. Clerk redirects to http://localhost:8080/callback?code=xxx
7. App exchanges authorization code for session token
8. App requests JWT token with "convex" template
9. App caches JWT and uses it for Convex queries/mutations
```

### Components

| Component | Purpose |
|-----------|---------|
| **ClerkAuthorizationCodeFlow** | Implements OAuth 2.0 with PKCE |
| **ClerkOAuthCallbackServer** | Local HTTP server for OAuth callback |
| **GodotClerkTokenService** | Token management and authentication orchestration |
| **ClerkAuthDialog** | Godot UI for authentication flow |
| **ConvexManager** | Initializes Clerk and Convex integration |

---

## Setup Instructions

### 1. Prerequisites

- **Godot 4.3+** with .NET support
- **.NET 8.0 SDK** or later
- **Clerk Account** (free tier available)
- **Convex Account** (free tier available)

### 2. Clerk Configuration

#### 2.1 Create Clerk Application

1. Go to [Clerk Dashboard](https://dashboard.clerk.com/)
2. Create a new application
3. Note your **Publishable Key** (starts with `pk_test_` or `pk_live_`)

#### 2.2 Configure OAuth Settings

1. In Clerk Dashboard, go to **Settings** → **Advanced**
2. Enable **OAuth 2.0** features
3. Add redirect URI: `http://localhost:8080/callback`
   - If port 8080 is in use, ports 8081-8089 are tried automatically
   - Add all alternative ports if you want guaranteed access

#### 2.3 Create JWT Template

1. Go to **JWT Templates** in Clerk Dashboard
2. Click **New template** → Select **Convex**
3. **CRITICAL:** Name must be exactly `convex` (do NOT rename!)
4. Save the template
5. Copy the **Issuer URL** (your Frontend API URL)
   - Format: `https://your-instance.clerk.accounts.dev` (development)
   - Format: `https://clerk.yourdomain.com` (production)

### 3. Convex Configuration

#### 3.1 Create auth.config.ts

In your Convex project folder, create `convex/auth.config.ts`:

```typescript
export default {
  providers: [
    {
      domain: "https://your-instance.clerk.accounts.dev", // From Clerk JWT Template Issuer URL
      applicationID: "convex", // Must match JWT template name
    },
  ],
};
```

#### 3.2 Deploy Convex Functions

```bash
cd path/to/your/convex/project
npx convex deploy
```

Note your deployment URL (e.g., `https://handsome-perch-90.convex.cloud`).

### 4. Application Configuration

#### 4.1 Update appsettings.json

Edit `examples/RealTimeChatClerk/appsettings.json`:

```json
{
  "Clerk": {
    "PublishableKey": "pk_test_YOUR_ACTUAL_KEY_HERE",
    "TokenTemplate": "convex",
    "ClerkDomain": "your-instance.clerk.accounts.dev",
    "CallbackPort": 8080,
    "CallbackPath": "/callback"
  },
  "Convex": {
    "DeploymentUrl": "https://your-deployment.convex.cloud",
    "InitialMessageLimit": 10,
    "EnableDebugLogging": true
  }
}
```

**Configuration Options:**

| Option | Required | Description | Example |
|--------|----------|-------------|---------|
| `PublishableKey` | ✅ | Clerk publishable key | `pk_test_xxx` |
| `TokenTemplate` | ✅ | JWT template name | `convex` |
| `ClerkDomain` | ✅ | Clerk Frontend API domain | `your-instance.clerk.accounts.dev` |
| `CallbackPort` | ⚙️ | OAuth callback port | `8080` (default) |
| `CallbackPath` | ⚙️ | OAuth callback path | `/callback` (default) |

#### 4.2 Environment Variables (Alternative)

You can also set configuration via environment variables:

```bash
export CLERK_PUBLISHABLE_KEY="pk_test_xxx"
export CLERK_TOKEN_TEMPLATE="convex"
export CLERK_DOMAIN="your-instance.clerk.accounts.dev"
export CONVEX_DEPLOYMENT_URL="https://your-deployment.convex.cloud"
```

### 5. Build and Run

#### 5.1 Open in Godot

1. Open Godot 4.3+
2. Import project: `examples/RealTimeChatClerk/GodotRealtimeChatClerk/project.godot`
3. Wait for .NET build to complete

#### 5.2 Run the Application

1. Press **F5** or click **Play** in Godot
2. Click **Authenticate** button
3. Browser should open automatically
4. Sign in with Clerk (create account if needed)
5. Browser shows "Authentication Successful" page
6. Return to Godot app - you should be authenticated

---

## Troubleshooting

### Common Issues

#### Port Already in Use

**Symptom:** Error message "Unable to start OAuth callback server. Ports 8080-8089 are all in use."

**Solutions:**
1. Close other applications using ports 8080-8089
2. Change `CallbackPort` in `appsettings.json` to a different port
3. Update redirect URI in Clerk Dashboard to match new port

#### Browser Fails to Open

**Symptom:** Error "Failed to open browser automatically"

**Solutions:**
1. Copy the displayed URL manually
2. Open it in your browser
3. Alternatively, use "Enter Token Manually" option

#### Authentication Times Out

**Symptom:** "Authentication timed out" after 5 minutes

**Solutions:**
1. Ensure you complete sign-in within 5 minutes
2. If browser didn't open, use manual URL
3. Check network connectivity

#### Token Exchange Failed

**Symptom:** "Token exchange failed" or "Missing access_token or session_id"

**Solutions:**
1. Verify Clerk OAuth is enabled (Settings → Advanced)
2. Verify redirect URI matches exactly: `http://localhost:8080/callback`
3. Check Clerk domain in `appsettings.json` is correct
4. Enable `EnableDebugLogging: true` in appsettings.json for detailed logs

#### JWT Token Not Found

**Symptom:** "JWT token not found in response" or "Ensure JWT template 'convex' exists"

**Solutions:**
1. Verify JWT template is named exactly `convex` (case-sensitive)
2. Check template is active in Clerk Dashboard
3. Verify Issuer URL matches `ClerkDomain` in config

#### Backend Auth Rejection

**Symptom:** Convex queries fail with "Unauthorized" error

**Solutions:**
1. Verify `auth.config.ts` has correct Clerk domain
2. Redeploy Convex functions: `npx convex deploy`
3. Check JWT template `applicationID` is `convex`
4. Verify Issuer URL in JWT template matches `domain` in auth.config.ts

### Debug Mode

Enable detailed logging in `appsettings.json`:

```json
{
  "Convex": {
    "EnableDebugLogging": true
  }
}
```

Check Godot console output for detailed error messages and authentication flow logs.

---

## Manual Token Entry (Fallback)

If OAuth flow fails, you can manually enter a JWT token:

### Option 1: Get Token from Clerk Dashboard

1. Go to Clerk Dashboard
2. Navigate to **Users**
3. Select your user
4. Copy session token (may require developer tools)

### Option 2: Get Token from Web Application

If you have a web app using Clerk + Convex:

```javascript
// In browser console
const token = await window.Clerk.session.getToken({ template: "convex" });
console.log(token);
```

Copy the token and paste it into the "Enter Token Manually" option in the Godot app.

**Note:** Manual tokens expire (typically 60 seconds to 1 hour). You'll need to get a new token when it expires.

---

## Security Considerations

### PKCE Implementation

This implementation uses **PKCE (Proof Key for Code Exchange)** to prevent authorization code interception:

- ✅ `code_verifier`: Random 32-byte value, stored locally
- ✅ `code_challenge`: SHA-256 hash of verifier, sent to Clerk
- ✅ Server validates: `SHA256(code_verifier) == stored_code_challenge`

### State Parameter

The `state` parameter provides **CSRF protection**:

- ✅ Random 32-byte value generated per request
- ✅ Validated on callback to ensure same session

### Token Storage

- ⚠️ **In-Memory Only:** Tokens are NOT persisted to disk
- ⚠️ **No Encryption:** Tokens stored in memory (RAM) only
- ✅ **Auto-Expiry:** Tokens expire after 5 minutes (configurable)

**Future Enhancement:** Implement secure token persistence using OS keychain/credential manager.

### Localhost Callback

- ✅ Callback server only listens during authentication (< 5 minutes)
- ✅ Automatically shuts down after receiving callback
- ✅ Only accepts connections from `localhost`
- ⚠️ Port 8080 is standard for OAuth desktop apps (not a security risk)

---

## Advanced Configuration

### Custom Callback Port

If port 8080 is unavailable, configure a custom port:

```json
{
  "Clerk": {
    "CallbackPort": 9000
  }
}
```

**Remember to update Clerk redirect URI:** `http://localhost:9000/callback`

### Custom Token Cache Duration

Default is 5 minutes. To change:

```json
{
  "Clerk": {
    "TokenCacheExpiration": "00:10:00"  // 10 minutes (HH:MM:SS format)
  }
}
```

### Production Deployment

For production, use a custom domain:

1. Set up custom domain in Clerk (e.g., `clerk.yourdomain.com`)
2. Update configuration:
   ```json
   {
     "Clerk": {
       "PublishableKey": "pk_live_YOUR_LIVE_KEY",
       "ClerkDomain": "clerk.yourdomain.com"
     }
   }
   ```
3. Update Convex `auth.config.ts`:
   ```typescript
   {
     domain: "https://clerk.yourdomain.com",
     applicationID: "convex"
   }
   ```

---

## API Reference

### GodotClerkTokenService

```csharp
public class GodotClerkTokenService : IClerkTokenService
{
    // Properties
    bool IsAuthenticated { get; }
    bool IsLoading { get; }

    // Methods
    Task<AuthFlowResult> StartAuthorizationFlowAsync(CancellationToken cancellationToken = default);
    Task<string?> GetTokenAsync(string tokenTemplate = "convex", bool skipCache = false, CancellationToken cancellationToken = default);
    void SetTokenManually(string token);
    void SignOut();
    void ClearCache();
}
```

### AuthFlowResult

```csharp
public class AuthFlowResult
{
    bool Success { get; set; }
    string? ErrorMessage { get; set; }
    string? AuthorizationUrl { get; set; } // Set if browser failed to open
}
```

---

## Architecture Details

For detailed technical information about the authorization code flow implementation, see:

- [CLERK_AUTH_SPEC.md](../../src/Convex.Client.Extensions.Clerk.Godot/CLERK_AUTH_SPEC.md) - Complete technical specification
- [Clerk OAuth Documentation](https://clerk.com/docs/advanced-usage/clerk-idp)
- [RFC 7636 - PKCE](https://datatracker.ietf.org/doc/html/rfc7636)

---

## Differences from Non-Clerk Version

The non-Clerk version (`examples/RealtimeChat/GodotRealtimeChat`) uses simple username-based authentication without any real security. Key differences:

| Feature | RealTimeChatClerk (This) | RealtimeChat (Basic) |
|---------|--------------------------|----------------------|
| Authentication | ✅ Clerk OAuth 2.0 + PKCE | ❌ Simple username only |
| Security | ✅ JWT tokens, secure auth | ❌ No security |
| User Management | ✅ Clerk Dashboard | ❌ None |
| Multi-factor Auth | ✅ Supported via Clerk | ❌ Not supported |
| Social Logins | ✅ Google, GitHub, etc. | ❌ Not supported |
| Production Ready | ✅ Yes | ❌ No (demo only) |

---

## Support & Resources

- **Clerk Documentation:** https://clerk.com/docs
- **Convex Documentation:** https://docs.convex.dev
- **GitHub Issues:** https://github.com/get-convex/convex-dotnet/issues
- **Clerk Community:** https://clerk.com/discord
- **Convex Community:** https://convex.dev/community

---

## License

This example is part of the Convex .NET SDK and is licensed under the same terms as the main project.

---

## Changelog

### Version 1.0.0 (2025-01-10)

- ✅ **Initial Release**
- ✅ OAuth 2.0 Authorization Code Flow with PKCE
- ✅ Clerk integration for Godot desktop apps
- ✅ Local HTTP callback server
- ✅ Cross-platform browser launching (Windows, macOS, Linux)
- ✅ Manual token entry fallback
- ✅ JWT token caching with configurable expiration
- ✅ Comprehensive error handling and user feedback

### Known Limitations

- ❌ Tokens not persisted to disk (user must re-authenticate on app restart)
- ⚠️ Clerk domain must be manually configured (not extracted from publishable key)
- ⚠️ No token refresh flow (tokens expire after cache duration)
- ⚠️ No in-app browser (uses system browser only)

### Future Enhancements

- 🔜 Secure token persistence (OS keychain/credential manager)
- 🔜 Automatic token refresh using refresh tokens
- 🔜 In-app browser for seamless auth experience
- 🔜 Automatic domain extraction from publishable key
- 🔜 QR code authentication for mobile verification
