using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Implements OAuth 2.0 Authorization Code Flow with PKCE for Clerk authentication.
/// </summary>
public class ClerkAuthorizationCodeFlow
{
    private readonly HttpClient _httpClient;
    private readonly string _oauthClientId;
    private readonly string _clerkDomain;

    /// <summary>
    /// PKCE parameters for the current auth flow.
    /// </summary>
    public class PkceParameters
    {
        public string CodeVerifier { get; set; } = string.Empty;
        public string CodeChallenge { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token response from Clerk OAuth endpoint.
    /// Maps snake_case JSON properties from Clerk to PascalCase C# properties.
    /// </summary>
    public class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; set; }
    }

    /// <summary>
    /// JWT token response from Clerk session endpoint.
    /// </summary>
    public class JwtTokenResponse
    {
        public string? Jwt { get; set; }
        public string? Token { get; set; }  // Alternative property name
    }

    /// <summary>
    /// Clerk user/client response containing sessions.
    /// </summary>
    public class ClerkClientResponse
    {
        [JsonPropertyName("sessions")]
        public List<ClerkSession>? Sessions { get; set; }
    }

    /// <summary>
    /// Clerk session information.
    /// </summary>
    public class ClerkSession
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("last_active_at")]
        public long LastActiveAt { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of ClerkAuthorizationCodeFlow.
    /// </summary>
    /// <param name="oauthClientId">The OAuth Client ID from Clerk Dashboard OAuth Application.</param>
    /// <param name="clerkDomain">Clerk domain (e.g., "your-instance.clerk.accounts.dev").</param>
    /// <param name="httpClient">Optional HttpClient instance.</param>
    public ClerkAuthorizationCodeFlow(string oauthClientId, string clerkDomain, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(oauthClientId))
        {
            throw new ArgumentNullException(nameof(oauthClientId));
        }

        if (string.IsNullOrWhiteSpace(clerkDomain))
        {
            throw new ArgumentNullException(nameof(clerkDomain));
        }

        _oauthClientId = oauthClientId;
        _clerkDomain = clerkDomain;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Generates PKCE parameters (code_verifier, code_challenge, state) for authorization request.
    /// </summary>
    /// <returns>PKCE parameters containing code_verifier, code_challenge, and state.</returns>
    public PkceParameters GeneratePkceParameters()
    {
        // Generate code_verifier (random 32-byte value, base64url encoded)
        var verifierBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(verifierBytes);
        }
        var codeVerifier = Base64UrlEncode(verifierBytes);

        // Generate code_challenge (SHA256 of code_verifier, base64url encoded)
        var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Base64UrlEncode(challengeBytes);

        // Generate state (random 32-byte value for CSRF protection)
        var stateBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(stateBytes);
        }
        var state = Base64UrlEncode(stateBytes);

        return new PkceParameters
        {
            CodeVerifier = codeVerifier,
            CodeChallenge = codeChallenge,
            State = state
        };
    }

    /// <summary>
    /// Builds the authorization URL for the OAuth flow.
    /// </summary>
    /// <param name="redirectUri">The redirect URI (e.g., "http://localhost:8080/callback").</param>
    /// <param name="pkceParams">PKCE parameters from GeneratePkceParameters().</param>
    /// <param name="scope">OAuth scopes (optional, Clerk manages scopes internally).</param>
    /// <returns>The authorization URL to open in a browser.</returns>
    public string BuildAuthorizationUrl(
        string redirectUri,
        PkceParameters pkceParams,
        string? scope = null)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "client_id", _oauthClientId },
            { "response_type", "code" },
            { "redirect_uri", redirectUri },
            { "code_challenge", pkceParams.CodeChallenge },
            { "code_challenge_method", "S256" },
            { "state", pkceParams.State }
        };

        // Only add scope if explicitly provided
        if (!string.IsNullOrWhiteSpace(scope))
        {
            queryParams["scope"] = scope;
        }

        var queryString = string.Join("&",
            queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"https://{_clerkDomain}/oauth/authorize?{queryString}";
    }

    /// <summary>
    /// Exchanges the authorization code for an access token.
    /// </summary>
    /// <param name="authorizationCode">The authorization code received from callback.</param>
    /// <param name="redirectUri">The same redirect URI used in authorization request.</param>
    /// <param name="codeVerifier">The code_verifier from PKCE parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token response containing access token and session ID.</returns>
    public async Task<TokenResponse> ExchangeCodeForTokenAsync(
        string authorizationCode,
        string redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = $"https://{_clerkDomain}/oauth/token";

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", authorizationCode },
            { "redirect_uri", redirectUri },
            { "code_verifier", codeVerifier },
            { "client_id", _oauthClientId }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                // Try to parse error response
                TokenResponse? errorResponse = null;
                try
                {
                    errorResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    // Failed to parse, will return generic error
                }

                var errorDetails = $"Status: {response.StatusCode} | ClientId: {_oauthClientId.Substring(0, Math.Min(8, _oauthClientId.Length))}... | Response: {responseBody.Substring(0, Math.Min(200, responseBody.Length))}";

                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
                {
                    errorResponse.ErrorDescription = $"{errorResponse.ErrorDescription} | {errorDetails}";
                    return errorResponse;
                }

                return new TokenResponse
                {
                    Error = "token_exchange_failed",
                    ErrorDescription = $"Failed to exchange authorization code for token. {errorDetails}"
                };
            }

            var successResponseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(
                successResponseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse == null)
            {
                return new TokenResponse
                {
                    Error = "invalid_response",
                    ErrorDescription = $"Failed to parse token response from Clerk. Response: {successResponseBody.Substring(0, Math.Min(500, successResponseBody.Length))}"
                };
            }

            // Log the response for debugging
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Token response received: AccessToken={!string.IsNullOrEmpty(tokenResponse.AccessToken)}, SessionId={!string.IsNullOrEmpty(tokenResponse.SessionId)}, TokenType={tokenResponse.TokenType}, ExpiresIn={tokenResponse.ExpiresIn}");
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Raw response: {successResponseBody.Substring(0, Math.Min(500, successResponseBody.Length))}");

            return tokenResponse;
        }
        catch (HttpRequestException ex)
        {
            return new TokenResponse
            {
                Error = "network_error",
                ErrorDescription = $"Network error during token exchange: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new TokenResponse
            {
                Error = "unexpected_error",
                ErrorDescription = $"Unexpected error during token exchange: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets a JWT token from Clerk using the session and template.
    /// </summary>
    /// <param name="sessionId">The session ID from token response.</param>
    /// <param name="accessToken">The access token from token response.</param>
    /// <param name="tokenTemplate">The JWT template name (default: "convex").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JWT token for use with Convex.</returns>
    public async Task<string> GetJwtTokenAsync(
        string sessionId,
        string accessToken,
        string tokenTemplate = "convex",
        CancellationToken cancellationToken = default)
    {
        // Try Backend API endpoint (works with OAuth access tokens)
        // Backend API: https://api.clerk.com/v1/sessions/{sessionId}/tokens/{template}
        var backendEndpoint = $"https://api.clerk.com/v1/sessions/{sessionId}/tokens/{tokenTemplate}";

        var request = new HttpRequestMessage(HttpMethod.Post, backendEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Requesting JWT template '{tokenTemplate}' from Backend API: {backendEndpoint}");
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Using session ID: {sessionId}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] JWT endpoint response status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] JWT endpoint response: {responseBody.Substring(0, Math.Min(500, responseBody.Length))}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"JWT endpoint returned {(int)response.StatusCode} {response.StatusCode}. " +
                    $"Response: {responseBody}");
            }

            var jsonResponse = System.Text.Json.JsonSerializer.Deserialize<JwtTokenResponse>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (jsonResponse == null)
            {
                throw new InvalidOperationException("Failed to parse JWT token response from Clerk.");
            }

            // Try both property names (jwt or token)
            var jwt = jsonResponse.Jwt ?? jsonResponse.Token;

            if (string.IsNullOrEmpty(jwt))
            {
                throw new InvalidOperationException(
                    $"JWT token not found in response. " +
                    $"Ensure JWT template '{tokenTemplate}' exists in Clerk Dashboard.");
            }

            return jwt;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to get JWT token from Clerk. " +
                $"Verify that JWT template '{tokenTemplate}' is configured in Clerk Dashboard. " +
                $"Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Extracts the session ID from the OIDC id_token JWT by decoding the payload.
    /// </summary>
    /// <param name="idToken">The id_token JWT from OAuth response.</param>
    /// <returns>The session ID from the 'sid' claim, or null if not found.</returns>
    public string? ExtractSessionIdFromIdToken(string idToken)
    {
        try
        {
            // JWT format: header.payload.signature
            var parts = idToken.Split('.');
            if (parts.Length != 3)
            {
                System.Diagnostics.Debug.WriteLine("[ClerkAuthorizationCodeFlow] Invalid JWT format");
                return null;
            }

            // Decode the payload (second part)
            var payload = parts[1];

            // Add padding if needed for base64 decoding
            var paddingNeeded = (4 - (payload.Length % 4)) % 4;
            if (paddingNeeded > 0)
            {
                payload += new string('=', paddingNeeded);
            }

            // Replace URL-safe characters with standard base64 characters
            payload = payload.Replace('-', '+').Replace('_', '/');

            // Decode base64 to get JSON
            var payloadBytes = Convert.FromBase64String(payload);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);

            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Decoded JWT payload: {payloadJson.Substring(0, Math.Min(500, payloadJson.Length))}");

            // Parse JSON to extract 'sid' claim
            var jsonDoc = JsonDocument.Parse(payloadJson);
            if (jsonDoc.RootElement.TryGetProperty("sid", out var sidElement))
            {
                var sessionId = sidElement.GetString();
                System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Extracted session ID from id_token: {sessionId}");
                return sessionId;
            }

            System.Diagnostics.Debug.WriteLine("[ClerkAuthorizationCodeFlow] No 'sid' claim found in id_token");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Failed to extract session ID from id_token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the active session ID using the access token.
    /// This is a fallback method if session ID is not in the id_token.
    /// </summary>
    /// <param name="accessToken">The access token from OAuth response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The active session ID, or null if no active session found.</returns>
    public async Task<string?> GetActiveSessionIdAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        // Clerk's Frontend API endpoint to get client/user info with sessions
        var clientEndpoint = $"https://{_clerkDomain}/v1/client";

        var request = new HttpRequestMessage(HttpMethod.Get, clientEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            _ = response.EnsureSuccessStatusCode();

            var clientResponse = await response.Content.ReadFromJsonAsync<ClerkClientResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (clientResponse?.Sessions == null || clientResponse.Sessions.Count == 0)
            {
                return null;
            }

            // Find the most recently active session with "active" status
            var activeSession = clientResponse.Sessions
                .Where(s => s.Status == "active")
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefault();

            return activeSession?.Id;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClerkAuthorizationCodeFlow] Failed to get active session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Base64URL encoding (RFC 4648 Section 5) for PKCE.
    /// </summary>
    private static string Base64UrlEncode(byte[] data)
    {
        var base64 = Convert.ToBase64String(data);

        // Replace characters as per RFC 4648 Section 5
        base64 = base64.Replace('+', '-');
        base64 = base64.Replace('/', '_');
        base64 = base64.TrimEnd('=');

        return base64;
    }
}
