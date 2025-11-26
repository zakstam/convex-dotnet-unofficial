using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.RealTime.Pagination;

/// <summary>
/// Fluent builder for creating paginated queries.
/// </summary>
internal sealed class PaginationBuilder<T>(
    IHttpClientProvider httpProvider,
    IConvexSerializer serializer,
    string functionName,
    ILogger? logger = null,
    bool enableDebugLogging = false) : IPaginationBuilder<T>
{
    private readonly IHttpClientProvider _httpProvider = httpProvider;
    private readonly IConvexSerializer _serializer = serializer;
    private readonly string _functionName = functionName;
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    private int _pageSize = 20;
    private object? _args;

    public IPaginationBuilder<T> WithPageSize(int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");

        _pageSize = pageSize;
        return this;
    }

    public IPaginationBuilder<T> WithArgs<TArgs>(TArgs args) where TArgs : notnull
    {
        _args = args;
        return this;
    }

    public IPaginationBuilder<T> WithArgs<TArgs>(Action<TArgs> configure) where TArgs : class, new()
    {
        var argsInstance = new TArgs();
        configure(argsInstance);
        _args = argsInstance;
        return this;
    }

    public IPaginator<T> Build() => new Paginator<T>(_httpProvider, _serializer, _functionName, _pageSize, _args, _logger, _enableDebugLogging);
}
