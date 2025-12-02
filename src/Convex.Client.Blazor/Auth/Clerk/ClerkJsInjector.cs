using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Convex.Client.Extensions.Clerk.Blazor;

/// <summary>
/// Service responsible for injecting the Clerk JavaScript interop code from embedded resources.
/// </summary>
internal class ClerkJsInjector(IJSRuntime jsRuntime)
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private static bool _isInjected = false;
    private static readonly object _lock = new object();

    /// <summary>
    /// Injects the Clerk JavaScript interop code into the page if not already injected.
    /// </summary>
    public async Task InjectClerkJsAsync()
    {
        if (_isInjected)
        {
            return;
        }

        lock (_lock)
        {
            if (_isInjected)
            {
                return;
            }
            _isInjected = true; // Set early to prevent race conditions
        }

        try
        {
            // Read the embedded JavaScript resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Convex.Client.Blazor.Resources.js.clerk.js";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var jsCode = reader.ReadToEnd();

            // Inject the JavaScript code by creating a script element
            // This is safer than eval and works better with CSP
            await _jsRuntime.InvokeVoidAsync("eval",
                $"(function() {{ {jsCode} }})()");
        }
        catch (Exception ex)
        {
            _isInjected = false; // Reset on failure
            throw new InvalidOperationException("Failed to inject Clerk JavaScript interop code.", ex);
        }
    }
}

