namespace Convex.BetterAuth.Models;

/// <summary>
/// Represents auth result.
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// The error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    private AuthResult() { }

    /// <summary>
    /// Gets the success.
    /// </summary>
    public static AuthResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Gets the failure.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static AuthResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
