using Convex.Client.Shared.ErrorHandling;

namespace Convex.Client.Extensions.ResultWrappers;

/// <summary>
/// Represents the result of a Convex operation that may succeed or fail.
/// Provides a type-safe alternative to exception handling for expected error conditions.
/// </summary>
/// <typeparam name="T">The type of the result value on success.</typeparam>
public record ConvexResult<T>
{
    private readonly T? _value;
    private readonly Exception? _error;

    private ConvexResult(T? value, Exception? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the result value if the operation was successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value
    {
        get
        {
            return !IsSuccess
                ? throw new InvalidOperationException(
                    $"Cannot access Value on a failed result. Error: {_error?.Message}")
                : _value!;
        }
    }

    /// <summary>
    /// Gets the error exception if the operation failed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result.</exception>
    public Exception Error => IsSuccess ? throw new InvalidOperationException("Cannot access Error on a successful result.") : _error!;

    /// <summary>
    /// Gets the error message if the operation failed, or null if successful.
    /// </summary>
    public string? ErrorMessage => _error?.Message;

    /// <summary>
    /// Gets the Convex-specific error code if available.
    /// </summary>
    public string? ErrorCode
    {
        get
        {
            return _error switch
            {
                ConvexException convexEx => convexEx.ErrorCode,
                _ => null
            };
        }
    }

    /// <summary>
    /// Tries to get the result value.
    /// </summary>
    /// <param name="value">The result value if successful, or default if failed.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public bool TryGetValue(out T? value)
    {
        value = IsSuccess ? _value : default;
        return IsSuccess;
    }

    /// <summary>
    /// Tries to get the error exception.
    /// </summary>
    /// <param name="error">The error exception if failed, or null if successful.</param>
    /// <returns>True if failed; otherwise, false.</returns>
    public bool TryGetError(out Exception? error)
    {
        error = IsSuccess ? null : _error;
        return IsFailure;
    }

    /// <summary>
    /// Gets the value if successful, or the specified default value if failed.
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure.</param>
    /// <returns>The result value or default value.</returns>
    public T GetValueOrDefault(T defaultValue) => IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Gets the value if successful, or computes a default value if failed.
    /// </summary>
    /// <param name="defaultValueFactory">Function to compute the default value on failure.</param>
    /// <returns>The result value or computed default value.</returns>
    public T GetValueOrDefault(Func<T> defaultValueFactory) => IsSuccess ? _value! : defaultValueFactory();

    /// <summary>
    /// Executes an action based on the result state.
    /// </summary>
    /// <param name="onSuccess">Action to execute if successful.</param>
    /// <param name="onFailure">Action to execute if failed.</param>
    public void Match(Action<T> onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess(_value!);
        }
        else
        {
            onFailure(_error!);
        }

    }

    /// <summary>
    /// Executes a function based on the result state and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result to return.</typeparam>
    /// <param name="onSuccess">Function to execute if successful.</param>
    /// <param name="onFailure">Function to execute if failed.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure) => IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    /// <summary>
    /// Transforms the result value if successful.
    /// </summary>
    /// <typeparam name="TResult">The type of the transformed value.</typeparam>
    /// <param name="transform">Function to transform the value.</param>
    /// <returns>A new result with the transformed value, or the original error.</returns>
    public ConvexResult<TResult> Map<TResult>(Func<T, TResult> transform)
    {
        return IsSuccess
            ? ConvexResult<TResult>.Success(transform(_value!))
            : ConvexResult<TResult>.Failure(_error!);
    }

    /// <summary>
    /// Chains another result-producing operation if this result is successful.
    /// </summary>
    /// <typeparam name="TResult">The type of the result from the next operation.</typeparam>
    /// <param name="bind">Function to produce the next result.</param>
    /// <returns>The result of the bound operation, or the original error.</returns>
    public ConvexResult<TResult> Bind<TResult>(Func<T, ConvexResult<TResult>> bind) => IsSuccess ? bind(_value!) : ConvexResult<TResult>.Failure(_error!);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">Action to execute on success.</param>
    /// <returns>This result instance for method chaining.</returns>
    public ConvexResult<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(_value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">Action to execute on failure.</param>
    /// <returns>This result instance for method chaining.</returns>
    public ConvexResult<T> OnFailure(Action<Exception> action)
    {
        if (IsFailure)
        {
            action(_error!);
        }

        return this;
    }


    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful result.</returns>
#pragma warning disable CA1000 // Static factory methods on generic types are the standard Result pattern
    public static ConvexResult<T> Success(T value) => new(value, null, true);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error exception.</param>
    /// <returns>A failed result.</returns>
    public static ConvexResult<T> Failure(Exception error) =>
#pragma warning disable CA2201 // Generic Exception is acceptable as fallback for null error parameter
        new(default, error ?? new Exception("Unknown error"), false);
#pragma warning restore CA2201


    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ConvexResult<T> Failure(string errorMessage) => new(default, new ConvexException(errorMessage), false);
#pragma warning restore CA1000

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator ConvexResult<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an exception to a failed result.
    /// </summary>
    public static implicit operator ConvexResult<T>(Exception error) => Failure(error);
}

/// <summary>
/// Represents a result without a value (for operations that don't return data).
/// </summary>
public record ConvexResult
{
    private readonly Exception? _error;

    private ConvexResult(Exception? error, bool isSuccess)
    {
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error exception if the operation failed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result.</exception>
    public Exception Error => IsSuccess ? throw new InvalidOperationException("Cannot access Error on a successful result.") : _error!;

    /// <summary>
    /// Gets the error message if the operation failed, or null if successful.
    /// </summary>
    public string? ErrorMessage => _error?.Message;

    /// <summary>
    /// Tries to get the error exception.
    /// </summary>
    /// <param name="error">The error exception if failed, or null if successful.</param>
    /// <returns>True if failed; otherwise, false.</returns>
    public bool TryGetError(out Exception? error)
    {
        error = IsSuccess ? null : _error;
        return IsFailure;
    }

    /// <summary>
    /// Executes an action based on the result state.
    /// </summary>
    /// <param name="onSuccess">Action to execute if successful.</param>
    /// <param name="onFailure">Action to execute if failed.</param>
    public void Match(Action onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(_error!);
        }

    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">Action to execute on success.</param>
    /// <returns>This result instance for method chaining.</returns>
    public ConvexResult OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }


        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">Action to execute on failure.</param>
    /// <returns>This result instance for method chaining.</returns>
    public ConvexResult OnFailure(Action<Exception> action)
    {
        if (IsFailure)
        {
            action(_error!);
        }


        return this;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ConvexResult Success() => new(null, true);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error exception.</param>
    /// <returns>A failed result.</returns>
    public static ConvexResult Failure(Exception error) =>
#pragma warning disable CA2201 // Generic Exception is acceptable as fallback for null error parameter
        new(error ?? new Exception("Unknown error"), false);
#pragma warning restore CA2201


    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static ConvexResult Failure(string errorMessage) => new(new ConvexException(errorMessage), false);
}
