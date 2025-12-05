using System.Text.Json.Serialization;

namespace Convex.BetterAuth.Models;

/// <summary>
/// Request model for sign-in operations.
/// </summary>
internal class SignInRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}
