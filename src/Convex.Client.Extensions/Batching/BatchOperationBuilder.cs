
using Convex.Client.Extensions.ResultWrappers;

namespace Convex.Client.Extensions.Batching;

/// <summary>
/// Builder for executing multiple Convex operations in a coordinated batch.
/// Provides methods to add queries, mutations, and actions, then execute them together.
/// </summary>
/// <remarks>
/// Initializes a new instance of the BatchOperationBuilder class.
/// </remarks>
/// <param name="client">The Convex client instance.</param>
public class BatchOperationBuilder(IConvexClient client)
{
    private readonly IConvexClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly List<BatchOperation> _operations = [];

    /// <summary>
    /// Adds a query operation to the batch.
    /// </summary>
    /// <typeparam name="TResult">The type of the query result.</typeparam>
    /// <param name="functionName">The name of the query function.</param>
    /// <param name="args">Optional query arguments.</param>
    /// <param name="key">Optional key to identify this operation in results (defaults to operation index).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public BatchOperationBuilder AddQuery<TResult>(
        string functionName,
        object? args = null,
        string? key = null)
    {
        _operations.Add(new BatchOperation
        {
            Type = OperationType.Query,
            FunctionName = functionName,
            Args = args,
            ResultType = typeof(TResult),
            Key = key ?? _operations.Count.ToString()
        });
        return this;
    }

    /// <summary>
    /// Adds a mutation operation to the batch.
    /// </summary>
    /// <typeparam name="TResult">The type of the mutation result.</typeparam>
    /// <param name="functionName">The name of the mutation function.</param>
    /// <param name="args">Optional mutation arguments.</param>
    /// <param name="key">Optional key to identify this operation in results (defaults to operation index).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public BatchOperationBuilder AddMutation<TResult>(
        string functionName,
        object? args = null,
        string? key = null)
    {
        _operations.Add(new BatchOperation
        {
            Type = OperationType.Mutation,
            FunctionName = functionName,
            Args = args,
            ResultType = typeof(TResult),
            Key = key ?? _operations.Count.ToString()
        });
        return this;
    }

    /// <summary>
    /// Adds an action operation to the batch.
    /// </summary>
    /// <typeparam name="TResult">The type of the action result.</typeparam>
    /// <param name="functionName">The name of the action function.</param>
    /// <param name="args">Optional action arguments.</param>
    /// <param name="key">Optional key to identify this operation in results (defaults to operation index).</param>
    /// <returns>This builder instance for method chaining.</returns>
    public BatchOperationBuilder AddAction<TResult>(
        string functionName,
        object? args = null,
        string? key = null)
    {
        _operations.Add(new BatchOperation
        {
            Type = OperationType.Action,
            FunctionName = functionName,
            Args = args,
            ResultType = typeof(TResult),
            Key = key ?? _operations.Count.ToString()
        });
        return this;
    }

    /// <summary>
    /// Removes an operation from the batch by its key.
    /// </summary>
    /// <param name="key">The key of the operation to remove.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public BatchOperationBuilder Remove(string key)
    {
        _ = _operations.RemoveAll(op => op.Key == key);
        return this;
    }

    /// <summary>
    /// Clears all operations from the batch.
    /// </summary>
    /// <returns>This builder instance for method chaining.</returns>
    public BatchOperationBuilder Clear()
    {
        _operations.Clear();
        return this;
    }

    /// <summary>
    /// Gets the number of operations in the batch.
    /// </summary>
    public int Count => _operations.Count;

    /// <summary>
    /// Executes all operations in parallel and returns the results.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A dictionary containing the results keyed by operation key.</returns>
    public async Task<BatchResults> ExecuteParallelAsync(CancellationToken cancellationToken = default)
    {
        var results = new BatchResults();
        var tasks = new List<Task>();

        foreach (var operation in _operations)
        {
            var task = ExecuteOperationAsync(operation, results, cancellationToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Executes all operations sequentially in the order they were added.
    /// Stops execution if any operation fails and continueOnError is false.
    /// </summary>
    /// <param name="continueOnError">Whether to continue executing operations if one fails (default: false).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A dictionary containing the results keyed by operation key.</returns>
    public async Task<BatchResults> ExecuteSequentialAsync(
        bool continueOnError = false,
        CancellationToken cancellationToken = default)
    {
        var results = new BatchResults();

        foreach (var operation in _operations)
        {
            try
            {
                await ExecuteOperationAsync(operation, results, cancellationToken);
            }
            catch (Exception ex)
            {
                results.AddError(operation.Key, ex);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Executes all operations with a specified maximum degree of parallelism.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Maximum number of operations to execute concurrently.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A dictionary containing the results keyed by operation key.</returns>
    public async Task<BatchResults> ExecuteWithDegreeAsync(
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        var results = new BatchResults();
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = new List<Task>();

        foreach (var operation in _operations)
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ExecuteOperationAsync(operation, results, cancellationToken);
                }
                finally
                {
                    _ = semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Executes a single batch operation.
    /// </summary>
    private async Task ExecuteOperationAsync(
        BatchOperation operation,
        BatchResults results,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = operation.Type switch
            {
                OperationType.Query => await ExecuteQueryAsync(operation, cancellationToken),
                OperationType.Mutation => await ExecuteMutationAsync(operation, cancellationToken),
                OperationType.Action => await ExecuteActionAsync(operation, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown operation type: {operation.Type}")
            };

            results.AddSuccess(operation.Key, result);
        }
        catch (Exception ex)
        {
            results.AddError(operation.Key, ex);
        }
    }

    private async Task<object?> ExecuteQueryAsync(BatchOperation operation, CancellationToken cancellationToken)
    {
        // Use fluent API: Query<TResult>(functionName).WithArgs(args).ExecuteAsync()
        var queryMethod = typeof(IConvexClient).GetMethod(nameof(IConvexClient.Query))!
            .MakeGenericMethod(operation.ResultType);
        var queryBuilder = queryMethod.Invoke(_client, [operation.FunctionName])!;

        // Get WithArgs method if args are provided
        if (operation.Args != null)
        {
            var withArgsMethod = queryBuilder.GetType().GetMethods()
                .First(m => m.Name == "WithArgs" && m.GetParameters().Length == 1);
            var withArgsGeneric = withArgsMethod.MakeGenericMethod(operation.Args.GetType());
            queryBuilder = withArgsGeneric.Invoke(queryBuilder, [operation.Args])!;
        }

        // Get ExecuteAsync method
        var executeMethod = queryBuilder.GetType().GetMethod("ExecuteAsync", [typeof(CancellationToken)])!;
        var task = (Task)executeMethod.Invoke(queryBuilder, [cancellationToken])!;
        await task;

        var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
        return resultProperty?.GetValue(task);
    }

    private async Task<object?> ExecuteMutationAsync(BatchOperation operation, CancellationToken cancellationToken)
    {
        // Use fluent API: Mutate<TResult>(functionName).WithArgs(args).ExecuteAsync()
        var mutateMethod = typeof(IConvexClient).GetMethod(nameof(IConvexClient.Mutate))!
            .MakeGenericMethod(operation.ResultType);
        var mutationBuilder = mutateMethod.Invoke(_client, [operation.FunctionName])!;

        // Get WithArgs method if args are provided
        if (operation.Args != null)
        {
            var withArgsMethod = mutationBuilder.GetType().GetMethods()
                .First(m => m.Name == "WithArgs" && m.GetParameters().Length == 1);
            var withArgsGeneric = withArgsMethod.MakeGenericMethod(operation.Args.GetType());
            mutationBuilder = withArgsGeneric.Invoke(mutationBuilder, [operation.Args])!;
        }

        // Get ExecuteAsync method
        var executeMethod = mutationBuilder.GetType().GetMethod("ExecuteAsync", [typeof(CancellationToken)])!;
        var task = (Task)executeMethod.Invoke(mutationBuilder, [cancellationToken])!;
        await task;

        var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
        return resultProperty?.GetValue(task);
    }

    private async Task<object?> ExecuteActionAsync(BatchOperation operation, CancellationToken cancellationToken)
    {
        // Use fluent API: Action<TResult>(functionName).WithArgs(args).ExecuteAsync()
        var actionMethod = typeof(IConvexClient).GetMethod(nameof(IConvexClient.Action))!
            .MakeGenericMethod(operation.ResultType);
        var actionBuilder = actionMethod.Invoke(_client, [operation.FunctionName])!;

        // Get WithArgs method if args are provided
        if (operation.Args != null)
        {
            var withArgsMethod = actionBuilder.GetType().GetMethods()
                .First(m => m.Name == "WithArgs" && m.GetParameters().Length == 1);
            var withArgsGeneric = withArgsMethod.MakeGenericMethod(operation.Args.GetType());
            actionBuilder = withArgsGeneric.Invoke(actionBuilder, [operation.Args])!;
        }

        // Get ExecuteAsync method
        var executeMethod = actionBuilder.GetType().GetMethod("ExecuteAsync", [typeof(CancellationToken)])!;
        var task = (Task)executeMethod.Invoke(actionBuilder, [cancellationToken])!;
        await task;

        var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result));
        return resultProperty?.GetValue(task);
    }
}

/// <summary>
/// Represents a single operation in a batch.
/// </summary>
internal class BatchOperation
{
    public OperationType Type { get; set; }
    public string FunctionName { get; set; } = "";
    public object? Args { get; set; }
    public Type ResultType { get; set; } = typeof(object);
    public string Key { get; set; } = "";
}

/// <summary>
/// Type of Convex operation.
/// </summary>
internal enum OperationType
{
    Query,
    Mutation,
    Action
}

/// <summary>
/// Contains the results of a batch operation execution.
/// </summary>
public class BatchResults
{
    private readonly Dictionary<string, ConvexResult<object?>> _results = [];

    /// <summary>
    /// Gets all result keys.
    /// </summary>
    public IEnumerable<string> Keys => _results.Keys;

    /// <summary>
    /// Gets the number of successful operations.
    /// </summary>
    public int SuccessCount => _results.Values.Count(r => r.IsSuccess);

    /// <summary>
    /// Gets the number of failed operations.
    /// </summary>
    public int FailureCount => _results.Values.Count(r => r.IsFailure);

    /// <summary>
    /// Gets the total number of operations.
    /// </summary>
    public int TotalCount => _results.Count;

    /// <summary>
    /// Checks if all operations succeeded.
    /// </summary>
    public bool AllSucceeded => _results.All(r => r.Value.IsSuccess);

    /// <summary>
    /// Checks if any operation failed.
    /// </summary>
    public bool AnyFailed => _results.Any(r => r.Value.IsFailure);

    /// <summary>
    /// Gets the result for a specific operation key.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="key">The operation key.</param>
    /// <returns>The typed result.</returns>
    public ConvexResult<T> GetResult<T>(string key)
    {
        if (!_results.TryGetValue(key, out var result))
        {
            return ConvexResult<T>.Failure(new KeyNotFoundException($"No result found for key: {key}"));
        }

        if (result.IsFailure)
        {
            return ConvexResult<T>.Failure(result.Error);
        }

        try
        {
            var typedValue = (T)result.Value!;
            return ConvexResult<T>.Success(typedValue);
        }
        catch (Exception ex)
        {
            return ConvexResult<T>.Failure(new InvalidCastException(
                $"Cannot cast result for key '{key}' to type {typeof(T).Name}", ex));
        }
    }

    /// <summary>
    /// Gets the value for a specific operation key, or default if failed.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="key">The operation key.</param>
    /// <returns>The typed value or default.</returns>
    public T? GetValueOrDefault<T>(string key)
    {
        var result = GetResult<T>(key);
        return result.IsSuccess ? result.Value : default;
    }

    /// <summary>
    /// Gets all errors from failed operations.
    /// </summary>
    /// <returns>Dictionary of keys to exceptions.</returns>
    public Dictionary<string, Exception> GetErrors() =>
        _results
            .Where(kvp => kvp.Value.IsFailure)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Error);

    internal void AddSuccess(string key, object? value) => _results[key] = ConvexResult<object?>.Success(value);

    internal void AddError(string key, Exception error) => _results[key] = ConvexResult<object?>.Failure(error);
}
