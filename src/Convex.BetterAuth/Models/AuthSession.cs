using System.Text.Json.Serialization;
using Convex.BetterAuth.Converters;

namespace Convex.BetterAuth.Models;

/// <summary>
/// Represents an authentication session from Better Auth.
/// </summary>
public class AuthSession
{
    /// <summary>
    /// The unique identifier of the session.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// The session token used for authentication.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = "";

    /// <summary>
    /// When the session expires.
    /// Better Auth returns this as a Unix timestamp in milliseconds.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime ExpiresAt { get; set; }
}
