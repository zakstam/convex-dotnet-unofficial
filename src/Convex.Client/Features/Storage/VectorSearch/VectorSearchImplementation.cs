using System.Text.Json;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Convex.Client.Features.Storage.VectorSearch;

/// <summary>
/// Internal implementation of Convex vector search operations using Shared infrastructure.
/// </summary>
internal sealed class VectorSearchImplementation(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    public async Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting vector search: IndexName: {IndexName}, VectorDimension: {VectorDimension}, Limit: {Limit}",
                indexName, vector?.Length ?? 0, limit);
        }

        try
        {
            ValidateSearchParameters(indexName, vector, limit);

            var args = new { indexName, vector, limit };
            var argsJson = _serializer.Serialize(args);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Vector search request: IndexName: {IndexName}, Args: {Args}",
                    indexName, argsJson);
            }

            var response = await ExecuteQueryAsync<JsonElement>("vectorSearch:search", args, cancellationToken);

            if (!response.TryGetProperty("results", out var resultsElement))
            {
                stopwatch.Stop();
                var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                    "Invalid response from vector search: missing results", indexName);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[VectorSearch] Vector search failed: Invalid response, IndexName: {IndexName}, Response: {Response}, Duration: {DurationMs}ms",
                        indexName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var results = new List<VectorSearchResult<T>>();

            foreach (var resultElement in resultsElement.EnumerateArray())
            {
                if (!resultElement.TryGetProperty("_id", out var idElement) ||
                    !resultElement.TryGetProperty("_score", out var scoreElement))
                {
                    continue;
                }

                var id = idElement.GetString();
                var score = scoreElement.GetSingle();

                if (string.IsNullOrEmpty(id))
                    continue;

                var dataJson = JsonSerializer.Serialize(resultElement);
                var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);
                var data = ExtractDocumentData<T>(dataElement);

                results.Add(new VectorSearchResult<T>
                {
                    Id = id,
                    Score = score,
                    Data = data,
                    Vector = ExtractVector(resultElement),
                    Metadata = ExtractMetadata(resultElement)
                });
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Vector search completed successfully: IndexName: {IndexName}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                    indexName, results.Count, stopwatch.Elapsed.TotalMilliseconds);
            }

            return results;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Vector search operation failed", indexName, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Vector search failed: IndexName: {IndexName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    indexName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var filterJson = _serializer.Serialize(filter);
            _logger!.LogDebug("[VectorSearch] Starting filtered vector search: IndexName: {IndexName}, VectorDimension: {VectorDimension}, Limit: {Limit}, Filter: {Filter}",
                indexName, vector?.Length ?? 0, limit, filterJson);
        }

        try
        {
            ValidateSearchParameters(indexName, vector, limit);

            var args = new { indexName, vector, limit, filter };
            var response = await ExecuteQueryAsync<JsonElement>("vectorSearch:search", args, cancellationToken);

            if (!response.TryGetProperty("results", out var resultsElement))
            {
                stopwatch.Stop();
                var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                    "Invalid response from vector search: missing results", indexName);
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[VectorSearch] Filtered vector search failed: Invalid response, IndexName: {IndexName}, Response: {Response}, Duration: {DurationMs}ms",
                        indexName, response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var results = new List<VectorSearchResult<TResult>>();

            foreach (var resultElement in resultsElement.EnumerateArray())
            {
                if (!resultElement.TryGetProperty("_id", out var idElement) ||
                    !resultElement.TryGetProperty("_score", out var scoreElement))
                {
                    continue;
                }

                var id = idElement.GetString();
                var score = scoreElement.GetSingle();

                if (string.IsNullOrEmpty(id))
                    continue;

                var dataJson = JsonSerializer.Serialize(resultElement);
                var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);
                var data = ExtractDocumentData<TResult>(dataElement);

                results.Add(new VectorSearchResult<TResult>
                {
                    Id = id,
                    Score = score,
                    Data = data,
                    Vector = ExtractVector(resultElement),
                    Metadata = ExtractMetadata(resultElement)
                });
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Filtered vector search completed successfully: IndexName: {IndexName}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                    indexName, results.Count, stopwatch.Elapsed.TotalMilliseconds);
            }

            return results;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Vector search operation failed", indexName, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Filtered vector search failed: IndexName: {IndexName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    indexName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting text-based vector search: IndexName: {IndexName}, TextLength: {TextLength}, Model: {Model}, Limit: {Limit}",
                indexName, text?.Length ?? 0, embeddingModel, limit);
        }

        try
        {
            ValidateTextSearchParameters(indexName, text, limit);

            // First, create embedding for the text
            // text is guaranteed to be non-null after validation
            var vector = await CreateEmbeddingAsync(text!, embeddingModel, cancellationToken);

            // Then perform the vector search
            var results = await SearchAsync<T>(indexName, vector, limit, cancellationToken);

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Text-based vector search completed successfully: IndexName: {IndexName}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                    indexName, results.Count(), stopwatch.Elapsed.TotalMilliseconds);
            }

            return results;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Text-based vector search operation failed", indexName, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Text-based vector search failed: IndexName: {IndexName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    indexName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var filterJson = _serializer.Serialize(filter);
            _logger!.LogDebug("[VectorSearch] Starting filtered text-based vector search: IndexName: {IndexName}, TextLength: {TextLength}, Model: {Model}, Limit: {Limit}, Filter: {Filter}",
                indexName, text?.Length ?? 0, embeddingModel, limit, filterJson);
        }

        try
        {
            ValidateTextSearchParameters(indexName, text, limit);

            // First, create embedding for the text
            // text is guaranteed to be non-null after validation
            var vector = await CreateEmbeddingAsync(text!, embeddingModel, cancellationToken);

            // Then perform the vector search with filter
            var results = await SearchAsync<TResult, TFilter>(indexName, vector, limit, filter, cancellationToken);

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Filtered text-based vector search completed successfully: IndexName: {IndexName}, ResultCount: {ResultCount}, Duration: {DurationMs}ms",
                    indexName, results.Count(), stopwatch.Elapsed.TotalMilliseconds);
            }

            return results;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Text-based vector search operation failed", indexName, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Filtered text-based vector search failed: IndexName: {IndexName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    indexName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting embedding creation: TextLength: {TextLength}, Model: {Model}",
                text?.Length ?? 0, model);
        }

        try
        {
            ValidateEmbeddingParameters(text, model);

            var args = new { text, model };
            var response = await ExecuteActionAsync<JsonElement>("ai:createEmbedding", args, cancellationToken);

            if (!response.TryGetProperty("embedding", out var embeddingElement))
            {
                stopwatch.Stop();
                var error = new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                    "Invalid response from embedding service: missing embedding data");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[VectorSearch] Embedding creation failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var embedding = embeddingElement.EnumerateArray()
                .Select(element => element.GetSingle())
                .ToArray();

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Embedding creation completed successfully: Model: {Model}, Dimension: {Dimension}, Duration: {DurationMs}ms",
                    model, embedding.Length, stopwatch.Elapsed.TotalMilliseconds);
            }

            return embedding;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "Embedding creation failed", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Embedding creation failed: Model: {Model}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    model, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting batch embedding creation: TextCount: {TextCount}, Model: {Model}",
                texts?.Length ?? 0, model);
        }

        try
        {
            ValidateBatchEmbeddingParameters(texts, model);

            var args = new { texts, model };
            var response = await ExecuteActionAsync<JsonElement>("ai:createEmbeddings", args, cancellationToken);

            if (!response.TryGetProperty("embeddings", out var embeddingsElement))
            {
                stopwatch.Stop();
                var error = new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                    "Invalid response from embedding service: missing embeddings data");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[VectorSearch] Batch embedding creation failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var embeddings = embeddingsElement.EnumerateArray()
                .Select(embeddingElement => embeddingElement.EnumerateArray()
                    .Select(element => element.GetSingle())
                    .ToArray())
                .ToArray();

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Batch embedding creation completed successfully: Model: {Model}, Count: {Count}, Dimension: {Dimension}, Duration: {DurationMs}ms",
                    model, embeddings.Length, embeddings.Length > 0 ? embeddings[0].Length : 0, stopwatch.Elapsed.TotalMilliseconds);
            }

            return embeddings;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "Batch embedding creation failed", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Batch embedding creation failed: Model: {Model}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    model, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting index info retrieval: IndexName: {IndexName}", indexName);
        }

        try
        {
            ValidateIndexName(indexName);

            var args = new { indexName };
            var response = await ExecuteQueryAsync<JsonElement>("vectorSearch:getIndexInfo", args, cancellationToken);

            var indexInfo = new VectorIndexInfo
            {
                Name = response.GetProperty("name").GetString()!,
                Dimension = response.GetProperty("dimension").GetInt32(),
                Metric = ParseDistanceMetric(response.GetProperty("metric").GetString()!),
                VectorCount = response.TryGetProperty("vectorCount", out var countElement) ? countElement.GetInt64() : 0,
                Table = response.GetProperty("table").GetString()!,
                VectorField = response.GetProperty("vectorField").GetString()!,
                FilterField = response.TryGetProperty("filterField", out var filterElement) ? filterElement.GetString() : null,
                CreatedAt = response.TryGetProperty("createdAt", out var createdElement)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(createdElement.GetInt64())
                    : DateTimeOffset.MinValue,
                UpdatedAt = response.TryGetProperty("updatedAt", out var updatedElement)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(updatedElement.GetInt64())
                    : DateTimeOffset.MinValue
            };

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] Index info retrieved successfully: IndexName: {IndexName}, Dimension: {Dimension}, VectorCount: {VectorCount}, Duration: {DurationMs}ms",
                    indexInfo.Name, indexInfo.Dimension, indexInfo.VectorCount, stopwatch.Elapsed.TotalMilliseconds);
            }

            return indexInfo;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Failed to get index information", indexName, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] Index info retrieval failed: IndexName: {IndexName}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    indexName, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            _logger!.LogDebug("[VectorSearch] Starting list indices operation");
        }

        try
        {
            var response = await ExecuteQueryAsync<JsonElement>("vectorSearch:listIndices", null, cancellationToken);

            if (!response.TryGetProperty("indices", out var indicesElement))
            {
                stopwatch.Stop();
                var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                    "Invalid response from listIndices: missing indices");
                if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
                {
                    _logger!.LogError(error, "[VectorSearch] List indices failed: Invalid response, Response: {Response}, Duration: {DurationMs}ms",
                        response.GetRawText(), stopwatch.Elapsed.TotalMilliseconds);
                }
                throw error;
            }

            var indices = new List<VectorIndexInfo>();

            foreach (var indexElement in indicesElement.EnumerateArray())
            {
                indices.Add(new VectorIndexInfo
                {
                    Name = indexElement.GetProperty("name").GetString()!,
                    Dimension = indexElement.GetProperty("dimension").GetInt32(),
                    Metric = ParseDistanceMetric(indexElement.GetProperty("metric").GetString()!),
                    VectorCount = indexElement.TryGetProperty("vectorCount", out var countElement) ? countElement.GetInt64() : 0,
                    Table = indexElement.GetProperty("table").GetString()!,
                    VectorField = indexElement.GetProperty("vectorField").GetString()!,
                    FilterField = indexElement.TryGetProperty("filterField", out var filterElement) ? filterElement.GetString() : null,
                    CreatedAt = indexElement.TryGetProperty("createdAt", out var createdElement)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(createdElement.GetInt64())
                        : DateTimeOffset.MinValue,
                    UpdatedAt = indexElement.TryGetProperty("updatedAt", out var updatedElement)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(updatedElement.GetInt64())
                        : DateTimeOffset.MinValue
                });
            }

            stopwatch.Stop();
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[VectorSearch] List indices completed successfully: Count: {Count}, Duration: {DurationMs}ms",
                    indices.Count, stopwatch.Elapsed.TotalMilliseconds);
            }

            return indices;
        }
        catch (Exception ex) when (ex is not ConvexVectorSearchException)
        {
            stopwatch.Stop();
            var error = new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Failed to list vector indices", null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[VectorSearch] List indices failed: Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    #region HTTP Execution Methods

    private async Task<TResult> ExecuteQueryAsync<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildQueryRequest(
            _httpProvider.DeploymentUrl,
            functionName,
            args,
            _serializer);

        // Note: VectorSearch uses status/value wrapper format for queries
        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            functionName,
            "query",
            _serializer,
            cancellationToken);
    }

    private async Task<TResult> ExecuteActionAsync<TResult>(string functionName, object? args, CancellationToken cancellationToken)
    {
        var request = ConvexRequestBuilder.BuildActionRequest(
            _httpProvider.DeploymentUrl,
            functionName,
            args,
            _serializer);

        var response = await _httpProvider.SendAsync(request, cancellationToken);

        return await ConvexResponseParser.ParseResponseAsync<TResult>(
            response,
            functionName,
            "action",
            _serializer,
            cancellationToken);
    }

    #endregion

    #region Validation Methods

    private static void ValidateSearchParameters(string indexName, float[]? vector, int limit)
    {
        ValidateIndexName(indexName);

        if (vector == null || vector.Length == 0)
            throw new ConvexVectorSearchException(VectorSearchErrorType.InvalidDimensions,
                "Vector cannot be null or empty");

        if (limit <= 0 || limit > 1000)
            throw new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Limit must be between 1 and 1000");
    }

    private static void ValidateTextSearchParameters(string indexName, string? text, int limit)
    {
        ValidateIndexName(indexName);

        if (string.IsNullOrEmpty(text))
            throw new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Search text cannot be null or empty");

        if (limit <= 0 || limit > 1000)
            throw new ConvexVectorSearchException(VectorSearchErrorType.SearchFailed,
                "Limit must be between 1 and 1000");
    }

    private static void ValidateEmbeddingParameters(string? text, string model)
    {
        if (string.IsNullOrEmpty(text))
            throw new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "Text for embedding cannot be null or empty");

        if (string.IsNullOrEmpty(model))
            throw new ConvexVectorSearchException(VectorSearchErrorType.InvalidModel,
                "Model name cannot be null or empty");
    }

    private static void ValidateBatchEmbeddingParameters(string[]? texts, string model)
    {
        if (texts == null || texts.Length == 0)
            throw new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "Texts array cannot be null or empty");

        if (texts.Any(string.IsNullOrEmpty))
            throw new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "All texts must be non-empty");

        if (string.IsNullOrEmpty(model))
            throw new ConvexVectorSearchException(VectorSearchErrorType.InvalidModel,
                "Model name cannot be null or empty");

        if (texts.Length > 100)
            throw new ConvexVectorSearchException(VectorSearchErrorType.EmbeddingFailed,
                "Batch size cannot exceed 100 texts");
    }

    private static void ValidateIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
            throw new ConvexVectorSearchException(VectorSearchErrorType.IndexNotFound,
                "Index name cannot be null or empty");
    }

    #endregion

    #region Helper Methods

    private static T ExtractDocumentData<T>(JsonElement element)
    {
        var filteredData = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.StartsWith("_"))
            {
                filteredData[property.Name] = property.Value;
            }
        }

        var json = JsonSerializer.Serialize(filteredData);
        return JsonSerializer.Deserialize<T>(json)!;
    }

    private static float[]? ExtractVector(JsonElement element)
    {
        if (!element.TryGetProperty("_vector", out var vectorElement))
            return null;

        return [.. vectorElement.EnumerateArray().Select(v => v.GetSingle())];
    }

    private static Dictionary<string, JsonElement>? ExtractMetadata(JsonElement element)
    {
        var metadata = new Dictionary<string, JsonElement>();

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.StartsWith("_") && property.Name != "_id" && property.Name != "_score" && property.Name != "_vector")
            {
                metadata[property.Name] = property.Value;
            }
        }

        return metadata.Count > 0 ? metadata : null;
    }

    private static VectorDistanceMetric ParseDistanceMetric(string metric)
    {
        return metric.ToLowerInvariant() switch
        {
            "cosine" => VectorDistanceMetric.Cosine,
            "euclidean" or "l2" => VectorDistanceMetric.Euclidean,
            "dotproduct" or "dot_product" => VectorDistanceMetric.DotProduct,
            _ => VectorDistanceMetric.Cosine
        };
    }

    #endregion
}
