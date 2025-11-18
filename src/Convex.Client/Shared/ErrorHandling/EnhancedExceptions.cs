using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Convex.Client.Shared.ErrorHandling;

/// <summary>
/// Enhanced exception context providing detailed diagnostic information.
/// </summary>
public class ErrorContext
{
    /// <summary>
    /// Gets or sets the request ID for correlation.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the function name that was being executed.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets or sets the operation type (query, mutation, action).
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional contextual data.
    /// </summary>
    public Dictionary<string, JsonElement?> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the client-side stack trace.
    /// </summary>
    public string? ClientStackTrace { get; set; }

    /// <summary>
    /// Gets or sets the server-side stack trace (if available).
    /// </summary>
    public string? ServerStackTrace { get; set; }

    /// <summary>
    /// Gets a hybrid stack trace combining client and server traces.
    /// </summary>
    public string HybridStackTrace
    {
        get
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(ClientStackTrace))
            {
                _ = sb.AppendLine("=== Client Stack Trace ===");
                _ = sb.AppendLine(ClientStackTrace);
                _ = sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(ServerStackTrace))
            {
                _ = sb.AppendLine("=== Server Stack Trace ===");
                _ = sb.AppendLine(ServerStackTrace);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Adds contextual data to the error.
    /// </summary>
    /// <typeparam name="T">The type of value to add.</typeparam>
    /// <param name="key">The key for the data.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>This ErrorContext for method chaining.</returns>
    public ErrorContext WithData<T>(string key, T? value)
    {
        var jsonString = JsonSerializer.Serialize(value);
        Data[key] = JsonSerializer.Deserialize<JsonElement>(jsonString);
        return this;
    }

    /// <summary>
    /// Creates an error context from a request context.
    /// </summary>
    public static ErrorContext FromRequestContext(RequestContext? requestContext)
    {
        return requestContext == null
            ? throw new ArgumentNullException(nameof(requestContext))
            : new ErrorContext
            {
                RequestId = requestContext.RequestId,
                FunctionName = requestContext.FunctionName,
                OperationType = requestContext.RequestType,
                Timestamp = requestContext.Timestamp
            };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine($"Error Context:");
        _ = sb.AppendLine($"  Request ID: {RequestId ?? "N/A"}");
        _ = sb.AppendLine($"  Function: {FunctionName ?? "N/A"}");
        _ = sb.AppendLine($"  Operation: {OperationType ?? "N/A"}");
        _ = sb.AppendLine($"  Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff}");

        if (Data.Count > 0)
        {
            _ = sb.AppendLine($"  Additional Data:");
            foreach (var kvp in Data)
            {
                _ = sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Enhanced Convex exception with rich error context.
/// </summary>
public class EnhancedConvexException : ConvexException
{
    /// <summary>
    /// Gets the enhanced error context.
    /// </summary>
    public ErrorContext Context { get; }

    /// <summary>
    /// Initializes a new instance of the EnhancedConvexException.
    /// </summary>
    public EnhancedConvexException(string message, ErrorContext? context = null)
        : base(message)
    {
        Context = context ?? new ErrorContext();
        Context.ClientStackTrace = new StackTrace(true).ToString();
    }

    /// <summary>
    /// Initializes a new instance of the EnhancedConvexException with an inner exception.
    /// </summary>
    public EnhancedConvexException(string message, Exception innerException, ErrorContext? context = null)
        : base(message, innerException)
    {
        Context = context ?? new ErrorContext();
        Context.ClientStackTrace = new StackTrace(true).ToString();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine(base.ToString());
        _ = sb.AppendLine();
        _ = sb.AppendLine(Context.ToString());

        if (!string.IsNullOrEmpty(Context.HybridStackTrace))
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine(Context.HybridStackTrace);
        }

        return sb.ToString();
    }
}

/// <summary>
/// Exception thrown when a validation error occurs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexValidationException.
/// </remarks>
public class ConvexValidationException(string message, List<ValidationError>? errors = null, ErrorContext? context = null) : EnhancedConvexException(message, context)
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<ValidationError> ValidationErrors { get; } = errors ?? [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine(base.ToString());

        if (ValidationErrors.Count > 0)
        {
            _ = sb.AppendLine();
            _ = sb.AppendLine("Validation Errors:");
            foreach (var error in ValidationErrors)
            {
                _ = sb.AppendLine($"  - {error}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public record ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Code { get; init; }

    public override string ToString() => $"{Field}: {Message}" + (Code != null ? $" (Code: {Code})" : "");
}

/// <summary>
/// Exception thrown when a timeout occurs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexTimeoutException.
/// </remarks>
public class ConvexTimeoutException(string message, TimeSpan timeout, TimeSpan elapsed, ErrorContext? context = null) : EnhancedConvexException(message, context)
{
    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; } = timeout;

    /// <summary>
    /// Gets the elapsed time before timeout.
    /// </summary>
    public TimeSpan Elapsed { get; } = elapsed;

    public override string ToString() => base.ToString() + $"\nTimeout: {Timeout.TotalSeconds:F1}s, Elapsed: {Elapsed.TotalSeconds:F1}s";

}

/// <summary>
/// Exception thrown when retry limits are exceeded.
/// </summary>
/// <remarks>
/// Initializes a new instance of the ConvexRetryExhaustedException.
/// </remarks>
public class ConvexRetryExhaustedException(string message, int attemptCount, List<Exception>? attempts = null, ErrorContext? context = null) : EnhancedConvexException(message, context)
{
    /// <summary>
    /// Gets the number of retry attempts made.
    /// </summary>
    public int AttemptCount { get; } = attemptCount;

    /// <summary>
    /// Gets the exceptions from each attempt.
    /// </summary>
    public List<Exception> Attempts { get; } = attempts ?? [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine(base.ToString());
        _ = sb.AppendLine($"\nRetry Attempts: {AttemptCount}");

        if (Attempts.Count > 0)
        {
            _ = sb.AppendLine("\nAttempt History:");
            for (var i = 0; i < Attempts.Count; i++)
            {
                _ = sb.AppendLine($"  Attempt {i + 1}: {Attempts[i].Message}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Builder for creating detailed error contexts.
/// </summary>
public class ErrorContextBuilder
{
    private readonly ErrorContext _context = new();

    public ErrorContextBuilder WithRequestId(string requestId)
    {
        _context.RequestId = requestId;
        return this;
    }

    public ErrorContextBuilder WithFunction(string functionName, string operationType)
    {
        _context.FunctionName = functionName;
        _context.OperationType = operationType;
        return this;
    }

    public ErrorContextBuilder WithData<T>(string key, T? value)
    {
        var jsonString = JsonSerializer.Serialize(value);
        _context.Data[key] = JsonSerializer.Deserialize<JsonElement>(jsonString);
        return this;
    }

    public ErrorContextBuilder WithServerStack(string serverStackTrace)
    {
        _context.ServerStackTrace = serverStackTrace;
        return this;
    }

    public ErrorContext Build() => _context;
}
