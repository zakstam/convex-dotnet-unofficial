using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Generated;
using RealtimeChat.Shared.Models;

namespace RealtimeChat.Shared.Services;

/// <summary>
/// Service for presence and typing indicator operations.
/// </summary>
public class PresenceService : IPresenceService, IDisposable
{
    private readonly IConvexClient _convexClient;
    private readonly SubscriptionTracker _subscriptions = new();
    private readonly string? _updatePresenceFunctionName;
    private readonly string? _setTypingFunctionName;
    private readonly string? _getOnlineUsersFunctionName;
    private readonly string? _getTypingUsersFunctionName;
    private Timer? _presenceTimer;
    private Timer? _typingTimer;
    private DateTime? _lastTypingUpdate;
    private bool _isTyping;
    private string? _currentUsername;

    public event EventHandler? OnlineUsersUpdated;
    public event EventHandler? TypingUsersUpdated;

    public List<OnlineUserDto> OnlineUsers { get; private set; } = [];
    public List<string> TypingUsers { get; private set; } = [];

    public PresenceService(IConvexClient convexClient, string? updatePresenceFunctionName = null, string? setTypingFunctionName = null, string? getOnlineUsersFunctionName = null, string? getTypingUsersFunctionName = null)
    {
        _convexClient = convexClient ?? throw new ArgumentNullException(nameof(convexClient));
        _updatePresenceFunctionName = updatePresenceFunctionName;
        _setTypingFunctionName = setTypingFunctionName;
        _getOnlineUsersFunctionName = getOnlineUsersFunctionName;
        _getTypingUsersFunctionName = getTypingUsersFunctionName;
    }

    public void StartPresenceTracking(string username)
    {
        _currentUsername = username;
        _ = UpdatePresenceAsync(username);

        _presenceTimer = new Timer(async _ =>
        {
            try
            {
                await UpdatePresenceAsync(username);
            }
            catch (Exception ex)
            {
                // Don't crash the app on presence update failures
                Console.Error.WriteLine($"Presence timer error: {ex.Message}");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

    public void StopPresenceTracking()
    {
        _presenceTimer?.Dispose();
        _presenceTimer = null;
        _currentUsername = null;
    }

    public async Task UpdatePresenceAsync(string username)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(_updatePresenceFunctionName))
        {
            return;
        }

        try
        {
            // Username is now obtained from authentication context on the server
            _ = await _convexClient.Mutate<object>(_updatePresenceFunctionName)
                .SkipQueue()
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent crashes (401 errors throw HttpRequestException, not ConvexException)
            Console.Error.WriteLine($"Error updating presence: {ex.Message}");
        }
    }

    public void StartTypingTracking()
    {
        _typingTimer = new Timer(async _ =>
        {
            try
            {
                if (_isTyping && _lastTypingUpdate.HasValue &&
                    (DateTime.Now - _lastTypingUpdate.Value).TotalSeconds > 3)
                {
                    if (_currentUsername != null)
                    {
                        await SetTypingAsync(_currentUsername, false);
                    }
                    _isTyping = false;
                }
            }
            catch (Exception ex)
            {
                // Don't crash the app on typing update failures
                Console.Error.WriteLine($"Typing timer error: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void StopTypingTracking()
    {
        _typingTimer?.Dispose();
        _typingTimer = null;
        _isTyping = false;
        _lastTypingUpdate = null;
    }

    public async Task SetTypingAsync(string username, bool isTyping)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(_setTypingFunctionName))
        {
            return;
        }

        try
        {
            // Username is now obtained from authentication context on the server
            _ = await _convexClient.Mutate<object>(_setTypingFunctionName)
                .WithArgs(new Convex.Generated.SetTypingArgs
                {
                    IsTyping = isTyping
                })
                .SkipQueue()
                .ExecuteAsync();
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent crashes
            Console.Error.WriteLine($"Error setting typing indicator: {ex.Message}");
        }
    }

    public void SubscribeToOnlineUsers(Action<List<OnlineUserDto>> onUpdate, Action<string>? onError = null)
    {
        if (string.IsNullOrEmpty(_getOnlineUsersFunctionName))
        {
            return;
        }

        _ = _subscriptions.Add(_convexClient
            .CreateResilientSubscription<List<OnlineUserDto>>(_getOnlineUsersFunctionName)
            .Subscribe(
                result =>
                {
                    OnlineUsers = result ?? [];
                    onUpdate(OnlineUsers);
                    OnlineUsersUpdated?.Invoke(this, EventArgs.Empty);
                },
                error =>
                {
                    var errorMessage = error is Exception ex ? ex.Message : error?.ToString() ?? "Unknown error";
                    Console.Error.WriteLine($"Online users subscription error: {errorMessage}");
                    onError?.Invoke(errorMessage);
                }
            ));
    }

    public void SubscribeToTypingUsers(string currentUsername, Action<List<string>> onUpdate, Action<string>? onError = null)
    {
        if (string.IsNullOrEmpty(_getTypingUsersFunctionName))
        {
            return;
        }

        _ = _subscriptions.Add(_convexClient
            .CreateResilientSubscription<List<string>>(_getTypingUsersFunctionName, new { excludeUsername = currentUsername })
            .Subscribe(
                result =>
                {
                    TypingUsers = result ?? [];
                    onUpdate(TypingUsers);
                    TypingUsersUpdated?.Invoke(this, EventArgs.Empty);
                },
                error =>
                {
                    var errorMessage = error is Exception ex ? ex.Message : error?.ToString() ?? "Unknown error";
                    Console.Error.WriteLine($"Typing users subscription error: {errorMessage}");
                    onError?.Invoke(errorMessage);
                }
            ));
    }

    public void HandleTypingInput(string messageText)
    {
        if (_currentUsername == null)
        {
            return;
        }

        _lastTypingUpdate = DateTime.Now;

        if (!_isTyping && !string.IsNullOrWhiteSpace(messageText))
        {
            _isTyping = true;
            _ = SetTypingAsync(_currentUsername, true);
        }
        else if (_isTyping && string.IsNullOrWhiteSpace(messageText))
        {
            _isTyping = false;
            _ = SetTypingAsync(_currentUsername, false);
        }
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _presenceTimer?.Dispose();
        _typingTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

