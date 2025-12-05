using System.Text.Json.Serialization;

namespace Convex.BetterAuth.Models;

/// <summary>
/// Response model for sign-up and sign-in operations.
/// Better Auth returns token directly, not wrapped in a session object.
/// </summary>
internal class AuthResponse
{
    [JsonPropertyName("user")]
    public AuthUser? User { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("session")]
    public AuthSession? Session { get; set; }
}
