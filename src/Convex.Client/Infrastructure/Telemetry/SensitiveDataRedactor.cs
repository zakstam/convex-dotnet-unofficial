using System.Text.RegularExpressions;

namespace Convex.Client.Infrastructure.Telemetry;

/// <summary>
/// Redacts secrets before values are written to logs.
/// </summary>
public static class SensitiveDataRedactor
{
#pragma warning disable SYSLIB1045
    private const string Redacted = "[REDACTED]";

    private static readonly Regex SensitiveJsonPropertyPattern = new(
        "(\\\"(?:authorization|cookie|set-cookie|better-auth-cookie|set-better-auth-cookie|token|access_token|refresh_token|id_token|password|secret|apiKey|api_key|adminKey|admin_key)\\\"\\s*:\\s*)\\\"[^\\\"]*\\\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BearerTokenPattern = new(
        "Bearer\\s+[A-Za-z0-9._~+/=-]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CookieValuePattern = new(
        "((?:__Secure-|__Host-)?(?:better-auth|better_auth)\\.session_token(?:\\.sig)?=)[^;,.\\s]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HeaderAssignmentPattern = new(
        "((?:Authorization|Cookie|Set-Cookie|better-auth-cookie|Set-Better-Auth-Cookie)\\s*[=:]\\s*)[^,;\\r\\n]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Redacts sensitive token-like values from log text.
    /// </summary>
    public static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var redacted = SensitiveJsonPropertyPattern.Replace(value, $"$1\"{Redacted}\"");
        redacted = BearerTokenPattern.Replace(redacted, $"Bearer {Redacted}");
        redacted = CookieValuePattern.Replace(redacted, $"$1{Redacted}");
        redacted = HeaderAssignmentPattern.Replace(redacted, $"$1{Redacted}");
        return redacted;
    }

    /// <summary>
    /// Redacts a header value when the header name is sensitive.
    /// </summary>
    public static string RedactHeader(string headerName, string headerValue)
    {
        if (IsSensitiveName(headerName))
        {
            return Redacted;
        }

        return Redact(headerValue) ?? string.Empty;
    }

    /// <summary>
    /// Redacts a sequence of HTTP headers for logging.
    /// </summary>
    public static string RedactHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers) =>
        string.Join(", ", headers.Select(h => $"{h.Key}={RedactHeader(h.Key, string.Join(",", h.Value))}"));

#pragma warning restore SYSLIB1045

    private static bool IsSensitiveName(string name) =>
        name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("better-auth-cookie", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Set-Better-Auth-Cookie", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("api-key", StringComparison.OrdinalIgnoreCase);
}
