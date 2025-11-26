using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Convex.Client.Infrastructure.Http;
using Convex.Client.Infrastructure.Serialization;
using Convex.Client.Infrastructure.Telemetry;
using Microsoft.Extensions.Logging;

namespace Convex.Client.Features.Operational.HttpActions;

/// <summary>
/// Internal implementation of HTTP Actions functionality using Shared infrastructure.
/// </summary>
internal class HttpActionsImplementation(IHttpClientProvider httpProvider, IConvexSerializer serializer, ILogger? logger = null, bool enableDebugLogging = false)
{
    private readonly IHttpClientProvider _httpProvider = httpProvider ?? throw new ArgumentNullException(nameof(httpProvider));
    private readonly IConvexSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly ILogger? _logger = logger;
    private readonly bool _enableDebugLogging = enableDebugLogging;

    public Task<ConvexHttpActionResponse<T>> GetAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => CallAsync<T>(HttpMethod.Get, actionPath, queryParameters: queryParameters, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PostAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => CallAsync<T>(HttpMethod.Post, actionPath, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull => CallAsync<TResponse, TBody>(HttpMethod.Post, actionPath, body, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PutAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => CallAsync<T>(HttpMethod.Put, actionPath, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull => CallAsync<TResponse, TBody>(HttpMethod.Put, actionPath, body, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(string actionPath, Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => CallAsync<T>(HttpMethod.Delete, actionPath, queryParameters: queryParameters, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<T>> PatchAsync<T>(string actionPath, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) => CallAsync<T>(HttpMethod.Patch, actionPath, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull => CallAsync<TResponse, TBody>(HttpMethod.Patch, actionPath, body, contentType, queryParameters: null, headers: headers, cancellationToken: cancellationToken);

    public async Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, string actionPath, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var queryParamsStr = queryParameters != null ? string.Join(", ", queryParameters.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
            var headersStr = headers != null ? string.Join(", ", headers.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
            _logger!.LogDebug("[HttpAction] Starting HTTP action call: Method: {Method}, ActionPath: {ActionPath}, ContentType: {ContentType}, QueryParameters: {QueryParameters}, Headers: {Headers}",
                method, actionPath, contentType, queryParamsStr, headersStr);
        }

        try
        {
            ValidateActionPath(actionPath);

            var url = BuildActionUrl(actionPath, queryParameters);
            var request = new HttpRequestMessage(method, url);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[HttpAction] HTTP action request built: Method: {Method}, URL: {Url}", method, url);
            }

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _ = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Set Convex-specific headers
            request.Headers.Add("Convex-Client", $"dotnet-{GetClientVersion()}");

            var response = await _httpProvider.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var responseHeadersStr = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                _logger!.LogDebug("[HttpAction] HTTP action response received: Method: {Method}, ActionPath: {ActionPath}, StatusCode: {StatusCode}, Headers: {Headers}, Duration: {DurationMs}ms",
                    method, actionPath, response.StatusCode, responseHeadersStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            var result = await ProcessHttpActionResponse<T>(response, actionPath, stopwatch.Elapsed.TotalMilliseconds);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var bodyStr = result.RawBody?.Length > 1000 ? result.RawBody[..1000] + "..." : result.RawBody ?? "null";
                _logger!.LogDebug("[HttpAction] HTTP action call completed: Method: {Method}, ActionPath: {ActionPath}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}, Duration: {DurationMs}ms",
                    method, actionPath, result.StatusCode, bodyStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex) when (ex is not ConvexHttpActionException)
        {
            stopwatch.Stop();

            var errorType = ex switch
            {
                HttpRequestException => HttpActionErrorType.NetworkError,
                TaskCanceledException => HttpActionErrorType.Timeout,
                _ => HttpActionErrorType.Unknown
            };

            var error = new ConvexHttpActionException(errorType, $"HTTP action call failed: {ex.Message}", actionPath, null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[HttpAction] HTTP action call failed: Method: {Method}, ActionPath: {ActionPath}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    method, actionPath, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, string actionPath, TBody body, string contentType = "application/json", Dictionary<string, string>? queryParameters = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TBody : notnull
    {
        var stopwatch = Stopwatch.StartNew();

        // Serialize body once and reuse for both logging and request
        string bodyContent;
        if (body is string stringBody)
        {
            bodyContent = stringBody;
        }
        else
        {
            bodyContent = _serializer.Serialize(body);
            if (bodyContent == null)
            {
                throw new ConvexHttpActionException(HttpActionErrorType.InvalidRequest,
                    $"Failed to serialize request body for action '{actionPath}'. Serializer returned null.",
                    actionPath);
            }
        }

        if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
        {
            var queryParamsStr = queryParameters != null ? string.Join(", ", queryParameters.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
            var headersStr = headers != null ? string.Join(", ", headers.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
            var bodyStr = bodyContent.Length > 1000 ? bodyContent[..1000] + "..." : bodyContent;
            _logger!.LogDebug("[HttpAction] Starting HTTP action call with body: Method: {Method}, ActionPath: {ActionPath}, ContentType: {ContentType}, QueryParameters: {QueryParameters}, Headers: {Headers}, Body: {Body}",
                method, actionPath, contentType, queryParamsStr, headersStr, bodyStr);
        }

        try
        {
            ValidateActionPath(actionPath);

            var url = BuildActionUrl(actionPath, queryParameters);
            var request = new HttpRequestMessage(method, url);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogDebug("[HttpAction] HTTP action request built: Method: {Method}, URL: {Url}", method, url);
            }

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _ = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body for methods that support it
            if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
            {
                request.Content = new StringContent(bodyContent, Encoding.UTF8, contentType);
            }

            // Set Convex-specific headers
            request.Headers.Add("Convex-Client", $"dotnet-{GetClientVersion()}");

            var response = await _httpProvider.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var responseHeadersStr = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                _logger!.LogDebug("[HttpAction] HTTP action response received: Method: {Method}, ActionPath: {ActionPath}, StatusCode: {StatusCode}, Headers: {Headers}, Duration: {DurationMs}ms",
                    method, actionPath, response.StatusCode, responseHeadersStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            var result = await ProcessHttpActionResponse<TResponse>(response, actionPath, stopwatch.Elapsed.TotalMilliseconds);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var responseBodyStr = result.RawBody?.Length > 1000 ? result.RawBody[..1000] + "..." : result.RawBody ?? "null";
                _logger!.LogDebug("[HttpAction] HTTP action call completed: Method: {Method}, ActionPath: {ActionPath}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}, Duration: {DurationMs}ms",
                    method, actionPath, result.StatusCode, responseBodyStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex) when (ex is not ConvexHttpActionException)
        {
            stopwatch.Stop();

            var errorType = ex switch
            {
                HttpRequestException => HttpActionErrorType.NetworkError,
                TaskCanceledException => HttpActionErrorType.Timeout,
                _ => HttpActionErrorType.Unknown
            };

            var error = new ConvexHttpActionException(errorType, $"HTTP action call failed: {ex.Message}", actionPath, null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[HttpAction] HTTP action call failed: Method: {Method}, ActionPath: {ActionPath}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    method, actionPath, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(string actionPath, Stream fileContent, string fileName, string contentType, Dictionary<string, string>? additionalFields = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        if (fileContent == null)
        {
            throw new ArgumentNullException(nameof(fileContent), "File content stream cannot be null");
        }

        var stopwatch = Stopwatch.StartNew();
        long? fileSize = null;

        try
        {
            if (fileContent.CanSeek)
            {
                fileSize = fileContent.Length;
            }

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var additionalFieldsStr = additionalFields != null ? string.Join(", ", additionalFields.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
                var headersStr = headers != null ? string.Join(", ", headers.Select(kvp => $"{kvp.Key}={kvp.Value}")) : "none";
                _logger!.LogDebug("[HttpAction] Starting file upload: ActionPath: {ActionPath}, FileName: {FileName}, ContentType: {ContentType}, Size: {Size}, AdditionalFields: {AdditionalFields}, Headers: {Headers}",
                    actionPath, fileName, contentType, fileSize?.ToString() ?? "unknown", additionalFieldsStr, headersStr);
            }

            ValidateActionPath(actionPath);

            var url = BuildActionUrl(actionPath);
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Add custom headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _ = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Create multipart form content
            using var formData = new MultipartFormDataContent();

            // Add the file
            var fileStreamContent = new StreamContent(fileContent);
            fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formData.Add(fileStreamContent, "file", fileName);

            // Add additional form fields
            if (additionalFields != null)
            {
                foreach (var field in additionalFields)
                {
                    formData.Add(new StringContent(field.Value), field.Key);
                }
            }

            request.Content = formData;

            // Set Convex-specific headers
            request.Headers.Add("Convex-Client", $"dotnet-{GetClientVersion()}");

            var response = await _httpProvider.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var responseHeadersStr = string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"));
                _logger!.LogDebug("[HttpAction] File upload response received: ActionPath: {ActionPath}, StatusCode: {StatusCode}, Headers: {Headers}, Duration: {DurationMs}ms",
                    actionPath, response.StatusCode, responseHeadersStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            var result = await ProcessHttpActionResponse<T>(response, actionPath, stopwatch.Elapsed.TotalMilliseconds);

            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                var responseBodyStr = result.RawBody?.Length > 1000 ? result.RawBody[..1000] + "..." : result.RawBody ?? "null";
                _logger!.LogDebug("[HttpAction] File upload completed: ActionPath: {ActionPath}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}, Duration: {DurationMs}ms",
                    actionPath, result.StatusCode, responseBodyStr, stopwatch.Elapsed.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex) when (ex is not ConvexHttpActionException)
        {
            stopwatch.Stop();

            var error = new ConvexHttpActionException(HttpActionErrorType.NetworkError,
                $"File upload failed: {ex.Message}", actionPath, null, ex);
            if (ConvexLoggerExtensions.IsDebugLoggingEnabled(_logger, _enableDebugLogging))
            {
                _logger!.LogError(ex, "[HttpAction] File upload failed: ActionPath: {ActionPath}, Duration: {DurationMs}ms, ErrorType: {ErrorType}, Message: {Message}, StackTrace: {StackTrace}",
                    actionPath, stopwatch.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
            }
            throw error;
        }
    }

    public async Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(string webhookPath, TPayload payload, string? signature = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default) where TPayload : notnull
    {
        var customHeaders = headers ?? [];

        // Add webhook-specific headers
        if (signature != null)
        {
            customHeaders["X-Convex-Signature"] = signature;
        }

        customHeaders["X-Webhook-Event"] = "true";

        return await PostAsync<TResponse, TPayload>(webhookPath, payload, "application/json", customHeaders, cancellationToken);
    }

    #region Private Helper Methods

    private async Task<ConvexHttpActionResponse<T>> ProcessHttpActionResponse<T>(HttpResponseMessage response, string actionPath, double responseTimeMs)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var responseHeaders = response.GetAllHeaders();
        var rawBody = await response.ReadContentAsStringAsync();
        var contentType = response.GetContentType();

        // Try to deserialize the body
        T? body = default;
        ConvexHttpActionError? error = null;

        if (!string.IsNullOrEmpty(rawBody))
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        body = (T)(object)rawBody;
                    }
                    else if (contentType?.Contains("application/json") == true)
                    {
                        body = _serializer.Deserialize<T>(rawBody);
                    }
                }
                catch (JsonException)
                {
                    // Failed to deserialize, body will remain default
                }
            }
            else
            {
                // Try to parse error response
                try
                {
                    var errorElement = JsonSerializer.Deserialize<JsonElement>(rawBody);
                    error = new ConvexHttpActionError
                    {
                        Code = errorElement.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null,
                        Message = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() ?? "Unknown error" : "Unknown error",
                        Details = errorElement.TryGetProperty("details", out var detailsElement) ? detailsElement : null,
                        StackTrace = errorElement.TryGetProperty("stackTrace", out var stackElement) ? stackElement.GetString() : null
                    };
                }
                catch (JsonException)
                {
                    error = new ConvexHttpActionError
                    {
                        Message = rawBody
                    };
                }
            }
        }

        return new ConvexHttpActionResponse<T>
        {
            StatusCode = response.StatusCode,
            Body = body,
            RawBody = rawBody,
            Headers = responseHeaders,
            ContentType = contentType,
            ResponseTimeMs = responseTimeMs,
            Error = error
        };
    }

    private string BuildActionUrl(string actionPath, Dictionary<string, string>? queryParameters = null)
    {
        var cleanPath = actionPath.TrimStart('/');
        var url = $"{_httpProvider.DeploymentUrl.TrimEnd('/')}/api/{cleanPath}";

        if (queryParameters != null && queryParameters.Count > 0)
        {
            var queryString = string.Join("&", queryParameters.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            url += "?" + queryString;
        }

        return url;
    }

    private static void ValidateActionPath(string actionPath)
    {
        if (string.IsNullOrEmpty(actionPath))
        {
            throw new ConvexHttpActionException(HttpActionErrorType.InvalidRequest,
                "Action path cannot be null or empty");
        }

        // Basic validation to prevent path traversal
        if (actionPath.Contains("..") || actionPath.Contains("//"))
        {
            throw new ConvexHttpActionException(HttpActionErrorType.InvalidRequest,
                "Invalid action path format", actionPath);
        }
    }

    private static string GetClientVersion() => typeof(HttpActionsImplementation).Assembly.GetName().Version?.ToString() ?? "unknown";

    #endregion
}
