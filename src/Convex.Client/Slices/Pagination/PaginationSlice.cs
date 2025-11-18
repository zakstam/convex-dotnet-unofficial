using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Slices.Pagination;

/// <summary>
/// Pagination slice - provides cursor-based pagination for Convex queries.
/// This is a self-contained vertical slice that handles all pagination functionality.
/// </summary>
public class PaginationSlice(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IConvexPagination
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    public IPaginationBuilder<T> Query<T>(string functionName) => new PaginationBuilder<T>(_httpProvider, _serializer, functionName, _logger, _enableDebugLogging);
}
