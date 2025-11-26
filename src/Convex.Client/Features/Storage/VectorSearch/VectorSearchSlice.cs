using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Storage.VectorSearch;

/// <summary>
/// VectorSearch slice - provides AI-powered similarity search and vector operations.
/// This is a self-contained vertical slice that handles all vector search functionality.
/// </summary>
public class VectorSearchSlice(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false) : IConvexVectorSearch
{
    private readonly VectorSearchImplementation _implementation = new VectorSearchImplementation(httpProvider, serializer, logger, enableDebugLogging);

    public Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default)
        => _implementation.SearchAsync<T>(indexName, vector, limit, cancellationToken);

    public Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull
        => _implementation.SearchAsync<TResult, TFilter>(indexName, vector, limit, filter, cancellationToken);

    public Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default)
        => _implementation.SearchByTextAsync<T>(indexName, text, embeddingModel, limit, cancellationToken);

    public Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull
        => _implementation.SearchByTextAsync<TResult, TFilter>(indexName, text, embeddingModel, limit, filter, cancellationToken);

    public Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)
        => _implementation.CreateEmbeddingAsync(text, model, cancellationToken);

    public Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default)
        => _implementation.CreateEmbeddingsAsync(texts, model, cancellationToken);

    public Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default)
        => _implementation.GetIndexInfoAsync(indexName, cancellationToken);

    public Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default)
        => _implementation.ListIndicesAsync(cancellationToken);
}
