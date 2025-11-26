namespace Convex.Client.Infrastructure.ErrorHandling;

/// <summary>
/// Extension methods for executing operations with result wrapping.
/// </summary>
public static class ExecutionExtensions
{
    /// <summary>
    /// Executes an async operation and wraps the result in a ConvexResult, catching any exceptions.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="execute">The async operation to execute.</param>
    /// <returns>A ConvexResult containing either the success value or error information.</returns>
    public static async Task<ConvexResult<TResult>> ExecuteWithResultAsync<TResult>(
        Func<Task<TResult>> execute)
    {
        try
        {
            var result = await execute();
            return ConvexResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return ConvexResult<TResult>.Failure(ex);
        }
    }

    /// <summary>
    /// Executes an async operation and wraps the result in a ConvexResult, catching any exceptions.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
    /// <param name="execute">The async operation to execute.</param>
    /// <returns>A ConvexResult containing either the success value or error information.</returns>
    public static async Task<ConvexResult<TResult>> ExecuteWithResultAsync<TResult>(
        Task<TResult> execute)
    {
        try
        {
            var result = await execute;
            return ConvexResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            return ConvexResult<TResult>.Failure(ex);
        }
    }
}

