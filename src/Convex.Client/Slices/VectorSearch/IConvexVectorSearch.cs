using System.Text.Json;

namespace Convex.Client.Slices.VectorSearch;

/// <summary>
/// Interface for Convex vector search operations.
/// Provides similarity search, vector indexing, and AI-powered search capabilities.
/// </summary>
public interface IConvexVectorSearch
{
    /// <summary>
    /// Performs a similarity search using a vector query without a filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<T>>> SearchAsync<T>(string indexName, float[] vector, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search using a vector query with a strongly-typed filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<TResult>>> SearchAsync<TResult, TFilter>(string indexName, float[] vector, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;

    /// <summary>
    /// Performs a similarity search using text that will be converted to a vector without a filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<T>>> SearchByTextAsync<T>(string indexName, string text, string embeddingModel = "text-embedding-ada-002", int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a similarity search using text that will be converted to a vector with a strongly-typed filter.
    /// </summary>
    Task<IEnumerable<VectorSearchResult<TResult>>> SearchByTextAsync<TResult, TFilter>(string indexName, string text, string embeddingModel, int limit, TFilter filter, CancellationToken cancellationToken = default) where TFilter : notnull;

    /// <summary>
    /// Creates a text embedding using the specified model.
    /// </summary>
    Task<float[]> CreateEmbeddingAsync(string text, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates embeddings for multiple texts in a single batch operation.
    /// </summary>
    Task<float[][]> CreateEmbeddingsAsync(string[] texts, string model = "text-embedding-ada-002", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a vector index.
    /// </summary>
    Task<VectorIndexInfo> GetIndexInfoAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available vector indices.
    /// </summary>
    Task<IEnumerable<VectorIndexInfo>> ListIndicesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a result from a vector similarity search.
/// </summary>
public class VectorSearchResult<T>
{
    public required string Id { get; init; }
    public required float Score { get; init; }
    public required T Data { get; init; }
    public float[]? Vector { get; init; }
    public Dictionary<string, JsonElement>? Metadata { get; init; }
}

/// <summary>
/// Information about a vector index.
/// </summary>
public class VectorIndexInfo
{
    public required string Name { get; init; }
    public required int Dimension { get; init; }
    public required VectorDistanceMetric Metric { get; init; }
    public long VectorCount { get; init; }
    public required string Table { get; init; }
    public required string VectorField { get; init; }
    public string? FilterField { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Distance metrics used for vector similarity calculations.
/// </summary>
public enum VectorDistanceMetric
{
    Cosine,
    Euclidean,
    DotProduct
}

/// <summary>
/// Exception thrown when vector search operations fail.
/// </summary>
public class ConvexVectorSearchException(VectorSearchErrorType errorType, string message, string? indexName = null, Exception? innerException = null) : Exception(message, innerException)
{
    public VectorSearchErrorType ErrorType { get; } = errorType;
    public string? IndexName { get; } = indexName;
}

/// <summary>
/// Types of vector search errors.
/// </summary>
public enum VectorSearchErrorType
{
    IndexNotFound,
    InvalidDimensions,
    InvalidFilter,
    EmbeddingFailed,
    SearchFailed,
    InvalidModel,
    RateLimitExceeded,
    QuotaExceeded
}
