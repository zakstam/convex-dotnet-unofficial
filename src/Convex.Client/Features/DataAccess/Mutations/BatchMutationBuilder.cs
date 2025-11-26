using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;

namespace Convex.Client.Features.DataAccess.Mutations;

/// <summary>
/// Builder for executing multiple mutations in parallel.
/// Note: This executes mutations in parallel (using Task.WhenAll), not as a single batch request.
/// Each mutation uses a separate HTTP request to `/api/mutation`.
/// This is different from convex-js which does not have a batch mutation API.
/// This implementation is primarily for reducing network round-trips and improving performance
/// when multiple independent mutations need to be executed.
/// </summary>
internal sealed class BatchMutationBuilder(IHttpClientProvider httpProvider, IConvexSerializer serializer)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly List<(string FunctionName, object? Args, Type ResultType)> _mutations = [];

    /// <summary>
    /// Adds a mutation to the batch without arguments.
    /// </summary>
    public BatchMutationBuilder Mutate<T>(string functionName)
    {
        _mutations.Add((functionName, null, typeof(T)));
        return this;
    }

    /// <summary>
    /// Adds a mutation to the batch with arguments.
    /// </summary>
    public BatchMutationBuilder Mutate<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        _mutations.Add((functionName, args, typeof(T)));
        return this;
    }

    /// <summary>
    /// Executes a batch of 2 mutations in parallel.
    /// </summary>
    public async Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default)
    {
        if (_mutations.Count != 2)
        {
            throw new InvalidOperationException($"Expected 2 mutations but got {_mutations.Count}");
        }

        var task1 = ExecuteMutationAsync<T1>(_mutations[0], cancellationToken);
        var task2 = ExecuteMutationAsync<T2>(_mutations[1], cancellationToken);

        // Await both tasks in parallel and directly return their results
        var result1 = await task1;
        var result2 = await task2;
        return (result1, result2);
    }

    /// <summary>
    /// Executes a batch of 3 mutations in parallel.
    /// </summary>
    public async Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default)
    {
        if (_mutations.Count != 3)
        {
            throw new InvalidOperationException($"Expected 3 mutations but got {_mutations.Count}");
        }

        var task1 = ExecuteMutationAsync<T1>(_mutations[0], cancellationToken);
        var task2 = ExecuteMutationAsync<T2>(_mutations[1], cancellationToken);
        var task3 = ExecuteMutationAsync<T3>(_mutations[2], cancellationToken);

        // Await all tasks in parallel and directly return their results
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;
        return (result1, result2, result3);
    }

    /// <summary>
    /// Executes a batch of 4 mutations in parallel.
    /// </summary>
    public async Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default)
    {
        if (_mutations.Count != 4)
        {
            throw new InvalidOperationException($"Expected 4 mutations but got {_mutations.Count}");
        }

        var task1 = ExecuteMutationAsync<T1>(_mutations[0], cancellationToken);
        var task2 = ExecuteMutationAsync<T2>(_mutations[1], cancellationToken);
        var task3 = ExecuteMutationAsync<T3>(_mutations[2], cancellationToken);
        var task4 = ExecuteMutationAsync<T4>(_mutations[3], cancellationToken);

        // Await all tasks in parallel and directly return their results
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;
        var result4 = await task4;
        return (result1, result2, result3, result4);
    }

    /// <summary>
    /// Executes a batch of 5 mutations in parallel.
    /// </summary>
    public async Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default)
    {
        if (_mutations.Count != 5)
        {
            throw new InvalidOperationException($"Expected 5 mutations but got {_mutations.Count}");
        }

        var task1 = ExecuteMutationAsync<T1>(_mutations[0], cancellationToken);
        var task2 = ExecuteMutationAsync<T2>(_mutations[1], cancellationToken);
        var task3 = ExecuteMutationAsync<T3>(_mutations[2], cancellationToken);
        var task4 = ExecuteMutationAsync<T4>(_mutations[3], cancellationToken);
        var task5 = ExecuteMutationAsync<T5>(_mutations[4], cancellationToken);

        // Await all tasks in parallel and directly return their results
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;
        var result4 = await task4;
        var result5 = await task5;
        return (result1, result2, result3, result4, result5);
    }

    /// <summary>
    /// Executes all mutations in parallel and returns an array of results.
    /// </summary>
    public async Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _mutations.Select(m => ExecuteMutationAsObjectAsync(m, cancellationToken)).ToArray();
        return await Task.WhenAll(tasks);
    }

    private async Task<T> ExecuteMutationAsync<T>((string FunctionName, object? Args, Type ResultType) mutation, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildMutationRequest(
            _httpProvider.DeploymentUrl,
            mutation.FunctionName,
            mutation.Args,
            _serializer);

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<T>(
            response,
            mutation.FunctionName,
            "mutation",
            _serializer,
            cancellationToken);
    }

    private async Task<object> ExecuteMutationAsObjectAsync((string FunctionName, object? Args, Type ResultType) mutation, CancellationToken cancellationToken)
    {
        // Use reflection to call ExecuteMutationAsync<T> with the correct result type
        var method = typeof(BatchMutationBuilder)
            .GetMethod(nameof(ExecuteMutationAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(mutation.ResultType);

        var task = (Task)method.Invoke(this, new object[] { mutation, cancellationToken })!;
        await task;

        // Get result from completed task
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task) ?? throw new InvalidOperationException("Failed to get mutation result");
    }
}
