using Convex.Client;
using Convex.Client.Extensions.ExtensionMethods;
using Convex.Client.Shared.ErrorHandling;
using RealtimeChatClerk.Shared.Models;

namespace RealtimeChatClerk.Shared.Services;

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
    private DateTime? _lastTypingMutationTime;
    private bool _isTyping;
    private string? _currentUsername;
    private const int TYPING_MUTATION_THROTTLE_MS = 1000; // Update typing indicator at most once per second

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
            await UpdatePresenceAsync(username);
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
            _ = await _convexClient.Mutate<object>(_updatePresenceFunctionName)
                .WithArgs(new UpdatePresenceArgs { Username = username })
                .SkipQueue()
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
            Console.Error.WriteLine($"Error updating presence: {ex.Message}");
        }
    }

    public void StartTypingTracking()
    {
        _typingTimer = new Timer(async _ =>
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
        // Username parameter is kept for validation but not sent to Convex
        // The Convex function extracts username from the authenticated user
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(_setTypingFunctionName))
        {
            return;
        }

        try
        {
            _ = await _convexClient.Mutate<object>(_setTypingFunctionName)
                .WithArgs(new SetTypingArgs
                {
                    IsTyping = isTyping
                })
                .SkipQueue()
                .ExecuteAsync();
        }
        catch (ConvexException ex)
        {
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

        // The Convex function gets the current user ID from auth, so no need to pass excludeUsername
        _ = _subscriptions.Add(_convexClient
            .CreateResilientSubscription<List<string>>(_getTypingUsersFunctionName)
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

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            // User is typing - update typing indicator
            // Throttle mutations to avoid calling too frequently
            var now = DateTime.Now;
            if (!_isTyping)
            {
                _isTyping = true;
                _lastTypingMutationTime = now;
                _ = SetTypingAsync(_currentUsername, true);
            }
            else if (!_lastTypingMutationTime.HasValue || 
                     (now - _lastTypingMutationTime.Value).TotalMilliseconds >= TYPING_MUTATION_THROTTLE_MS)
            {
                // Refresh typing indicator periodically (throttled)
                _lastTypingMutationTime = now;
                _ = SetTypingAsync(_currentUsername, true);
            }
        }
        else if (_isTyping)
        {
            // User stopped typing - clear typing indicator immediately
            _isTyping = false;
            _lastTypingMutationTime = null;
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

