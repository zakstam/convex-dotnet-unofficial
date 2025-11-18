using System.Text;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Slices.Queries;

/// <summary>
/// Builder for executing multiple queries in a single batch request.
/// This implementation uses Shared infrastructure instead of CoreOperations.
/// </summary>
internal sealed class BatchQueryBuilder(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IBatchQueryBuilder
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;
    private readonly List<BatchQueryItem> _queries = [];

    /// <inheritdoc/>
    public IBatchQueryBuilder Query<T>(string functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or whitespace.", nameof(functionName));

        _queries.Add(new BatchQueryItem
        {
            FunctionName = functionName,
            Args = null,
            ResultType = typeof(T)
        });

        return this;
    }

    /// <inheritdoc/>
    public IBatchQueryBuilder Query<T, TArgs>(string functionName, TArgs args) where TArgs : notnull
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or whitespace.", nameof(functionName));

        _queries.Add(new BatchQueryItem
        {
            FunctionName = functionName,
            Args = args,
            ResultType = typeof(T)
        });

        return this;
    }

    /// <inheritdoc/>
    public async Task<(T1, T2)> ExecuteAsync<T1, T2>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 2)
            throw new InvalidOperationException($"Expected 2 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3)> ExecuteAsync<T1, T2, T3>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 3)
            throw new InvalidOperationException($"Expected 3 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3, T4)> ExecuteAsync<T1, T2, T3, T4>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 4)
            throw new InvalidOperationException($"Expected 4 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2]),
            DeserializeResult<T4>(results[3])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3, T4, T5)> ExecuteAsync<T1, T2, T3, T4, T5>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 5)
            throw new InvalidOperationException($"Expected 5 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2]),
            DeserializeResult<T4>(results[3]),
            DeserializeResult<T5>(results[4])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3, T4, T5, T6)> ExecuteAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 6)
            throw new InvalidOperationException($"Expected 6 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2]),
            DeserializeResult<T4>(results[3]),
            DeserializeResult<T5>(results[4]),
            DeserializeResult<T6>(results[5])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3, T4, T5, T6, T7)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 7)
            throw new InvalidOperationException($"Expected 7 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2]),
            DeserializeResult<T4>(results[3]),
            DeserializeResult<T5>(results[4]),
            DeserializeResult<T6>(results[5]),
            DeserializeResult<T7>(results[6])
        );
    }

    /// <inheritdoc/>
    public async Task<(T1, T2, T3, T4, T5, T6, T7, T8)> ExecuteAsync<T1, T2, T3, T4, T5, T6, T7, T8>(CancellationToken cancellationToken = default)
    {
        if (_queries.Count != 8)
            throw new InvalidOperationException($"Expected 8 queries but found {_queries.Count}. Use ExecuteAsync() for dynamic number of queries.");

        var results = await ExecuteBatchAsync(cancellationToken);

        return (
            DeserializeResult<T1>(results[0]),
            DeserializeResult<T2>(results[1]),
            DeserializeResult<T3>(results[2]),
            DeserializeResult<T4>(results[3]),
            DeserializeResult<T5>(results[4]),
            DeserializeResult<T6>(results[5]),
            DeserializeResult<T7>(results[6]),
            DeserializeResult<T8>(results[7])
        );
    }

    /// <inheritdoc/>
    public async Task<object[]> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_queries.Count == 0)
            throw new InvalidOperationException("No queries have been added to the batch.");

        var results = await ExecuteBatchAsync(cancellationToken);

        // Convert string results to typed objects based on stored result types
        var typedResults = new object[results.Count];
        for (var i = 0; i < results.Count; i++)
        {
            var item = _queries[i];
            var result = _serializer.Deserialize(results[i], item.ResultType);
            typedResults[i] = result ?? throw new InvalidOperationException(
                $"Failed to deserialize result for query {i} ({item.FunctionName})");
        }

        return typedResults;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> ExecuteAsDictionaryAsync(CancellationToken cancellationToken = default)
    {
        if (_queries.Count == 0)
            throw new InvalidOperationException("No queries have been added to the batch.");

        var results = await ExecuteBatchAsync(cancellationToken);

        // Convert string results to typed objects and key by function name
        var dictionary = new Dictionary<string, object>(_queries.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var item = _queries[i];
            var result = _serializer.Deserialize(results[i], item.ResultType);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize result for query {i} ({item.FunctionName})");
            }

            // Use function name as key (handle duplicates by appending index if needed)
            var key = item.FunctionName;
            if (dictionary.ContainsKey(key))
            {
                // If duplicate function names exist, append index to make unique
                key = $"{item.FunctionName}[{i}]";
            }

            dictionary[key] = result;
        }

        return dictionary;
    }

    /// <summary>
    /// Executes the batch query request and returns raw JSON results.
    /// </summary>
    private async Task<List<string>> ExecuteBatchAsync(CancellationToken cancellationToken)
    {
        if (_queries.Count == 0)
            throw new InvalidOperationException("No queries have been added to the batch.");

        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var queryNames = string.Join(", ", _queries.Select(q => q.FunctionName));
            _logger!.LogDebug("[BatchQuery] Starting batch query execution: {QueryCount} queries, Functions: [{QueryNames}]",
                _queries.Count, queryNames);
        }

        // Build batch request
        var batchRequest = _queries.Select(q => new
        {
            path = q.FunctionName,
            args = q.Args ?? new { }
        }).ToArray();

        string json;
        try
        {
            json = _serializer.Serialize(batchRequest) ?? throw new InvalidOperationException(
                "Failed to serialize batch query request. Serializer returned null.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to serialize batch query request for {_queries.Count} queries: {ex.Message}", ex);
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[BatchQuery] Batch request body: {RequestJson}", json);
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_httpProvider.DeploymentUrl}/api/batch_query";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[BatchQuery] Batch query request: URL: {Url}, Method: {Method}, QueryCount: {QueryCount}",
                url, request.Method, _queries.Count);
        }

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[BatchQuery] Batch query response received: StatusCode: {StatusCode}, QueryCount: {QueryCount}",
                response.StatusCode, _queries.Count);
        }

        // Handle STATUS_CODE_UDF_FAILED (560) as valid response with error data
        // This matches convex-js behavior where HTTP 560 is treated as a valid response
        ConvexHttpConstants.EnsureConvexResponse(response);

        var responseJson = await response.ReadContentAsStringAsync(cancellationToken);

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[BatchQuery] Batch query response body: {ResponseJson}", responseJson);
        }

        if (string.IsNullOrWhiteSpace(responseJson))
        {
            throw new InvalidOperationException(
                $"Empty response from batch query. Expected {_queries.Count} results.");
        }

        // Parse the response array
        System.Text.Json.JsonDocument doc;
        try
        {
            doc = System.Text.Json.JsonDocument.Parse(responseJson);
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            throw new InvalidOperationException(
                $"Invalid JSON response from batch query: {jsonEx.Message}. Expected array with {_queries.Count} results.", jsonEx);
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    $"Invalid batch query response format: expected array but got {root.ValueKind}. Expected {_queries.Count} results.");
            }

            var resultList = new List<string>();

            // Process each element in the array
            var elementIndex = 0;
            foreach (var element in root.EnumerateArray())
            {
                try
                {
                    // Check if element is wrapped in Convex response format (has status/value)
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Object &&
                        element.TryGetProperty("status", out var status))
                    {
                        var statusValue = status.GetString();
                        if (statusValue == "success" && element.TryGetProperty("value", out var value))
                        {
                            // Extract the value from wrapped format
                            resultList.Add(value.GetRawText());
                        }
                        else if (statusValue == "error")
                        {
                            // Handle error response
                            var errorMessage = element.TryGetProperty("errorMessage", out var errMsg)
                                ? errMsg.GetString()
                                : "Unknown batch query error";
                            var queryIndex = elementIndex;
                            var functionName = queryIndex < _queries.Count ? _queries[queryIndex].FunctionName : "unknown";
                            throw new InvalidOperationException(
                                $"Batch query failed at index {queryIndex} ({functionName}): {errorMessage}");
                        }
                        else
                        {
                            var functionName = elementIndex < _queries.Count ? _queries[elementIndex].FunctionName : "unknown";
                            throw new InvalidOperationException(
                                $"Invalid batch query response format at index {elementIndex} ({functionName}): unknown status '{statusValue}'");
                        }
                    }
                    else
                    {
                        // Raw format - use element as-is
                        resultList.Add(element.GetRawText());
                    }
                }
                catch (InvalidOperationException)
                {
                    // Re-throw InvalidOperationException as-is (these are our validation errors)
                    throw;
                }
                catch (Exception ex)
                {
                    // Wrap unexpected exceptions with context
                    var functionName = elementIndex < _queries.Count ? _queries[elementIndex].FunctionName : "unknown";
                    throw new InvalidOperationException(
                        $"Failed to process batch query result at index {elementIndex} ({functionName}): {ex.Message}", ex);
                }
                elementIndex++;
            }

            if (resultList.Count != _queries.Count)
            {
                var difference = resultList.Count - _queries.Count;
                var message = difference > 0
                    ? $"Batch query returned {resultList.Count} results but expected {_queries.Count} (received {difference} extra result(s))"
                    : $"Batch query returned {resultList.Count} results but expected {_queries.Count} (missing {Math.Abs(difference)} result(s))";
                throw new InvalidOperationException(message);
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[BatchQuery] Batch query execution completed: QueryCount: {QueryCount}, Duration: {DurationMs}ms",
                    _queries.Count, stopwatch.Elapsed.TotalMilliseconds);
            }

            return resultList;
        }
    }

    /// <summary>
    /// Deserializes a result string to the specified type.
    /// </summary>
    private T DeserializeResult<T>(string json)
    {
        var result = _serializer.Deserialize<T>(json);
        return result ?? throw new InvalidOperationException($"Failed to deserialize result to type {typeof(T).Name}");
    }

    /// <summary>
    /// Internal class to store query information.
    /// </summary>
    private class BatchQueryItem
    {
        public string FunctionName { get; set; } = "";
        public object? Args { get; set; }
        public Type ResultType { get; set; } = typeof(object);
    }
}
