using System.Net;
using System.Text.Json;

namespace Convex.Client.Features.Operational.HttpActions;

/// <summary>
/// Interface for Convex HTTP Actions - building HTTP APIs directly in Convex.
/// Provides capabilities to create REST endpoints, handle webhooks, and build custom HTTP services.
/// HTTP Actions allow you to expose Convex functions as HTTP endpoints that can be called from any HTTP client.
/// </summary>
/// <remarks>
/// <para>
/// HTTP Actions enable you to:
/// <list type="bullet">
/// <item>Build REST APIs directly in Convex</item>
/// <item>Handle webhooks from external services</item>
/// <item>Create public endpoints for third-party integrations</item>
/// <item>Support file uploads via multipart form data</item>
/// </list>
/// </para>
/// <para>
/// HTTP Actions are defined in your Convex backend using the `httpAction` function.
/// The action path corresponds to the route you define in your backend.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // GET request
    /// var response = await client.Http.GetAsync&lt;User&gt;(
    ///     actionPath: "users/123",
    ///     queryParameters: new Dictionary&lt;string, string&gt; { ["include"] = "profile" }
    /// );
    ///
    /// if (response.IsSuccess)
    /// {
    ///     var user = response.Body;
    ///     Console.WriteLine($"User: {user.Name}");
    /// }
    ///
    /// // POST request with body
    /// var createResponse = await client.Http.PostAsync&lt;User, CreateUserRequest&gt;(
    ///     actionPath: "users",
    ///     body: new CreateUserRequest { Name = "John", Email = "john@example.com" }
    /// );
    /// </code>
    /// </example>
    /// <seealso cref="HttpActionsSlice"/>
    public interface IConvexHttpActions
{
    /// <summary>
    /// Calls a Convex HTTP action endpoint using GET method.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint using POST method without a body.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint using POST method with strongly-typed body.
    /// </summary>
    Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;

    /// <summary>
    /// Calls a Convex HTTP action endpoint using PUT method without a body.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint using PUT method with strongly-typed body.
    /// </summary>
    Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;

    /// <summary>
    /// Calls a Convex HTTP action endpoint using DELETE method.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint using PATCH method without a body.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint using PATCH method with strongly-typed body.
    /// </summary>
    Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;

    /// <summary>
    /// Calls a Convex HTTP action endpoint with a custom HTTP method without a body.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a Convex HTTP action endpoint with a custom HTTP method and strongly-typed body.
    /// </summary>
    Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull;

    /// <summary>
    /// Uploads a file through a Convex HTTP action endpoint.
    /// </summary>
    Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a webhook endpoint with strongly-typed payload (similar to HTTP action but optimized for webhook patterns).
    /// </summary>
    Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull;
}

/// <summary>
/// Response from a Convex HTTP action.
/// </summary>
public class ConvexHttpActionResponse<T>
{
    /// <summary>
    /// The HTTP status code of the response.
    /// </summary>
    public required HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// Whether the request was successful (2xx status code).
    /// </summary>
    public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;

    /// <summary>
    /// The response body deserialized to the specified type.
    /// </summary>
    public T? Body { get; init; }

    /// <summary>
    /// The raw response body as a string.
    /// </summary>
    public string? RawBody { get; init; }

    /// <summary>
    /// The response headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = [];

    /// <summary>
    /// The content type of the response.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// The response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; init; }

    /// <summary>
    /// Error information if the request failed.
    /// </summary>
    public ConvexHttpActionError? Error { get; init; }
}

/// <summary>
/// Error information for failed HTTP action requests.
/// </summary>
public class ConvexHttpActionError
{
    /// <summary>
    /// The error code from the HTTP action.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// The error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Additional error details.
    /// </summary>
    public JsonElement? Details { get; init; }

    /// <summary>
    /// The stack trace if available.
    /// </summary>
    public string? StackTrace { get; init; }
}

/// <summary>
/// Exception thrown when HTTP action operations fail.
/// </summary>
public class ConvexHttpActionException(HttpActionErrorType errorType, string message, string? actionPath = null, HttpStatusCode? statusCode = null, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>
    /// The HTTP status code of the failed request.
    /// </summary>
    public HttpStatusCode? StatusCode { get; } = statusCode;

    /// <summary>
    /// The action path that failed.
    /// </summary>
    public string? ActionPath { get; } = actionPath;

    /// <summary>
    /// The type of HTTP action error that occurred.
    /// </summary>
    public HttpActionErrorType ErrorType { get; } = errorType;
}

/// <summary>
/// Types of HTTP action errors.
/// </summary>
public enum HttpActionErrorType
{
    /// <summary>
    /// HTTP action endpoint not found.
    /// </summary>
    ActionNotFound,

    /// <summary>
    /// Invalid request format or parameters.
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// Authentication required for the action.
    /// </summary>
    AuthenticationRequired,

    /// <summary>
    /// Access denied to the action.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Rate limit exceeded for HTTP actions.
    /// </summary>
    RateLimitExceeded,

    /// <summary>
    /// Request payload too large.
    /// </summary>
    PayloadTooLarge,

    /// <summary>
    /// Unsupported media type.
    /// </summary>
    UnsupportedMediaType,

    /// <summary>
    /// Internal server error in the action.
    /// </summary>
    InternalError,

    /// <summary>
    /// Network error during request.
    /// </summary>
    NetworkError,

    /// <summary>
    /// Request timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown
}
