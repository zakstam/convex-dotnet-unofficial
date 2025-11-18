using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Convex.Client.Extensions.Clerk.Blazor;

/// <summary>
/// Blazor WebAssembly implementation of IClerkTokenService using JavaScript interop.
/// This service automatically injects the required JavaScript code and communicates with the Clerk JavaScript SDK.
/// </summary>
/// <remarks>
/// Initializes a new instance of the BlazorClerkTokenService.
/// </remarks>
public class BlazorClerkTokenService(IJSRuntime jsRuntime, IConfiguration configuration) : IClerkTokenService
{
    private readonly IJSRuntime _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ClerkJsInjector _jsInjector = new ClerkJsInjector(jsRuntime);
    private bool _isLoaded = false;
    private bool _isAuthenticated = false;
    private bool _isInitializing = false;

    /// <summary>
    /// Gets whether the user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Gets whether the authentication state is still loading.
    /// </summary>
    public bool IsLoading => !_isLoaded || _isInitializing;

    /// <summary>
    /// Gets the current authentication token from Clerk.
    /// </summary>
    public async Task<string?> GetTokenAsync(string tokenTemplate = "convex", bool skipCache = false, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure Clerk is initialized
            if (!_isLoaded)
            {
                await InitializeAsync();
            }

            // Verify authentication state before attempting to get token
            if (!_isAuthenticated)
            {
                await UpdateAuthStateAsync();
                if (!_isAuthenticated)
                {
                    System.Diagnostics.Debug.WriteLine("[BlazorClerkTokenService] User not authenticated, cannot get token");
                    return null;
                }
            }

            // Call JavaScript function to get token from Clerk
            var token = await _jsRuntime.InvokeAsync<string>(
                "clerk.getToken",
                cancellationToken,
                tokenTemplate,
                skipCache);

            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine($"[BlazorClerkTokenService] GetTokenAsync returned null or empty for template '{tokenTemplate}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BlazorClerkTokenService] GetTokenAsync succeeded, token length: {token.Length}");
            }

            return token;
        }
        catch (JSException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BlazorClerkTokenService] JSException getting token: {ex.Message}");
            // Clerk might not be initialized or user not authenticated
            if (ex.Message.Contains("not authenticated") || ex.Message.Contains("null") || ex.Message.Contains("not available"))
            {
                return null;
            }
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BlazorClerkTokenService] Exception getting token: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Initializes the Clerk SDK with the publishable key from configuration
    /// and checks authentication state. Automatically injects JavaScript interop code if needed.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isLoaded || _isInitializing)
        {
            return;
        }

        _isInitializing = true;

        try
        {
            // Inject JavaScript interop code first
            await _jsInjector.InjectClerkJsAsync();

            // Get publishable key from configuration
            var publishableKey = _configuration["Clerk:PublishableKey"];

            if (string.IsNullOrEmpty(publishableKey) || publishableKey == "pk_test_YOUR_CLERK_PUBLISHABLE_KEY_HERE")
            {
                throw new InvalidOperationException("Clerk publishable key is not configured. Please set Clerk:PublishableKey in appsettings.json");
            }

            // Load Clerk SDK dynamically with publishable key (prevents auto-init error)
            try
            {
                _ = await _jsRuntime.InvokeAsync<object>("clerk.loadScript", "https://cdn.jsdelivr.net/npm/@clerk/clerk-js@latest/dist/clerk.browser.js", publishableKey);
            }
            catch (JSException ex)
            {
                throw new InvalidOperationException($"Failed to load Clerk SDK: {ex.Message}", ex);
            }

            // Initialize Clerk with publishable key (if not already initialized)
            await _jsRuntime.InvokeVoidAsync("clerk.initialize", publishableKey);

            // Check authentication state
            var isSignedIn = await _jsRuntime.InvokeAsync<bool>("clerk.isSignedIn");
            _isAuthenticated = isSignedIn;
            _isLoaded = true;
        }
        catch (JSException ex)
        {
            // Clerk SDK not loaded or initialization failed
            _isLoaded = false;
            _isAuthenticated = false;
            throw new InvalidOperationException($"Failed to initialize Clerk: {ex.Message}", ex);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    /// <summary>
    /// Updates the authentication state. Call this when Clerk authentication state changes.
    /// </summary>
    public async Task UpdateAuthStateAsync()
    {
        try
        {
            var isSignedIn = await _jsRuntime.InvokeAsync<bool>("clerk.isSignedIn");
            _isAuthenticated = isSignedIn;
            _isLoaded = true;
        }
        catch (JSException)
        {
            _isAuthenticated = false;
            _isLoaded = false;
        }
    }

    /// <summary>
    /// Gets the current user's ID from Clerk.
    /// </summary>
    public async Task<string?> GetUserIdAsync()
    {
        try
        {
            if (!_isLoaded)
            {
                await InitializeAsync();
            }
            return await _jsRuntime.InvokeAsync<string>("clerk.getUserId");
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the current user's email from Clerk.
    /// </summary>
    public async Task<string?> GetUserEmailAsync()
    {
        try
        {
            if (!_isLoaded)
            {
                await InitializeAsync();
            }
            return await _jsRuntime.InvokeAsync<string>("clerk.getUserEmail");
        }
        catch (JSException)
        {
            return null;
        }
    }

    /// <summary>
    /// Opens the Clerk sign-in modal.
    /// </summary>
    public async Task OpenSignInAsync()
    {
        try
        {
            if (!_isLoaded)
            {
                await InitializeAsync();
            }
            await _jsRuntime.InvokeVoidAsync("clerk.openSignIn");
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Failed to open Clerk sign-in: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sets up a listener for Clerk authentication state changes.
    /// </summary>
    public async Task SetupAuthStateListenerAsync<T>(DotNetObjectReference<T> dotNetHelper, string methodName) where T : class
    {
        try
        {
            if (!_isLoaded)
            {
                await InitializeAsync();
            }
            await _jsRuntime.InvokeVoidAsync("clerk.addListener", dotNetHelper, methodName);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Failed to set up Clerk listener: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Signs out the current user from Clerk.
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            if (!_isLoaded)
            {
                await InitializeAsync();
            }
            await _jsRuntime.InvokeVoidAsync("clerk.signOut");
            _isAuthenticated = false;
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Failed to sign out from Clerk: {ex.Message}", ex);
        }
    }
}

