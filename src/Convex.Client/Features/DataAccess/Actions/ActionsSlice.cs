using Convex.Client.Infrastructure.Builders;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.DataAccess.Actions;

/// <summary>
/// Actions slice - provides HTTP action execution for Convex functions.
/// This is a self-contained vertical slice that handles all action-related functionality.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ActionsSlice"/> class.
/// </remarks>
/// <param name="httpProvider">The HTTP client provider for making requests.</param>
/// <param name="serializer">The serializer for handling Convex JSON format.</param>
/// <param name="logger">Optional logger for debug logging.</param>
/// <param name="enableDebugLogging">Whether debug logging is enabled.</param>
public class ActionsSlice(
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
    /// Creates an action builder for the specified Convex function.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the action.</typeparam>
    /// <param name="functionName">The name of the Convex action to execute.</param>
    /// <param name="middlewareExecutor">Optional middleware executor for intercepting requests.</param>
    /// <returns>An action builder for fluent configuration and execution.</returns>
    public IActionBuilder<TResult> Action<TResult>(
        string functionName,
        Func<string, string, object?, TimeSpan?, CancellationToken, Task<TResult>>? middlewareExecutor = null)
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("Function name cannot be null or whitespace.", nameof(functionName));

        return new ActionBuilder<TResult>(_httpProvider, _serializer, functionName, middlewareExecutor, _logger, _enableDebugLogging);
    }
}
