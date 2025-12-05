using System.Text.Json.Serialization;

namespace Convex.BetterAuth.Models;

/// <summary>
/// Request model for sign-up operations.
/// </summary>
internal class SignUpRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
