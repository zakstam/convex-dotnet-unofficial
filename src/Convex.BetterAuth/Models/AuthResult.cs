namespace Convex.BetterAuth.Models;

/// <summary>
/// Represents the result of an authentication operation.
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
    /// Creates a successful result.
    /// </summary>
    public static AuthResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static AuthResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}
