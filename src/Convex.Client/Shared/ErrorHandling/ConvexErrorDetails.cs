using System.Text.Json;

namespace Convex.Client.Shared.ErrorHandling;

/// <summary>
/// Provides structured error information with context and suggestions.
/// </summary>
public class ConvexErrorDetails
{
    /// <summary>
    /// Gets or sets the function name that failed.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets or sets the request type (query, mutation, action).
    /// </summary>
    public string? RequestType { get; set; }

    /// <summary>
    /// Gets or sets the arguments that were passed to the function.
    /// </summary>
    public object? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the request ID, if available.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets additional error data from the server.
    /// </summary>
    public JsonElement? ErrorData { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code, if applicable.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets suggested actions to resolve the error.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Creates error details from a ConvexException.
    /// This method should be called after the exception is fully constructed.
    /// </summary>
    /// <param name="exception">The exception to extract details from.</param>
    /// <returns>Error details with context and suggestions.</returns>
    public static ConvexErrorDetails FromException(ConvexException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var details = new ConvexErrorDetails
        {
            ErrorData = exception.ErrorData,
            Timestamp = DateTimeOffset.UtcNow
        };

        if (exception.RequestContext != null)
        {
            details.FunctionName = exception.RequestContext.FunctionName;
            details.RequestType = exception.RequestContext.RequestType;
            details.RequestId = exception.RequestContext.RequestId;
            details.Timestamp = exception.RequestContext.Timestamp;
        }

        // Add suggestions based on error type
        details.Suggestions.AddRange(GetSuggestions(exception));

        return details;
    }

    private static IEnumerable<string> GetSuggestions(ConvexException exception)
    {
        var suggestions = new List<string>();

        switch (exception)
        {
            case ConvexFunctionException funcEx:
                suggestions.Add($"Check the function '{funcEx.FunctionName}' implementation on the Convex backend.");
                suggestions.Add("Verify the function arguments match the expected schema.");
                suggestions.Add("Check Convex dashboard logs for detailed error information.");
                break;

            case ConvexArgumentException argEx:
                suggestions.Add($"Verify the '{argEx.ArgumentName}' argument is valid.");
                suggestions.Add("Check the function signature in your Convex backend.");
                break;

            case ConvexNetworkException netEx:
                suggestions.Add("Check your internet connection.");
                suggestions.Add("Verify the Convex deployment URL is correct.");
                if (netEx.ErrorType == NetworkErrorType.Timeout)
                {
                    suggestions.Add("Consider increasing the timeout or checking server load.");
                }
                break;

            case ConvexAuthenticationException:
                suggestions.Add("Verify your authentication token is valid and not expired.");
                suggestions.Add("Check if authentication is required for this function.");
                break;

            case ConvexRateLimitException rateEx:
                suggestions.Add($"Wait {rateEx.RetryAfter.TotalSeconds} seconds before retrying.");
                suggestions.Add("Consider implementing exponential backoff for retries.");
                break;

            case ConvexCircuitBreakerException:
                suggestions.Add("The circuit breaker is open. Wait before retrying.");
                suggestions.Add("Check if the Convex service is experiencing issues.");
                break;
        }

        return suggestions;
    }

    /// <summary>
    /// Formats the error details as a human-readable message.
    /// </summary>
    /// <returns>A formatted error message with context and suggestions.</returns>
    public string ToFormattedMessage()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(FunctionName))
        {
            parts.Add($"Function: {FunctionName}");
        }

        if (!string.IsNullOrEmpty(RequestType))
        {
            parts.Add($"Type: {RequestType}");
        }

        if (StatusCode.HasValue)
        {
            parts.Add($"HTTP Status: {StatusCode}");
        }

        if (Timestamp != default)
        {
            parts.Add($"Time: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        }

        var context = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";

        var message = $"Error occurred{context}";

        if (Suggestions.Count > 0)
        {
            message += "\n\nSuggestions:\n" + string.Join("\n", Suggestions.Select(s => $"  • {s}"));
        }

        return message;
    }
}

