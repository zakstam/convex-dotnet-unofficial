# HttpActions Slice

## Purpose
Provides HTTP API endpoint functionality for building REST APIs directly in Convex. Supports all standard HTTP methods (GET, POST, PUT, DELETE, PATCH), file uploads, and webhook integrations.

## Responsibilities
- HTTP method operations (GET, POST, PUT, DELETE, PATCH)
- Custom HTTP method calls with strongly-typed requests/responses
- File upload through multipart form data
- Webhook endpoint integration with signature verification
- Response time tracking and error handling
- Query parameter and header management

## Public API Surface

### Main Interface
```csharp
public interface IConvexHttpActions
{
    // Standard HTTP methods
    Task<ConvexHttpActionResponse<T>> GetAsync<T>(...);
    Task<ConvexHttpActionResponse<T>> PostAsync<T>(...);
    Task<ConvexHttpActionResponse<TResponse>> PostAsync<TResponse, TBody>(...);
    Task<ConvexHttpActionResponse<T>> PutAsync<T>(...);
    Task<ConvexHttpActionResponse<TResponse>> PutAsync<TResponse, TBody>(...);
    Task<ConvexHttpActionResponse<T>> DeleteAsync<T>(...);
    Task<ConvexHttpActionResponse<T>> PatchAsync<T>(...);
    Task<ConvexHttpActionResponse<TResponse>> PatchAsync<TResponse, TBody>(...);

    // Custom methods
    Task<ConvexHttpActionResponse<T>> CallAsync<T>(HttpMethod method, ...);
    Task<ConvexHttpActionResponse<TResponse>> CallAsync<TResponse, TBody>(HttpMethod method, ...);

    // File operations
    Task<ConvexHttpActionResponse<T>> UploadFileAsync<T>(...);

    // Webhook support
    Task<ConvexHttpActionResponse<TResponse>> CallWebhookAsync<TResponse, TPayload>(...);
}
```

### Response Types
```csharp
public class ConvexHttpActionResponse<T>
{
    public HttpStatusCode StatusCode { get; }
    public bool IsSuccess { get; }
    public T? Body { get; }
    public string? RawBody { get; }
    public Dictionary<string, string> Headers { get; }
    public string? ContentType { get; }
    public double ResponseTimeMs { get; }
    public ConvexHttpActionError? Error { get; }
}

public class ConvexHttpActionError
{
    public string? Code { get; }
    public string Message { get; }
    public JsonElement? Details { get; }
    public string? StackTrace { get; }
}
```

### Exception Types
```csharp
public class ConvexHttpActionException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? ActionPath { get; }
    public HttpActionErrorType ErrorType { get; }
}

public enum HttpActionErrorType
{
    ActionNotFound,
    InvalidRequest,
    AuthenticationRequired,
    AccessDenied,
    RateLimitExceeded,
    PayloadTooLarge,
    UnsupportedMediaType,
    InternalError,
    NetworkError,
    Timeout,
    Unknown
}
```

## Shared Dependencies
- **IHttpClientProvider**: For HTTP request execution with authentication
- **IConvexSerializer**: For JSON serialization/deserialization

## Architecture
- **HttpActionsSlice**: Public facade implementing IConvexHttpActions
- **HttpActionsImplementation**: Internal implementation with direct HTTP calls
- **URL Pattern**: `{deploymentUrl}/api/{actionPath}` (different from other slices)

## Usage Example
```csharp
// GET request
var response = await client.HttpActionsSlice.GetAsync<User>(
    "users/123",
    queryParameters: new Dictionary<string, string> { ["include"] = "profile" }
);

// POST request with body
var newUser = new User { Name = "John", Email = "john@example.com" };
var createResponse = await client.HttpActionsSlice.PostAsync<User, User>(
    "users",
    newUser
);

// File upload
using var fileStream = File.OpenRead("document.pdf");
var uploadResponse = await client.HttpActionsSlice.UploadFileAsync<UploadResult>(
    "documents/upload",
    fileStream,
    "document.pdf",
    "application/pdf"
);

// Webhook call
var webhookPayload = new { Event = "user.created", UserId = "123" };
var webhookResponse = await client.HttpActionsSlice.CallWebhookAsync<AckResponse, object>(
    "webhooks/github",
    webhookPayload,
    signature: "sha256=..."
);
```

## Implementation Details
- Uses Stopwatch for accurate response time measurement
- Supports custom headers for all requests
- Automatically adds "Convex-Client" header with version info
- Handles multipart form data for file uploads
- Validates action paths to prevent path traversal attacks
- Comprehensive error handling with typed exceptions
- IHttpClientProvider handles authentication automatically

## Error Handling
- Network errors → `HttpActionErrorType.NetworkError`
- Timeouts → `HttpActionErrorType.Timeout`
- Invalid paths → `HttpActionErrorType.InvalidRequest`
- Response parsing errors are handled gracefully
- Non-success status codes include error details when available

## Owner
TBD
