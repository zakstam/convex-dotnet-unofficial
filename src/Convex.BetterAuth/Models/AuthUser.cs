using System.Text.Json.Serialization;

namespace Convex.BetterAuth.Models;

/// <summary>
/// Represents an authenticated user from Better Auth.
/// </summary>
public class AuthUser
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// The user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    /// <summary>
    /// The user's display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// URL to the user's profile image.
    /// </summary>
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    /// <summary>
    /// Whether the user's email has been verified.
    /// </summary>
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }
}
