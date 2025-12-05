namespace Convex.BetterAuth;

/// <summary>
/// In-memory session storage. Tokens are lost when the application restarts.
/// Use this for testing or when persistent storage is not needed.
/// </summary>
public class InMemorySessionStorage : ISessionStorage
{
    private string? _token;

    /// <inheritdoc />
    public Task StoreTokenAsync(string token)
    {
        _token = token;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetTokenAsync()
    {
        return Task.FromResult(_token);
    }

    /// <inheritdoc />
    public Task RemoveTokenAsync()
    {
        _token = null;
        return Task.CompletedTask;
    }
}
