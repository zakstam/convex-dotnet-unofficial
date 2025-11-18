namespace Convex.Client.Shared.Middleware;

/// <summary>
/// Represents a response from a Convex function.
/// Used by middleware to inspect and transform responses.
/// </summary>
public sealed class ConvexResponse
{
    /// <summary>
    /// Gets or sets the response value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the response type.
    /// </summary>
    public Type? ValueType { get; set; }

    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error that occurred, if any.
    /// </summary>
    public Exception? Error { get; set; }

    /// <summary>
    /// Gets the response metadata that can be used by middleware.
    /// Middleware can store custom data here for passing information between middleware layers.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; } = [];

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ConvexResponse Success(object? value, Type? valueType = null)
    {
        return new ConvexResponse
        {
            Value = value,
            ValueType = valueType ?? value?.GetType(),
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a successful response with a specific type.
    /// </summary>
    public static ConvexResponse Success<T>(T value)
    {
        return new ConvexResponse
        {
            Value = value,
            ValueType = typeof(T),
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed response with an error.
    /// </summary>
    public static ConvexResponse Failure(Exception error)
    {
        return new ConvexResponse
        {
            IsSuccess = false,
            Error = error ?? throw new ArgumentNullException(nameof(error))
        };
    }

    /// <summary>
    /// Gets the value as the specified type.
    /// Throws an exception if the response is not successful or the value cannot be cast.
    /// </summary>
    public T GetValue<T>()
    {
        if (!IsSuccess)
        {
            throw new InvalidOperationException(
                "Cannot get value from a failed response. Check IsSuccess first or use TryGetValue.",
                Error);
        }

        if (Value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException(
            $"Cannot cast response value of type {Value?.GetType()?.Name ?? "null"} to {typeof(T).Name}");
    }

    /// <summary>
    /// Tries to get the value as the specified type.
    /// Returns false if the response is not successful or the value cannot be cast.
    /// </summary>
    public bool TryGetValue<T>(out T? value)
    {
        if (IsSuccess && Value is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }
}
