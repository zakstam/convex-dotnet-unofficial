namespace Convex.Client.Shared.ErrorHandling;

/// <summary>
/// Represents an error that occurred during a Convex operation.
/// Provides pattern matching capabilities for different error types.
/// </summary>
public abstract class ConvexError(Exception exception)
{
    /// <summary>
    /// Gets the exception that caused this error.
    /// </summary>
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    /// <summary>
    /// Gets a human-readable error message.
    /// </summary>
    public string Message => Exception.Message;

    /// <summary>
    /// Creates a ConvexError from an exception.
    /// </summary>
    public static ConvexError FromException(Exception exception)
    {
        return exception switch
        {
            ConvexException convexEx => new ConvexFunctionError(convexEx),
            HttpRequestException httpEx => new NetworkError(httpEx),
            TimeoutException timeoutEx => new TimeoutError(timeoutEx),
            OperationCanceledException cancelEx => new CancellationError(cancelEx),
            _ => new UnexpectedError(exception)
        };
    }

    /// <summary>
    /// Matches the error type and executes the corresponding function.
    /// </summary>
    public TResult Match<TResult>(
        Func<ConvexFunctionError, TResult> onConvexError,
        Func<NetworkError, TResult> onNetworkError,
        Func<TimeoutError, TResult> onTimeoutError,
        Func<CancellationError, TResult> onCancellationError,
        Func<UnexpectedError, TResult> onUnexpectedError)
    {
        return this switch
        {
            ConvexFunctionError convex => onConvexError(convex),
            NetworkError network => onNetworkError(network),
            TimeoutError timeout => onTimeoutError(timeout),
            CancellationError cancellation => onCancellationError(cancellation),
            UnexpectedError unexpected => onUnexpectedError(unexpected),
            _ => throw new InvalidOperationException($"Unknown error type: {GetType().Name}")
        };
    }

    /// <summary>
    /// Matches the error type and executes the corresponding function, with a default handler.
    /// </summary>
    public TResult MatchOrDefault<TResult>(
        Func<ConvexFunctionError, TResult> onConvexError,
        Func<NetworkError, TResult> onNetworkError,
        Func<TimeoutError, TResult> onTimeoutError,
        Func<CancellationError, TResult> onCancellationError,
        Func<UnexpectedError, TResult> onUnexpectedError)
    {
        return this switch
        {
            ConvexFunctionError convex => onConvexError(convex),
            NetworkError network => onNetworkError(network),
            TimeoutError timeout => onTimeoutError(timeout),
            CancellationError cancellation => onCancellationError(cancellation),
            UnexpectedError unexpected => onUnexpectedError(unexpected),
            _ => throw new InvalidOperationException($"Unknown error type: {GetType().Name}")
        };
    }

    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    public override string ToString() => $"{GetType().Name}: {Message}";
}

/// <summary>
/// Represents an error returned by a Convex function.
/// </summary>
public sealed class ConvexFunctionError : ConvexError
{
    /// <summary>
    /// Gets the Convex exception.
    /// </summary>
    public new ConvexException Exception => (ConvexException)base.Exception;

    /// <summary>
    /// Gets the error data returned from the Convex function.
    /// </summary>
    public object? ErrorData => Exception.ErrorData;

    internal ConvexFunctionError(ConvexException exception) : base(exception)
    {
    }
}

/// <summary>
/// Represents a network-related error (connection issues, DNS failures, etc.).
/// </summary>
public sealed class NetworkError : ConvexError
{
    /// <summary>
    /// Gets the HTTP request exception.
    /// </summary>
    public new HttpRequestException Exception => (HttpRequestException)base.Exception;

    internal NetworkError(HttpRequestException exception) : base(exception)
    {
    }
}

/// <summary>
/// Represents a timeout error (operation took too long).
/// </summary>
public sealed class TimeoutError : ConvexError
{
    /// <summary>
    /// Gets the timeout exception.
    /// </summary>
    public new TimeoutException Exception => (TimeoutException)base.Exception;

    internal TimeoutError(TimeoutException exception) : base(exception)
    {
    }
}

/// <summary>
/// Represents a cancellation error (operation was cancelled by user).
/// </summary>
public sealed class CancellationError : ConvexError
{
    /// <summary>
    /// Gets the operation cancelled exception.
    /// </summary>
    public new OperationCanceledException Exception => (OperationCanceledException)base.Exception;

    internal CancellationError(OperationCanceledException exception) : base(exception)
    {
    }
}

/// <summary>
/// Represents an unexpected error that doesn't fit other categories.
/// </summary>
public sealed class UnexpectedError : ConvexError
{
    internal UnexpectedError(Exception exception) : base(exception)
    {
    }
}
