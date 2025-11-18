using Convex.Client.Shared.Builders;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Slices.Queries;

/// <summary>
/// Queries slice - provides read-only Convex function execution.
/// This is a self-contained vertical slice that handles all query-related functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueriesSlice"/> class.
/// </remarks>
/// <param name="httpProvider">The HTTP client provider for making requests.</param>
/// <param name="serializer">The serializer for handling Convex JSON format.</param>
/// <param name="logger">Optional logger for debug logging.</param>
/// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
public class QueriesSlice(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    ILogger? logger = null,
    bool enableDebugLogging = false)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    /// <summary>
    /// Creates a query builder for the specified Convex function.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="functionName">The name of the Convex function to query.</param>
    /// <returns>A query builder for fluent configuration and execution.</returns>
    public IQueryBuilder<TResult> Query<TResult>(string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or whitespace.", nameof(functionName));

        return new QueryBuilder<TResult>(_httpProvider, _serializer, functionName, _logger, _enableDebugLogging);
    }

    /// <summary>
    /// Creates a batch query builder for executing multiple queries in a single request.
    /// </summary>
    /// <returns>A batch query builder.</returns>
    public IBatchQueryBuilder Batch() => new BatchQueryBuilder(_httpProvider, _serializer, _logger, _enableDebugLogging);
}
