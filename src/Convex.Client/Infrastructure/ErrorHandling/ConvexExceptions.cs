using System.Net;
using System.Text.Json;

namespace Convex.Client.Infrastructure.ErrorHandling;

/// <summary>
/// Base exception for all Convex-related errors.
/// </summary>
public class ConvexException : Exception
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error data.
    /// </summary>
    public JsonElement? ErrorData { get; set; }

    /// <summary>
    /// Gets or sets the request context.
    /// </summary>
    public RequestContext? RequestContext { get; set; }

    /// <summary>
    /// Gets or sets the error details.
    /// </summary>
    public ConvexErrorDetails? ErrorDetails { get; set; }

    /// <summary>
    /// Initializes a new instance of the ConvexException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConvexException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ConvexException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConvexException(string message, Exception? innerException) : base(message, innerException) { }

    /// <summary>
    /// Gets detailed message.
    /// </summary>
    /// <returns>A formatted error message.</returns>
    public string GetDetailedMessage()
    {
        // Lazy initialization of ErrorDetails to avoid circular dependency
        ErrorDetails ??= ConvexErrorDetails.FromException(this);

        if (ErrorDetails != null)
        {
            return $"{Message}\n\n{ErrorDetails.ToFormattedMessage()}";
        }

        return Message;
    }

    /// <summary>
    /// Returns a string representation of the exception with enhanced context.
    /// </summary>
    /// <returns>A string representation of the exception.</returns>
    public override string ToString()
    {
        var baseString = base.ToString();
        
        // Lazy initialization of ErrorDetails to avoid circular dependency
        ErrorDetails ??= ConvexErrorDetails.FromException(this);
        
        if (ErrorDetails != null && ErrorDetails.Suggestions.Count > 0)
        {
            return $"{baseString}\n\n{ErrorDetails.ToFormattedMessage()}";
        }

        return baseString;
    }
}

/// <summary>
/// Exception thrown when a Convex function execution fails.
/// </summary>
public class ConvexFunctionException : ConvexException
{
    /// <summary>
    /// Gets the function name.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Initializes a new instance of the ConvexFunctionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="functionName">The name of the function that failed.</param>
    public ConvexFunctionException(string message, string functionName) : base(message) => FunctionName = functionName;

    /// <summary>
    /// Initializes a new instance of the ConvexFunctionException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="functionName">The name of the function that failed.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConvexFunctionException(string message, string functionName, Exception innerException) : base(message, innerException) => FunctionName = functionName;
}

/// <summary>
/// Exception thrown when function arguments are invalid.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexArgumentException class.
/// </remarks>
/// <param name="message">The error message.</param>
/// <param name="argumentName">The name of the invalid argument.</param>
public class ConvexArgumentException(string message, string argumentName) : ConvexException(message)
{
    /// <summary>
    /// Gets the argument name.
    /// </summary>
    public string ArgumentName { get; } = argumentName;
}

/// <summary>
/// Exception thrown when network-related errors occur.
/// </summary>
public class ConvexNetworkException : ConvexException
{
    /// <summary>
    /// Gets the error type.
    /// </summary>
    public NetworkErrorType ErrorType { get; }

    /// <summary>
    /// Gets or sets the status code.
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the ConvexNetworkException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorType">The type of network error.</param>
    public ConvexNetworkException(string message, NetworkErrorType errorType) : base(message) => ErrorType = errorType;

    /// <summary>
    /// Initializes a new instance of the ConvexNetworkException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorType">The type of network error.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConvexNetworkException(string message, NetworkErrorType errorType, Exception innerException) : base(message, innerException) => ErrorType = errorType;
}

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class ConvexAuthenticationException : ConvexException
{
    /// <summary>
    /// Initializes a new instance of the ConvexAuthenticationException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConvexAuthenticationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ConvexAuthenticationException class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConvexAuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when rate limits are exceeded.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexRateLimitException class.
/// </remarks>
/// <param name="message">The error message.</param>
/// <param name="retryAfter">The time to wait before retrying.</param>
/// <param name="currentLimit">The current rate limit.</param>
public class ConvexRateLimitException(string message, TimeSpan retryAfter, int currentLimit) : ConvexException(message)
{
    /// <summary>
    /// Gets the retry after.
    /// </summary>
    public TimeSpan RetryAfter { get; } = retryAfter;

    /// <summary>
    /// Gets the current limit.
    /// </summary>
    public int CurrentLimit { get; } = currentLimit;
}

/// <summary>
/// Exception thrown when a circuit breaker is open.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexCircuitBreakerException class.
/// </remarks>
/// <param name="message">The error message.</param>
/// <param name="circuitState">The current circuit breaker state.</param>
public class ConvexCircuitBreakerException(string message, CircuitBreakerState circuitState) : ConvexException(message)
{
    /// <summary>
    /// Gets the circuit state.
    /// </summary>
    public CircuitBreakerState CircuitState { get; } = circuitState;
}

/// <summary>
/// Enumeration of network error types.
/// </summary>
public enum NetworkErrorType
{
    /// <summary>
    /// Request timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// DNS resolution failure.
    /// </summary>
    DnsResolution,

    /// <summary>
    /// SSL certificate error.
    /// </summary>
    SslCertificate,

    /// <summary>
    /// General server error.
    /// </summary>
    ServerError,

    /// <summary>
    /// Connection failure.
    /// </summary>
    ConnectionFailure
}

/// <summary>
/// Enumeration of circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed, requests flow normally.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, requests are blocked.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, testing if service has recovered.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Context information for a request.
/// </summary>
public class RequestContext
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string FunctionName { get; set; } = "";

    /// <summary>
    /// Gets or sets the request type.
    /// </summary>
    public string RequestType { get; set; } = "";

    /// <summary>
    /// Gets or sets the unique ID.
    /// </summary>
    public string RequestId { get; set; } = "";

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
