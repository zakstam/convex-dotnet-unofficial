using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Local HTTP server to receive OAuth 2.0 authorization callbacks from Clerk.
/// Listens on localhost for the redirect after user authentication.
/// </summary>
/// <remarks>
/// Initializes a new instance of ClerkOAuthCallbackServer.
/// </remarks>
/// <param name="port">Port to listen on (default: 8080).</param>
/// <param name="callbackPath">Callback path (default: "/callback").</param>
public class ClerkOAuthCallbackServer(int port = 8080, string callbackPath = "/callback") : IDisposable
{
    private readonly HttpListener _listener = new HttpListener();
    private readonly int _port = port;
    private readonly string _callbackPath = callbackPath.StartsWith("/") ? callbackPath : "/" + callbackPath;
    private readonly TaskCompletionSource<OAuthCallbackResult> _callbackReceived = new TaskCompletionSource<OAuthCallbackResult>();
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning = false;
    private bool _disposed;

    /// <summary>
    /// Result of an OAuth callback.
    /// </summary>
    public class OAuthCallbackResult
    {
        public bool Success { get; set; }
        public string? AuthorizationCode { get; set; }
        public string? State { get; set; }
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
    }

    /// <summary>
    /// Gets the callback URL that should be used in OAuth requests.
    /// </summary>
    public string CallbackUrl => $"http://localhost:{_port}{_callbackPath}";

    /// <summary>
    /// Gets whether the server is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Starts the HTTP server and begins listening for callbacks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown if server is already running.</exception>
    /// <exception cref="HttpListenerException">Thrown if port is already in use or permission denied.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Try primary port first
            if (!TryStartListener(_port))
            {
                // Try alternative ports if primary is in use
                var started = false;
                for (var alternativePort = _port + 1; alternativePort < _port + 10; alternativePort++)
                {
                    if (TryStartListener(alternativePort))
                    {
                        started = true;
                        break;
                    }
                }

                if (!started)
                {
                    throw new InvalidOperationException(
                        $"Unable to start OAuth callback server. Ports {_port}-{_port + 9} are all in use. " +
                        "Please close other applications and try again.");
                }
            }

            _isRunning = true;

            // Start processing requests in background
            _ = ProcessRequestsAsync(_cancellationTokenSource.Token);
        }
        catch (HttpListenerException ex)
        {
            throw new InvalidOperationException(
                $"Failed to start OAuth callback server on port {_port}. " +
                $"Error: {ex.Message}", ex);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Attempts to start the listener on a specific port.
    /// </summary>
    private bool TryStartListener(int port)
    {
        try
        {
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://localhost:{port}{_callbackPath}/");
            _listener.Start();
            return true;
        }
        catch (HttpListenerException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for an OAuth callback to be received.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds (default: 300 = 5 minutes).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The callback result containing authorization code or error.</returns>
    public async Task<OAuthCallbackResult> WaitForCallbackAsync(
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Server is not running. Call StartAsync() first.");
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            // Wait for callback or cancellation
            var completedTask = await Task.WhenAny(
                _callbackReceived.Task,
                Task.Delay(Timeout.Infinite, linkedCts.Token));

            if (completedTask == _callbackReceived.Task)
            {
                return await _callbackReceived.Task;
            }
            else
            {
                return new OAuthCallbackResult
                {
                    Success = false,
                    Error = "timeout",
                    ErrorDescription = "Authentication timed out. Please try again."
                };
            }
        }
        catch (OperationCanceledException)
        {
            return new OAuthCallbackResult
            {
                Success = false,
                Error = "cancelled",
                ErrorDescription = "Authentication was cancelled."
            };
        }
    }

    /// <summary>
    /// Stops the HTTP server.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _cancellationTokenSource?.Cancel();
        _listener.Stop();
        _isRunning = false;
    }

    /// <summary>
    /// Processes incoming HTTP requests.
    /// </summary>
    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Listener was disposed
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Log error
            System.Diagnostics.Debug.WriteLine($"[ClerkOAuthCallbackServer] Error processing requests: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles an individual HTTP request.
    /// </summary>
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // Parse query parameters
            var queryParams = request.QueryString;

            // Check for authorization code
            var code = queryParams["code"];
            var state = queryParams["state"];
            var error = queryParams["error"];
            var errorDescription = queryParams["error_description"];

            OAuthCallbackResult result;

            if (!string.IsNullOrEmpty(error))
            {
                // OAuth error response
                result = new OAuthCallbackResult
                {
                    Success = false,
                    Error = error,
                    ErrorDescription = errorDescription ?? "Authentication failed."
                };
            }
            else if (!string.IsNullOrEmpty(code))
            {
                // Success - received authorization code
                result = new OAuthCallbackResult
                {
                    Success = true,
                    AuthorizationCode = code,
                    State = state
                };
            }
            else
            {
                // Invalid response
                result = new OAuthCallbackResult
                {
                    Success = false,
                    Error = "invalid_response",
                    ErrorDescription = "Invalid OAuth callback. Missing code parameter."
                };
            }

            // Send result to waiting task
            _ = _callbackReceived.TrySetResult(result);

            // Send HTML response to browser
            var htmlResponse = GenerateHtmlResponse(result);
            var buffer = Encoding.UTF8.GetBytes(htmlResponse);

            response.ContentType = "text/html";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;

            await response.OutputStream.WriteAsync(buffer.AsMemory(0, buffer.Length));
            response.OutputStream.Close();

            // Stop server after callback received
            Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClerkOAuthCallbackServer] Error handling request: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates an HTML response to show in the browser after callback.
    /// </summary>
    private string GenerateHtmlResponse(OAuthCallbackResult result)
    {
        if (result.Success)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>Authentication Successful</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }
        .container {
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }
        .success-icon {
            font-size: 64px;
            color: #52c41a;
            margin-bottom: 20px;
        }
        h1 {
            color: #333;
            margin: 0 0 10px 0;
        }
        p {
            color: #666;
            margin: 10px 0;
        }
        .close-message {
            margin-top: 20px;
            font-size: 14px;
            color: #999;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='success-icon'>✓</div>
        <h1>Authentication Successful!</h1>
        <p>You have successfully signed in with Clerk.</p>
        <p class='close-message'>You can close this window and return to your application.</p>
    </div>
    <script>
        // Auto-close after 3 seconds (optional)
        setTimeout(function() {
            window.close();
        }, 3000);
    </script>
</body>
</html>";
        }
        else
        {
            var errorMessage = HttpUtility.HtmlEncode(result.ErrorDescription ?? "Unknown error");
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Authentication Failed</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }}
        .error-icon {{
            font-size: 64px;
            color: #f5222d;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #333;
            margin: 0 0 10px 0;
        }}
        p {{
            color: #666;
            margin: 10px 0;
        }}
        .error-details {{
            background: #fff1f0;
            border: 1px solid #ffccc7;
            border-radius: 4px;
            padding: 10px;
            margin-top: 20px;
            font-size: 14px;
            color: #cf1322;
        }}
        .close-message {{
            margin-top: 20px;
            font-size: 14px;
            color: #999;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>✗</div>
        <h1>Authentication Failed</h1>
        <p>We encountered an error during sign-in.</p>
        <div class='error-details'>{errorMessage}</div>
        <p class='close-message'>You can close this window and try again.</p>
    </div>
</body>
</html>";
        }
    }

    /// <summary>
    /// Disposes the server resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _cancellationTokenSource?.Dispose();
        _listener?.Close();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
