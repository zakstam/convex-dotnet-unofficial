namespace Convex.Client.Infrastructure.ErrorHandling;

/// <summary>
/// Represents the result of a Convex operation that can either succeed with a value or fail with an error.
/// This provides a type-safe alternative to exception-based error handling, following functional programming patterns.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ConvexResult{T}"/> is useful when you want to handle errors without exceptions, or when you want
/// to chain multiple operations together functionally. It provides methods for:
/// <list type="bullet">
/// <item>Checking success/failure status</item>
/// <item>Accessing values safely</item>
/// <item>Transforming values with Map</item>
/// <item>Chaining operations with Bind</item>
/// <item>Handling both success and failure cases with Match</item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="Convex.Client.Infrastructure.Builders.IQueryBuilder{TResult}.ExecuteWithResultAsync(CancellationToken)"/> on query/mutation/action builders to get
/// a result instead of throwing exceptions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Execute query with result handling
/// var result = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
///     .ExecuteWithResultAsync();
///
/// // Pattern matching approach
/// result.Match(
///     onSuccess: todos => Console.WriteLine($"Found {todos.Count} todos"),
///     onFailure: error => Console.WriteLine($"Error: {error.Message}")
/// );
///
/// // Functional chaining
/// var todoCount = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
///     .ExecuteWithResultAsync()
///     .Map(todos => todos.Count)
///     .GetValueOrDefault(0);
/// </code>
/// </example>
/// <seealso cref="Convex.Client.Infrastructure.Builders.IQueryBuilder{TResult}.ExecuteWithResultAsync(CancellationToken)"/>
/// <seealso cref="Convex.Client.Infrastructure.Builders.IMutationBuilder{TResult}.ExecuteWithResultAsync(CancellationToken)"/>
/// <seealso cref="Convex.Client.Infrastructure.Builders.IActionBuilder{TResult}.ExecuteWithResultAsync(CancellationToken)"/>
public sealed record ConvexResult<T>
{
    private readonly T? _value;
    private readonly ConvexError? _error;

    private ConvexResult(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private ConvexResult(ConvexError error)
    {
        IsSuccess = false;
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// Throws InvalidOperationException if the result is a failure.
    /// Always check <see cref="IsSuccess"/> before accessing, or use <see cref="Match{TResult}(Func{T, TResult}, Func{ConvexError, TResult})"/> or <see cref="GetValueOrDefault(T)"/> for safe access.
    /// </summary>
    /// <value>The success value if the operation succeeded.</value>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result. Check <see cref="IsSuccess"/> first or use <see cref="Match{TResult}(Func{T, TResult}, Func{ConvexError, TResult})"/> or <see cref="GetValueOrDefault(T)"/> instead.</exception>
    /// <example>
    /// <code>
    /// var result = await client.Query&lt;User&gt;("functions/getUser").ExecuteWithResultAsync();
    ///
    /// // Safe access - check first
    /// if (result.IsSuccess)
    /// {
    ///     var user = result.Value; // Safe to access
    ///     Console.WriteLine(user.Name);
    /// }
    ///
    /// // Or use GetValueOrDefault for default value
    /// var user = result.GetValueOrDefault(new User { Name = "Unknown" });
    /// </code>
    /// </example>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result. Check IsSuccess first or use Match() instead.");

    /// <summary>
    /// Gets the error.
    /// Throws InvalidOperationException if the result is a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on a successful result.</exception>
    public ConvexError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result. Check IsFailure first or use Match() instead.");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static ConvexResult<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static ConvexResult<T> Failure(ConvexError error) => new(error);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static ConvexResult<T> Failure(Exception exception) => new(ConvexError.FromException(exception));

    /// <summary>
    /// Matches the result and executes the corresponding function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="onSuccess">Function to execute if the result is successful.</param>
    /// <param name="onFailure">Function to execute if the result is a failure.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<ConvexError, TResult> onFailure)
    {
        return onSuccess == null
            ? throw new ArgumentNullException(nameof(onSuccess))
            : onFailure == null ? throw new ArgumentNullException(nameof(onFailure)) : IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    /// <summary>
    /// Matches the result and executes the corresponding action.
    /// </summary>
    /// <param name="onSuccess">Action to execute if the result is successful.</param>
    /// <param name="onFailure">Action to execute if the result is a failure.</param>
    public void Match(
        Action<T> onSuccess,
        Action<ConvexError> onFailure)
    {
        if (onSuccess == null)
        {
            throw new ArgumentNullException(nameof(onSuccess));
        }

        if (onFailure == null)
        {
            throw new ArgumentNullException(nameof(onFailure));
        }

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
    /// Executes an action if the result is successful.
    /// </summary>
    public ConvexResult<T> OnSuccess(Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsSuccess)
        {
            action(_value!);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public ConvexResult<T> OnFailure(Action<ConvexError> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsFailure)
        {
            action(_error!);
        }

        return this;
    }

    /// <summary>
    /// Maps the success value to a new value using the specified function.
    /// If the result is a failure, returns the failure unchanged.
    /// This allows you to transform successful results without unwrapping them.
    /// </summary>
    /// <typeparam name="TNew">The type of the new value.</typeparam>
    /// <param name="mapper">The function to transform the success value. Must not be null.</param>
    /// <returns>A new result with the transformed value if successful, or the original failure if failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapper"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Transform the result without unwrapping
    /// var countResult = await client.Query&lt;List&lt;Todo&gt;&gt;("functions/listTodos")
    ///     .ExecuteWithResultAsync()
    ///     .Map(todos => todos.Count);
    ///
    /// // Chain multiple transformations
    /// var formattedResult = await client.Query&lt;User&gt;("functions/getUser")
    ///     .ExecuteWithResultAsync()
    ///     .Map(user => user.Name)
    ///     .Map(name => name.ToUpper());
    /// </code>
    /// </example>
    /// <seealso cref="Bind{TNew}(Func{T, ConvexResult{TNew}})"/>
    public ConvexResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return mapper == null
            ? throw new ArgumentNullException(nameof(mapper))
            : IsSuccess
            ? ConvexResult<TNew>.Success(mapper(_value!))
            : ConvexResult<TNew>.Failure(_error!);
    }

    /// <summary>
    /// Binds the success value to a new result using the specified function.
    /// If the result is a failure, returns the failure unchanged.
    /// </summary>
    public ConvexResult<TNew> Bind<TNew>(Func<T, ConvexResult<TNew>> binder)
    {
        return binder == null
            ? throw new ArgumentNullException(nameof(binder))
            : IsSuccess
            ? binder(_value!)
            : ConvexResult<TNew>.Failure(_error!);
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the specified default value.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!) =>
        IsSuccess ? _value! : defaultValue;

    /// <summary>
    /// Gets the value if successful, otherwise returns the result of the specified function.
    /// </summary>
    public T GetValueOrDefault(Func<ConvexError, T> defaultValueFactory)
    {
        return defaultValueFactory == null
            ? throw new ArgumentNullException(nameof(defaultValueFactory))
            : IsSuccess ? _value! : defaultValueFactory(_error!);
    }

    /// <summary>
    /// Converts the result to a string representation.
    /// </summary>
    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
