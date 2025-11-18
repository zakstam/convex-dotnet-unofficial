namespace Convex.Client.Shared.ErrorHandling;

/// <summary>
/// Extension methods for ConvexResult to provide common functional programming patterns.
/// </summary>
public static class ConvexResultExtensions
{
    /// <summary>
    /// Converts a Result to an optional value, returning null if failed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The value if successful, null otherwise.</returns>
    public static T? ToNullable<T>(this ConvexResult<T> result) where T : class
    {
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// Converts a Result to an optional value, returning null if failed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The value if successful, null otherwise.</returns>
    public static T? ToNullableValueType<T>(this ConvexResult<T> result) where T : struct
    {
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// Throws the exception if the result is a failure, otherwise returns the value.
    /// Useful for converting Result types back to exception-based error handling.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to unwrap.</param>
    /// <returns>The value if successful.</returns>
    /// <exception cref="Exception">Thrown if the result is a failure.</exception>
    public static T Unwrap<T>(this ConvexResult<T> result)
    {
        if (result.IsFailure)
        {
            throw result.Error.Exception;
        }

        return result.Value;
    }

    /// <summary>
    /// Throws the exception if the result is a failure, otherwise returns the value.
    /// Uses the provided exception factory if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to unwrap.</param>
    /// <param name="exceptionFactory">Factory function to create exception from error.</param>
    /// <returns>The value if successful.</returns>
    /// <exception cref="Exception">Thrown if the result is a failure.</exception>
    public static T Unwrap<T>(this ConvexResult<T> result, Func<ConvexError, Exception> exceptionFactory)
    {
        if (result.IsFailure)
        {
            throw exceptionFactory(result.Error);
        }

        return result.Value;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the result of the provided function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="fallback">Function to provide fallback value on failure.</param>
    /// <returns>The value if successful, otherwise the fallback value.</returns>
    public static T OrElse<T>(this ConvexResult<T> result, Func<ConvexError, T> fallback)
    {
        return result.IsSuccess ? result.Value : fallback(result.Error);
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the provided fallback value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="fallback">Fallback value to use on failure.</param>
    /// <returns>The value if successful, otherwise the fallback value.</returns>
    public static T OrElse<T>(this ConvexResult<T> result, T fallback)
    {
        return result.IsSuccess ? result.Value : fallback;
    }

    /// <summary>
    /// Filters the result based on a predicate. If the predicate fails, returns a failure result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to filter.</param>
    /// <param name="predicate">The predicate to test the value.</param>
    /// <param name="errorMessage">Error message if predicate fails.</param>
    /// <returns>A new result that is successful only if the predicate passes.</returns>
    public static ConvexResult<T> Filter<T>(
        this ConvexResult<T> result,
        Func<T, bool> predicate,
        string errorMessage = "Value did not pass filter")
    {
        if (result.IsFailure)
        {
            return result;
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return predicate(result.Value)
            ? result
            : ConvexResult<T>.Failure(ConvexError.FromException(new InvalidOperationException(errorMessage)));
    }

    /// <summary>
    /// Flattens a nested Result into a single Result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The nested result.</param>
    /// <returns>A flattened result.</returns>
    public static ConvexResult<T> Flatten<T>(this ConvexResult<ConvexResult<T>> result)
    {
        return result.IsSuccess ? result.Value : ConvexResult<T>.Failure(result.Error);
    }

    /// <summary>
    /// Converts a Result to a Task that completes with the value or throws.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A task that completes with the value or throws an exception.</returns>
    public static Task<T> AsTask<T>(this ConvexResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Task.FromResult(result.Value);
        }

        return Task.FromException<T>(result.Error.Exception);
    }
}

