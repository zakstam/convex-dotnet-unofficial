using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Slices.HttpActions;

/// <summary>
/// HttpActions slice - provides HTTP API endpoints for building REST APIs in Convex.
/// This is a self-contained vertical slice that handles all HTTP action functionality.
/// </summary>
public class HttpActionsSlice(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false) : IConvexHttpActions
{
    private readonly HttpActionsImplementation _implementation = new HttpActionsImplementation(httpProvider, serializer, logger, enableDebugLogging);

    public Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.GetAsync<T>(actionPath, queryParameters, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.PostAsync<T>(actionPath, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull
        => _implementation.PostAsync<TResponse, TBody>(actionPath, body, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.PutAsync<T>(actionPath, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull
        => _implementation.PutAsync<TResponse, TBody>(actionPath, body, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.DeleteAsync<T>(actionPath, queryParameters, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.PatchAsync<T>(actionPath, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull
        => _implementation.PatchAsync<TResponse, TBody>(actionPath, body, contentType, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.CallAsync<T>(method, actionPath, contentType, queryParameters, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull
        => _implementation.CallAsync<TResponse, TBody>(method, actionPath, body, contentType, queryParameters, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => _implementation.UploadFileAsync<T>(actionPath, fileContent, fileName, contentType, additionalFields, headers, cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull
        => _implementation.CallWebhookAsync<TResponse, TPayload>(webhookPath, payload, signature, headers, cancellationToken);
}
