using Convex.BetterAuth;
using Microsoft.JSInterop;

namespace RealtimeChat.Frontend.Services;

/// <summary>
/// Session storage implementation using browser localStorage.
/// This is specific to Blazor WebAssembly applications.
/// </summary>
public class LocalStorageSessionStorage : ISessionStorage
{
    private const string StorageKey = "better_auth_session";
    private readonly IJSRuntime _jsRuntime;

    public LocalStorageSessionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task StoreTokenAsync(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
        }
        catch
        {
            // Ignore storage errors (e.g., during prerendering)
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task RemoveTokenAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch
        {
            // Ignore storage errors
        }
    }
}
