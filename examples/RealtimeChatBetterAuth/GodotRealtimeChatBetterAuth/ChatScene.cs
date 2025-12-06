using Godot;
using Convex.Client.Infrastructure.Connection;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Generated;
using RealtimeChat.Shared.Models;
using RealtimeChat.Shared.Services;
using Convex.BetterAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GodotRealtimeChat;

/// <summary>
/// Enhanced chat scene with modern UI, animations, and rich features.
/// </summary>
public partial class ChatScene : Control
{
    #region Theme Colors
    private static readonly Color BackgroundDark = new(0.11f, 0.12f, 0.13f);
    private static readonly Color SurfaceDark = new(0.15f, 0.16f, 0.17f);
    private static readonly Color PrimaryColor = new(0.4f, 0.5f, 0.92f);
    private static readonly Color SecondaryColor = new(0.58f, 0.4f, 0.92f);
    private static readonly Color SuccessColor = new(0.3f, 0.8f, 0.5f);
    private static readonly Color ErrorColor = new(0.9f, 0.3f, 0.3f);
    private static readonly Color TextPrimary = new(0.95f, 0.95f, 0.95f);
    private static readonly Color TextSecondary = new(0.7f, 0.7f, 0.7f);
    #endregion

    #region UI References
    private VBoxContainer? _messageContainer;
    private ScrollContainer? _scrollContainer;
    private LineEdit? _messageInput;
    private Button? _sendButton;
    private Label? _statusLabel;
    private Label? _userLabel;
    private OptionButton? _usernameDropdown;
    private Button? _connectButton;
    private Button? _authButton;
    private BetterAuthDialog? _authDialog;
    private PanelContainer? _typingIndicator;
    private PopupMenu? _contextMenu;
    private PopupPanel? _emojiPicker;
    private Button? _emojiButton;
    private VBoxContainer? _skeletonLoader;

    // Tween references for skeleton loader animations (need to be stopped when hidden)
    private List<Tween> _skeletonTweens = new();
    private PopupPanel? _searchPanel;
    private LineEdit? _searchInput;
    private VBoxContainer? _searchResults;
    private PanelContainer? _loadingMoreIndicator;
    private Button? _loadMoreButton;

    // Toast scene for notifications
    private PackedScene? _toastScene;
    #endregion

    #region State
    private ChatService? _chatService;
    private Dictionary<string, Color> _userColors = new();
    private MessageDto? _selectedMessage;
    private bool _isLoadingOlderMessages = false;
    private float _scrollPositionBeforeLoad = 0f;
    private int _previousMessageCount = 0; // Track message count before loading older messages
    private bool _shouldAddSeparator = false; // Flag to indicate we should add a separator on next display
    private readonly string[] _emojiList = new[]
    {
        "😀", "😃", "😄", "😁", "😆", "😅", "🤣", "😂",
        "😊", "😇", "🙂", "🙃", "😉", "😌", "😍", "🥰",
        "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍",
        "👍", "👎", "👏", "🙌", "👊", "✊", "🤝", "🙏",
        "🔥", "✨", "💫", "⭐", "🌟", "💥", "💯", "✅"
    };
    #endregion

    public override void _Ready()
    {
        GD.Print($"[ChatScene] ===== _Ready() STARTED ===== at {Time.GetTimeStringFromSystem()}");
        GD.Print($"[ChatScene] Enhanced UI initialization started at {Time.GetTimeStringFromSystem()}");

        // Initialize chat service with generated function names and configurable initial message limit
        var client = ConvexManager.Instance.Client;
        var config = ConvexManager.Instance.ChatConfig;
        _chatService = new ChatService(client, ConvexFunctions.Queries.GetMessages, ConvexFunctions.Mutations.SendMessage, config.InitialMessageLimit);
        GD.Print($"[ChatScene] ChatService initialized");

        // Wire up service events FIRST - before any async operations
        GD.Print($"[ChatScene] Subscribing to service events...");
        _chatService.MessagesUpdated += OnMessagesUpdated;
        _chatService.LoadingStateChanged += OnLoadingStateChanged;
        _chatService.LoadingMoreStateChanged += OnLoadingMoreStateChanged;
        _chatService.ErrorOccurred += OnChatServiceError;
        GD.Print($"[ChatScene] Service events subscribed");

        // Find UI nodes
        _messageContainer = GetNode<VBoxContainer>("%MessageContainer");
        _scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
        _messageInput = GetNode<LineEdit>("%MessageInput");
        _sendButton = GetNode<Button>("%SendButton");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _userLabel = GetNode<Label>("%UserLabel");
        _usernameDropdown = GetNodeOrNull<OptionButton>("%UsernameDropdown");
        _connectButton = GetNode<Button>("%ConnectButton");
        _authButton = GetNodeOrNull<Button>("%AuthButton");

        // Get popup UI elements
        _contextMenu = GetNode<PopupMenu>("%ContextMenu");
        _emojiPicker = GetNode<PopupPanel>("%EmojiPickerPanel");
        _searchPanel = GetNode<PopupPanel>("%SearchPanel");

        // Get loading/state UI elements (scene instances from MessageContainer)
        _typingIndicator = GetNode<PanelContainer>("VBoxContainer/ScrollContainer/MessageContainer/TypingIndicator");
        _skeletonLoader = GetNode<VBoxContainer>("VBoxContainer/ScrollContainer/MessageContainer/SkeletonLoader");

        // Get child nodes from scene instances (must access through parent)
        _searchInput = _searchPanel.GetNode<LineEdit>("%SearchInput");
        _searchResults = _searchPanel.GetNode<VBoxContainer>("%SearchResults");

        // Load Toast scene for notifications
        _toastScene = GD.Load<PackedScene>("res://Toast.tscn");

        // Apply modern theme
        ApplyModernTheme();

        // Setup typing indicator styling and animation
        SetupTypingIndicator();

        // Setup context menu items
        SetupContextMenu();

        // Populate emoji picker
        PopulateEmojiPicker();

        // Setup skeleton loader styling
        SetupSkeletonLoader();

        // Setup search panel styling
        SetupSearchPanel();

        // Setup loading more indicator
        SetupLoadingMoreIndicator();

        // Setup load more button
        SetupLoadMoreButton();

        // Setup UI events
        _sendButton.Pressed += OnSendMessagePressed;
        _messageInput.TextSubmitted += _ => OnSendMessagePressed();
        _connectButton.Pressed += OnConnectButtonPressed;

        // If Better Auth is configured, hide username dropdown and show auth button
        var hasBetterAuth = ConvexManager.Instance.BetterAuthService != null ||
                           ConvexManager.Instance.ChatConfig?.HasBetterAuth == true;

        if (hasBetterAuth)
        {
            GD.Print("[ChatScene] Better Auth configuration detected, setting up auth UI");
            if (_usernameDropdown != null)
            {
                _usernameDropdown.Visible = false;
            }
            if (_authButton != null)
            {
                _authButton.Visible = true;
                _authButton.Pressed += OnAuthButtonPressed;
            }

            // Authentication check will happen when Better Auth is ready (see OnBetterAuthReady)
        }
        else
        {
            GD.Print("[ChatScene] No Better Auth configuration, using username dropdown");
            // Use username dropdown if Better Auth not configured
            if (_usernameDropdown != null)
            {
                _usernameDropdown.Visible = true;
                _usernameDropdown.ItemSelected += idx => OnUsernameSelected((int)idx);
            }
            if (_authButton != null)
            {
                _authButton.Visible = false;
            }
        }

        // Subscribe to connection state changes
        ConvexManager.Instance.ConnectionStateChanged += OnConnectionStateChanged;

        GD.Print($"[ChatScene] About to check Better Auth - ConvexManager ready: {ConvexManager.Instance != null}");
        var instance = ConvexManager.Instance;
        GD.Print($"[ChatScene] ConvexManager initialized - Client: {instance?.Client != null}, ChatConfig: {instance?.ChatConfig != null}");

        // Check if Better Auth is configured
        var chatConfig = instance?.ChatConfig;
        var hasBetterAuthService = instance?.BetterAuthService != null;
        var hasBetterAuthConfig = chatConfig?.HasBetterAuth == true;
        var hasBetterAuthSetup = hasBetterAuthService || hasBetterAuthConfig;

        GD.Print($"[ChatScene] Better Auth check - Service: {hasBetterAuthService}, Config: {hasBetterAuthConfig}");

        if (hasBetterAuthSetup)
        {
            GD.Print("[ChatScene] Better Auth configuration detected - BLOCKING message loading until authenticated");

            // DO NOT load messages yet - wait for authentication
            // Subscribe to Better Auth ready signal
            if (instance?.BetterAuthService != null)
            {
                GD.Print("[ChatScene] BetterAuthService already exists, subscribing to BetterAuthReady signal");
                _ = ConvexManager.Instance?.Connect("BetterAuthReady", new Callable(this, nameof(OnBetterAuthReady)));
            }
            else
            {
                GD.Print("[ChatScene] BetterAuthService not yet created, will wait for BetterAuthReady signal");
                // Connect to signal even if BetterAuthService isn't ready yet
                _ = ConvexManager.Instance?.Connect("BetterAuthReady", new Callable(this, nameof(OnBetterAuthReady)));
            }

            // Check auth immediately AND use a timer as backup
            GD.Print("[ChatScene] Checking auth immediately...");
            _ = CallDeferred(nameof(CheckBetterAuth));

            // Also use a timer as backup
            var timer = new Timer();
            timer.WaitTime = 0.5f; // Wait 500ms for Better Auth to initialize
            timer.OneShot = true;
            timer.Timeout += () =>
            {
                GD.Print("[ChatScene] Timer expired, checking Better Auth again");
                CheckBetterAuth();
                timer.QueueFree();
            };
            AddChild(timer);
            timer.Start();
            GD.Print("[ChatScene] Started backup timer to check Better Auth in 0.5 seconds");
        }
        else
        {
            GD.Print("[ChatScene] No Better Auth configured - BUT backend requires auth, so we'll show dialog on error");
            // Even if Better Auth isn't configured in frontend, backend requires auth
            // So we'll show auth dialog when we get authentication errors
            // Load messages anyway - error handler will catch auth failures
            _ = _chatService.LoadInitialMessagesAsync();

            // Subscribe to real-time message updates
            _chatService.SubscribeToMessages();

            // Also set up a timer to check for auth errors after a short delay
            // This is a fallback in case the error handler doesn't fire
            var errorCheckTimer = new Timer();
            errorCheckTimer.WaitTime = 2.0f; // Wait 2 seconds
            errorCheckTimer.OneShot = true;
            errorCheckTimer.Timeout += () =>
            {
                GD.Print("[ChatScene] Error check timer fired - checking if we got auth errors");
                // If we still have no messages and no error was shown, show auth dialog
                if (_chatService.CurrentMessages.Count == 0 && _authDialog == null)
                {
                    GD.Print("[ChatScene] No messages loaded and no auth dialog shown - showing auth dialog as fallback");
                    ShowAuthDialog();
                }
                errorCheckTimer.QueueFree();
            };
            AddChild(errorCheckTimer);
            errorCheckTimer.Start();
        }

        GD.Print("[ChatScene] Enhanced UI initialization completed");
        GD.Print($"[ChatScene] ===== _Ready() COMPLETED ===== ");
    }

    private void OnBetterAuthReady()
    {
        GD.Print("[ChatScene] Better Auth ready, checking authentication");
        CheckBetterAuth();
    }

    private void CheckBetterAuth()
    {
        GD.Print("[ChatScene] CheckBetterAuth called");

        // Check if Better Auth is configured in the first place
        var config = ConvexManager.Instance.ChatConfig;
        var hasBetterAuthConfig = config?.HasBetterAuth == true;

        if (!hasBetterAuthConfig)
        {
            GD.Print("[ChatScene] No Better Auth configuration found, skipping auth check");
            return;
        }

        var authService = ConvexManager.Instance.BetterAuthService;
        if (authService == null)
        {
            GD.Print("[ChatScene] BetterAuthService is null, but Better Auth is configured - showing auth dialog immediately");
            // Show auth dialog immediately - don't wait
            ShowAuthDialog();
            return;
        }

        GD.Print($"[ChatScene] IsAuthenticated: {authService.IsAuthenticated}");
        if (!authService.IsAuthenticated)
        {
            GD.Print("[ChatScene] User not authenticated, showing auth dialog");
            // Show auth dialog immediately
            ShowAuthDialog();
        }
        else
        {
            GD.Print("[ChatScene] User already authenticated");
            UpdateUserLabel();
            // Load and display messages
            _ = _chatService!.LoadInitialMessagesAsync();
            // Subscribe to real-time message updates
            _chatService.SubscribeToMessages();
        }
    }

    #region Keyboard Shortcuts
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            // Only handle shortcuts when input field has focus or specific keys
            var inputHasFocus = _messageInput?.HasFocus() ?? false;

            // Enter: Send message (only if input has focus)
            if (keyEvent.Keycode == Key.Enter && inputHasFocus)
            {
                if (keyEvent.ShiftPressed)
                {
                    // Shift+Enter: Insert new line (default behavior)
                    return;
                }
                else
                {
                    // Enter: Send message
                    OnSendMessagePressed();
                    GetViewport().SetInputAsHandled();
                }
            }
            // Escape: Clear input or close popups
            else if (keyEvent.Keycode == Key.Escape)
            {
                if (_emojiPicker?.Visible ?? false)
                {
                    _emojiPicker.Hide();
                }
                else if (inputHasFocus)
                {
                    _messageInput!.Text = "";
                    _messageInput.ReleaseFocus();
                }
                GetViewport().SetInputAsHandled();
            }
            // Ctrl+E: Toggle emoji picker
            else if (keyEvent.Keycode == Key.E && keyEvent.CtrlPressed)
            {
                ToggleEmojiPicker();
                GetViewport().SetInputAsHandled();
            }
            // Ctrl+F: Toggle search panel
            else if (keyEvent.Keycode == Key.F && keyEvent.CtrlPressed)
            {
                ToggleSearchPanel();
                GetViewport().SetInputAsHandled();
            }
        }
    }
    #endregion

    #region Theme Application
    private void ApplyModernTheme()
    {
        GD.Print("[ChatScene] Applying modern theme...");

        // Style message input
        var inputStyle = new StyleBoxFlat();
        inputStyle.BgColor = SurfaceDark;
        inputStyle.CornerRadiusTopLeft = 8;
        inputStyle.CornerRadiusTopRight = 8;
        inputStyle.CornerRadiusBottomLeft = 8;
        inputStyle.CornerRadiusBottomRight = 8;
        inputStyle.ContentMarginLeft = 12;
        inputStyle.ContentMarginRight = 12;
        inputStyle.ContentMarginTop = 8;
        inputStyle.ContentMarginBottom = 8;
        _messageInput!.AddThemeStyleboxOverride("normal", inputStyle);

        // Focus style
        var focusStyle = new StyleBoxFlat();
        focusStyle.BgColor = SurfaceDark;
        focusStyle.BorderColor = PrimaryColor;
        focusStyle.BorderWidthLeft = 2;
        focusStyle.BorderWidthTop = 2;
        focusStyle.BorderWidthRight = 2;
        focusStyle.BorderWidthBottom = 2;
        focusStyle.CornerRadiusTopLeft = 8;
        focusStyle.CornerRadiusTopRight = 8;
        focusStyle.CornerRadiusBottomLeft = 8;
        focusStyle.CornerRadiusBottomRight = 8;
        _messageInput.AddThemeStyleboxOverride("focus", focusStyle);

        // Style send button
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = PrimaryColor;
        buttonStyle.CornerRadiusTopLeft = 8;
        buttonStyle.CornerRadiusTopRight = 8;
        buttonStyle.CornerRadiusBottomLeft = 8;
        buttonStyle.CornerRadiusBottomRight = 8;
        buttonStyle.ContentMarginLeft = 16;
        buttonStyle.ContentMarginRight = 16;
        buttonStyle.ContentMarginTop = 8;
        buttonStyle.ContentMarginBottom = 8;
        _sendButton!.AddThemeStyleboxOverride("normal", buttonStyle);

        // Button hover
        var buttonHoverStyle = new StyleBoxFlat();
        buttonHoverStyle.BgColor = PrimaryColor.Lightened(0.1f);
        buttonHoverStyle.CornerRadiusTopLeft = 8;
        buttonHoverStyle.CornerRadiusTopRight = 8;
        buttonHoverStyle.CornerRadiusBottomLeft = 8;
        buttonHoverStyle.CornerRadiusBottomRight = 8;
        _sendButton.AddThemeStyleboxOverride("hover", buttonHoverStyle);

        // Style reconnect button
        var reconnectStyle = new StyleBoxFlat();
        reconnectStyle.BgColor = SecondaryColor;
        reconnectStyle.CornerRadiusTopLeft = 6;
        reconnectStyle.CornerRadiusTopRight = 6;
        reconnectStyle.CornerRadiusBottomLeft = 6;
        reconnectStyle.CornerRadiusBottomRight = 6;
        reconnectStyle.ContentMarginLeft = 12;
        reconnectStyle.ContentMarginRight = 12;
        reconnectStyle.ContentMarginTop = 6;
        reconnectStyle.ContentMarginBottom = 6;
        _connectButton!.AddThemeStyleboxOverride("normal", reconnectStyle);

        GD.Print("[ChatScene] Modern theme applied successfully");
    }

    private void AnimateInputFocus(bool focused)
    {
        var tween = CreateTween();
        if (focused)
        {
            tween.TweenProperty(_messageInput, "scale", new Vector2(1.02f, 1.02f), 0.1);
        }
        else
        {
            tween.TweenProperty(_messageInput, "scale", Vector2.One, 0.1);
        }
    }
    #endregion

    #region User Avatars
    private Control CreateAvatarCircle(string username, int size = 40)
    {
        var avatar = new PanelContainer();
        avatar.CustomMinimumSize = new Vector2(size, size);
        avatar.SizeFlagsVertical = SizeFlags.ShrinkCenter;

        // Rounded circle style
        var style = new StyleBoxFlat();
        style.BgColor = GetUserColor(username);
        style.CornerRadiusTopLeft = size / 2;
        style.CornerRadiusTopRight = size / 2;
        style.CornerRadiusBottomLeft = size / 2;
        style.CornerRadiusBottomRight = size / 2;
        style.ShadowColor = new Color(0, 0, 0, 0.3f);
        style.ShadowSize = 2;
        avatar.AddThemeStyleboxOverride("panel", style);

        // Initial letter
        var label = new Label();
        label.Text = username.Length > 0 ? username[0].ToString().ToUpper() : "?";
        label.AddThemeFontSizeOverride("font_size", size / 2);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        avatar.AddChild(label);

        return avatar;
    }

    private Color GetUserColor(string username)
    {
        if (_userColors.TryGetValue(username, out var existingColor))
        {
            return existingColor;
        }

        // Generate deterministic color based on username hash
        var hash = username.GetHashCode();
        var hue = (float)(Math.Abs(hash) % 360) / 360.0f;
        var newColor = Color.FromHsv(hue, 0.6f, 0.85f);
        _userColors[username] = newColor;
        return newColor;
    }
    #endregion

    #region Message Display
    #region Service Event Handlers
    /// <summary>
    /// Called when messages are updated from the service.
    /// Note: This callback is already on the UI thread thanks to WithUIThreadMarshalling() in ChatService.
    /// </summary>
    private void OnMessagesUpdated(List<MessageDto> messages)
    {
        GD.Print($"[ChatScene] OnMessagesUpdated - Received {messages.Count} messages");
        // Callback is already on UI thread, no need for CallDeferred
        DisplayMessages(messages);
        UpdateLoadMoreButton();
    }

    /// <summary>
    /// Called when loading state changes.
    /// Note: This callback is already on the UI thread thanks to WithUIThreadMarshalling() in ChatService.
    /// </summary>
    private void OnLoadingStateChanged(bool isLoading)
    {
        ShowSkeletonLoader(isLoading);
        if (isLoading)
        {
            UpdateStatusLabel("Loading messages...");
        }
    }

    /// <summary>
    /// Called when loading more (older messages) state changes.
    /// Note: This callback is already on the UI thread thanks to WithUIThreadMarshalling() in ChatService.
    /// </summary>
    private void OnLoadingMoreStateChanged(bool isLoading)
    {
        _isLoadingOlderMessages = isLoading;

        // Capture current message count when starting to load older messages
        if (isLoading && _chatService != null)
        {
            _previousMessageCount = _chatService.CurrentMessages.Count;
            GD.Print($"[ChatScene] Captured previous message count: {_previousMessageCount}");
        }

        ShowLoadingMoreIndicator(isLoading);
        UpdateLoadMoreButton();
    }

    #endregion

    private void DisplayMessages(List<MessageDto>? messages)
    {
        if (messages == null)
        {
            return;
        }

        GD.Print($"[ChatScene] DisplayMessages() - Displaying {messages.Count} messages");
        GD.Print($"[ChatScene] DisplayMessages - _shouldAddSeparator: {_shouldAddSeparator}, _previousMessageCount: {_previousMessageCount}, _isLoadingOlderMessages: {_isLoadingOlderMessages}");

        // If loading older messages, preserve scroll position and track previous count
        var preserveScrollPosition = _isLoadingOlderMessages || _shouldAddSeparator;
        var previousContentHeight = 0f;
        var previousScrollValue = 0f;
        var previousCount = _previousMessageCount;
        var shouldAddSeparator = _shouldAddSeparator;

        if (preserveScrollPosition)
        {
            // Store current content height and scroll position
            previousContentHeight = _messageContainer!.Size.Y;
            var scrollBar = _scrollContainer!.GetVScrollBar();
            previousScrollValue = (float)scrollBar.Value;
            // Previous count is already set from last display
        }
        else
        {
            // On initial load, reset the previous count
            _previousMessageCount = 0;
            _shouldAddSeparator = false;
        }

        // Clear existing message nodes (except loading indicator and load more button)
        foreach (var child in _messageContainer!.GetChildren())
        {
            if (child != _loadingMoreIndicator && child != _loadMoreButton)
            {
                child.QueueFree();
            }
        }

        var sortedMessages = messages.OrderBy(m => m.Timestamp).ToList();

        // Add loading indicator at the top if it exists (after load more button)
        if (_loadingMoreIndicator != null && _loadingMoreIndicator.GetParent() == null)
        {
            _messageContainer.AddChild(_loadingMoreIndicator);
            // Place after load more button if it exists
            if (_loadMoreButton != null && _loadMoreButton.GetParent() == _messageContainer)
            {
                var buttonIndex = _messageContainer.GetChildCount() - 1;
                _messageContainer.MoveChild(_loadingMoreIndicator, buttonIndex + 1);
            }
            else
            {
                _messageContainer.MoveChild(_loadingMoreIndicator, 0);
            }
        }

        // Get page boundaries from ChatService
        var pageBoundaries = _chatService?.PageBoundaries ?? [];

        // Add messages with grouping and pagination separators
        MessageDto? previousMessage = null;
        var messageIndex = 0;

        GD.Print($"[ChatScene] DisplayMessages - preserveScrollPosition: {preserveScrollPosition}, previousCount: {previousCount}, totalMessages: {sortedMessages.Count}, boundaries: [{string.Join(", ", pageBoundaries)}]");

        foreach (var message in sortedMessages)
        {
            // Add separator BEFORE the message at a pagination boundary
            // Boundaries mark where different pagination batches meet
            // Skip separator at index 0 (first message shouldn't have a separator before it)
            if (messageIndex > 0 && pageBoundaries.Contains(messageIndex))
            {
                GD.Print($"[ChatScene] Adding pagination separator BEFORE message at index {messageIndex} (message: '{message.Text.Substring(0, Math.Min(20, message.Text.Length))}...', boundary: {messageIndex}, total: {sortedMessages.Count})");
                var separator = CreatePaginationSeparator();
                _messageContainer.AddChild(separator);
            }

            var shouldGroup = ShouldGroupWithPrevious(message, previousMessage);
            var messageNode = shouldGroup
                ? CreateCompactMessageNode(message)
                : CreateFullMessageNode(message);

            _messageContainer.AddChild(messageNode);

            // Animate entrance (skip animation when loading older messages for smoother UX)
            if (!preserveScrollPosition)
            {
                AnimateMessageEntrance(messageNode);
            }

            previousMessage = message;
            messageIndex++;
        }

        // Update previous message count for next pagination load
        if (shouldAddSeparator)
        {
            // After adding separator, update count and clear flag
            _previousMessageCount = sortedMessages.Count;
            _shouldAddSeparator = false;
            GD.Print($"[ChatScene] Updated previous message count to {_previousMessageCount}, cleared separator flag");
        }
        else if (!preserveScrollPosition)
        {
            // Reset on initial load
            _previousMessageCount = sortedMessages.Count;
        }

        // Handle scroll position based on whether we're loading older messages
        if (preserveScrollPosition)
        {
            // When loading older messages, scroll to top to show the newly loaded older messages
            CallDeferred(nameof(ScrollToTopAfterLoad), previousContentHeight, previousScrollValue);
        }
        else
        {
            // Auto-scroll to bottom with animation for initial load or new messages
            ScrollToBottomSmooth();
        }
    }

    private async void ScrollToTopAfterLoad(float previousContentHeight, float previousScrollValue)
    {
        if (_scrollContainer == null || _messageContainer == null) return;

        // Wait a frame for layout to complete
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var scrollBar = _scrollContainer.GetVScrollBar();

        // Scroll to the top to show the newly loaded older messages
        // Smooth scroll to top (value = 0)
        var tween = CreateTween();
        tween.TweenProperty(scrollBar, "value", 0.0, 0.3)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    private Control CreatePaginationSeparator()
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", 8);
        container.CustomMinimumSize = new Vector2(0, 24);
        container.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Left line
        var leftLine = new PanelContainer();
        leftLine.CustomMinimumSize = new Vector2(0, 1);
        leftLine.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var leftLineStyle = new StyleBoxFlat();
        leftLineStyle.BgColor = TextSecondary;
        leftLineStyle.BgColor = leftLineStyle.BgColor with { A = 0.3f };
        leftLine.AddThemeStyleboxOverride("panel", leftLineStyle);
        container.AddChild(leftLine);

        // Date/time label (optional - could show date if messages span days)
        var label = new Label();
        label.Text = "Older Messages";
        label.AddThemeColorOverride("font_color", TextSecondary);
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeConstantOverride("autowrap_mode", (int)TextServer.AutowrapMode.Off);
        container.AddChild(label);

        // Right line
        var rightLine = new PanelContainer();
        rightLine.CustomMinimumSize = new Vector2(0, 1);
        rightLine.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var rightLineStyle = new StyleBoxFlat();
        rightLineStyle.BgColor = TextSecondary;
        rightLineStyle.BgColor = rightLineStyle.BgColor with { A = 0.3f };
        rightLine.AddThemeStyleboxOverride("panel", rightLineStyle);
        container.AddChild(rightLine);

        return container;
    }

    private bool ShouldGroupWithPrevious(MessageDto current, MessageDto? previous)
    {
        if (previous == null)
        {
            return false;
        }

        // Group if same user and within 2 minutes
        var timeDiff = current.Timestamp - previous.Timestamp;
        return previous.Username == current.Username && timeDiff < 120000;
    }

    private Control CreateFullMessageNode(MessageDto message)
    {
        var container = new VBoxContainer();
        container.AddThemeConstantOverride("separation", 4);

        var messageRow = new HBoxContainer();
        messageRow.AddThemeConstantOverride("separation", 12);

        // Avatar
        var avatar = CreateAvatarCircle(message.Username);
        messageRow.AddChild(avatar);

        // Message content
        var contentBox = new VBoxContainer();
        contentBox.AddThemeConstantOverride("separation", 4);
        contentBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Header (username + timestamp)
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);

        var username = new Label();
        username.Text = message.Username;
        username.AddThemeFontSizeOverride("font_size", 14);
        username.AddThemeColorOverride("font_color", GetUserColor(message.Username));
        header.AddChild(username);

        var timestamp = new Label();
        timestamp.Text = message.GetFormattedTime();
        timestamp.AddThemeFontSizeOverride("font_size", 11);
        timestamp.AddThemeColorOverride("font_color", TextSecondary);
        header.AddChild(timestamp);

        contentBox.AddChild(header);

        // Message bubble
        var bubble = CreateMessageBubble(message);
        contentBox.AddChild(bubble);

        messageRow.AddChild(contentBox);
        container.AddChild(messageRow);

        return container;
    }

    private Control CreateCompactMessageNode(MessageDto message)
    {
        var container = new HBoxContainer();
        container.AddThemeConstantOverride("separation", 12);

        // Spacer (same width as avatar)
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(40, 0);
        container.AddChild(spacer);

        // Message bubble only
        var bubble = CreateMessageBubble(message);
        bubble.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.AddChild(bubble);

        return container;
    }

    private Control CreateMessageBubble(MessageDto message)
    {
        var bubble = new PanelContainer();

        // Bubble style
        var style = new StyleBoxFlat();
        style.BgColor = SurfaceDark;
        style.CornerRadiusTopLeft = 12;
        style.CornerRadiusTopRight = 12;
        style.CornerRadiusBottomRight = 12;
        style.CornerRadiusBottomLeft = 4;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        style.ShadowColor = new Color(0, 0, 0, 0.2f);
        style.ShadowSize = 2;
        bubble.AddThemeStyleboxOverride("panel", style);

        var contentBox = new VBoxContainer();

        // Message text with rich formatting
        var textLabel = new RichTextLabel();
        textLabel.BbcodeEnabled = true;
        textLabel.Text = FormatRichText(message.Text);
        textLabel.CustomMinimumSize = new Vector2(200, 0);
        textLabel.FitContent = true;
        textLabel.ScrollActive = false;
        textLabel.AddThemeColorOverride("default_color", TextPrimary);
        contentBox.AddChild(textLabel);

        // Attachments
        if (message.Attachments != null && message.Attachments.Count > 0)
        {
            var attachmentLabel = new Label();
            attachmentLabel.Text = $"📎 {message.Attachments.Count} attachment(s)";
            attachmentLabel.AddThemeColorOverride("font_color", SuccessColor);
            contentBox.AddChild(attachmentLabel);
        }

        bubble.AddChild(contentBox);

        // Enable right-click context menu
        bubble.GuiInput += (inputEvent) => OnMessageBubbleInput(inputEvent, message);

        return bubble;
    }

    private void OnMessageBubbleInput(InputEvent @event, MessageDto message)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                ShowMessageContextMenu(message, mouseButton.GlobalPosition);
            }
        }
    }

    private void AnimateMessageEntrance(Control messageNode)
    {
        // Use modulate for fade-in effect only
        // Don't use position as it breaks VBoxContainer layout
        messageNode.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(messageNode, "modulate:a", 1.0, 0.3)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
    #endregion

    #region Smooth Scrolling
    private void ScrollToBottomSmooth()
    {
        var scrollBar = _scrollContainer!.GetVScrollBar();
        var target = scrollBar.MaxValue;

        var tween = CreateTween();
        tween.TweenProperty(scrollBar, "value", target, 0.3).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }
    #endregion

    #region Typing Indicator
    private void SetupTypingIndicator()
    {
        // Apply styling to typing indicator panel
        var style = new StyleBoxFlat();
        style.BgColor = SurfaceDark;
        style.CornerRadiusTopLeft = 12;
        style.CornerRadiusTopRight = 12;
        style.CornerRadiusBottomRight = 12;
        style.CornerRadiusBottomLeft = 12;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        _typingIndicator!.AddThemeStyleboxOverride("panel", style);

        // Get references to child nodes (must access through _typingIndicator instance)
        var typingLabel = _typingIndicator!.GetNode<Label>("%TypingLabel");
        var dot1 = _typingIndicator.GetNode<Label>("%Dot1");
        var dot2 = _typingIndicator.GetNode<Label>("%Dot2");
        var dot3 = _typingIndicator.GetNode<Label>("%Dot3");

        // Apply styling to label and dots
        typingLabel.AddThemeColorOverride("font_color", TextSecondary);
        dot1.AddThemeColorOverride("font_color", TextSecondary);
        dot2.AddThemeColorOverride("font_color", TextSecondary);
        dot3.AddThemeColorOverride("font_color", TextSecondary);

        // Animate dots
        AnimateTypingDot(dot1, 0.0f);
        AnimateTypingDot(dot2, 0.2f);
        AnimateTypingDot(dot3, 0.4f);
    }

    private void AnimateTypingDot(Label dot, float delay)
    {
        // Use a large number of loops instead of infinite to avoid Godot's infinite loop detection
        // 1000 loops should be more than enough for any practical use case
        var tween = CreateTween().SetLoops(1000);
        tween.TweenInterval(delay);
        tween.TweenProperty(dot, "modulate:a", 0.3, 0.5);
        tween.TweenProperty(dot, "modulate:a", 1.0, 0.5);
        // Typing indicator tweens don't need to be tracked - they run continuously
    }
    #endregion

    #region Loading States
    private void SetupSkeletonLoader()
    {
        // Apply styling to skeleton loader container
        _skeletonLoader!.AddThemeConstantOverride("separation", 12);

        // Setup each skeleton item (1, 2, 3)
        for (var i = 1; i <= 3; i++)
        {
            SetupSkeletonItem(i);
        }
    }

    private void SetupSkeletonItem(int index)
    {
        // Get references to skeleton components
        var skeleton = _skeletonLoader!.GetNode<HBoxContainer>($"Skeleton{index}");
        var avatarPlaceholder = skeleton.GetNode<Panel>($"AvatarPlaceholder{index}");
        var textContainer = skeleton.GetNode<VBoxContainer>($"TextContainer{index}");
        var usernamePlaceholder = textContainer.GetNode<Panel>($"UsernamePlaceholder{index}");
        var messagePlaceholder = textContainer.GetNode<Panel>($"MessagePlaceholder{index}");

        // Apply styling to skeleton components
        skeleton.AddThemeConstantOverride("separation", 12);
        textContainer.AddThemeConstantOverride("separation", 6);

        // Avatar placeholder styling
        var avatarStyle = new StyleBoxFlat();
        avatarStyle.BgColor = SurfaceDark.Darkened(0.1f);
        avatarStyle.CornerRadiusTopLeft = 20;
        avatarStyle.CornerRadiusTopRight = 20;
        avatarStyle.CornerRadiusBottomLeft = 20;
        avatarStyle.CornerRadiusBottomRight = 20;
        avatarPlaceholder.AddThemeStyleboxOverride("panel", avatarStyle);

        // Username placeholder styling
        var usernameStyle = new StyleBoxFlat();
        usernameStyle.BgColor = SurfaceDark.Darkened(0.1f);
        usernameStyle.CornerRadiusTopLeft = 4;
        usernameStyle.CornerRadiusTopRight = 4;
        usernameStyle.CornerRadiusBottomLeft = 4;
        usernameStyle.CornerRadiusBottomRight = 4;
        usernamePlaceholder.AddThemeStyleboxOverride("panel", usernameStyle);

        // Message placeholder styling
        var messageStyle = new StyleBoxFlat();
        messageStyle.BgColor = SurfaceDark.Darkened(0.1f);
        messageStyle.CornerRadiusTopLeft = 4;
        messageStyle.CornerRadiusTopRight = 4;
        messageStyle.CornerRadiusBottomLeft = 4;
        messageStyle.CornerRadiusBottomRight = 4;
        messagePlaceholder.AddThemeStyleboxOverride("panel", messageStyle);

        // Animate shimmer effect
        AnimateShimmer(skeleton);
    }

    private void AnimateShimmer(Control node)
    {
        // Use a large number of loops instead of infinite to avoid Godot's infinite loop detection
        // 1000 loops should be more than enough for any practical use case
        var tween = CreateTween().SetLoops(1000);
        tween.TweenProperty(node, "modulate:a", 0.5, 1.0);
        tween.TweenProperty(node, "modulate:a", 1.0, 1.0);
        _skeletonTweens.Add(tween);
    }

    private void ShowSkeletonLoader(bool show)
    {
        if (_skeletonLoader != null && GodotObject.IsInstanceValid(_skeletonLoader))
        {
            _skeletonLoader.Visible = show;

            // Stop shimmer animations when hidden to save resources
            if (!show)
            {
                // Kill tweens associated with skeleton items
                foreach (var tween in _skeletonTweens.ToList())
                {
                    if (tween != null && GodotObject.IsInstanceValid(tween))
                    {
                        tween.Kill();
                    }
                }
                _skeletonTweens.Clear();
            }
        }
    }
    #endregion

    #region Message Context Menu
    private void SetupContextMenu()
    {
        _contextMenu!.AddItem("Copy Text", 0);
        _contextMenu.AddSeparator();
        _contextMenu.AddItem("Reply", 1);
        _contextMenu.AddSeparator();
        _contextMenu.AddItem("Delete (Own Only)", 2);

        _contextMenu.IdPressed += OnContextMenuItemSelected;
    }

    private void OnContextMenuItemSelected(long id)
    {
        if (_selectedMessage == null)
        {
            return;
        }

        switch (id)
        {
            case 0: // Copy
                DisplayServer.ClipboardSet(_selectedMessage.Text);
                ShowSuccessToast("Copied to clipboard!");
                break;
            case 1: // Reply
                _messageInput!.Text = $"@{_selectedMessage.Username} ";
                _messageInput.GrabFocus();
                _messageInput.CaretColumn = _messageInput.Text.Length;
                break;
            case 2: // Delete
                if (_selectedMessage.Username == _chatService!.Username)
                {
                    ShowToast("Delete feature coming soon!", PrimaryColor, "🗑️");
                }
                else
                {
                    ShowErrorToast("You can only delete your own messages");
                }
                break;
        }
    }

    private void ShowMessageContextMenu(MessageDto message, Vector2 position)
    {
        _selectedMessage = message;
        _contextMenu!.Position = (Vector2I)position;
        _contextMenu.Popup();
    }
    #endregion

    #region Emoji Picker
    private void PopulateEmojiPicker()
    {
        // Get references to child nodes (must access through _emojiPicker instance)
        var closeButton = _emojiPicker!.GetNode<Button>("%EmojiCloseButton");
        var emojiGrid = _emojiPicker.GetNode<GridContainer>("%EmojiGrid");

        // Setup close button
        closeButton.Pressed += () => _emojiPicker!.Hide();

        // Apply styling
        emojiGrid.AddThemeConstantOverride("h_separation", 4);
        emojiGrid.AddThemeConstantOverride("v_separation", 4);

        // Populate emoji grid
        foreach (var emoji in _emojiList)
        {
            var btn = new Button();
            btn.Text = emoji;
            btn.CustomMinimumSize = new Vector2(36, 36);
            btn.AddThemeFontSizeOverride("font_size", 20);
            btn.Pressed += () => OnEmojiSelected(emoji);
            emojiGrid.AddChild(btn);
        }
    }

    private void OnEmojiSelected(string emoji)
    {
        _messageInput!.Text += emoji;
        _messageInput.GrabFocus();
        _messageInput.CaretColumn = _messageInput.Text.Length;
        _emojiPicker!.Hide();
    }

    private void ToggleEmojiPicker()
    {
        if (_emojiPicker!.Visible)
        {
            _emojiPicker.Hide();
        }
        else
        {
            // Position near input field
            var inputPos = _messageInput!.GlobalPosition;
            _emojiPicker.Position = (Vector2I)(inputPos + new Vector2(0, -290));
            _emojiPicker.Popup();
        }
    }
    #endregion

    #region Rich Text Formatting
    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*([^\*]+?)\*(?!\*)")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"`(.+?)`")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"~~(.+?)~~")]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"(https?://[^\s]+)")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"@(\w+)")]
    private static partial Regex MentionRegex();

    private string FormatRichText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var formatted = text;

        // Escape existing BBCode to prevent injection
        formatted = formatted.Replace("[", "[[");

        // Bold: **text** → [b]text[/b]
        formatted = BoldRegex().Replace(formatted, "[b]$1[/b]");

        // Italic: *text* → [i]text[/i] (but not **)
        formatted = ItalicRegex().Replace(formatted, "[i]$1[/i]");

        // Code: `text` → [code]text[/code]
        formatted = CodeRegex().Replace(formatted, "[code]$1[/code]");

        // Strikethrough: ~~text~~ → [s]text[/s]
        formatted = StrikethroughRegex().Replace(formatted, "[s]$1[/s]");

        // URLs: Make clickable
        formatted = UrlRegex().Replace(formatted, "[url]$1[/url]");

        // Mentions: @username → colored
        formatted = MentionRegex().Replace(formatted, $"[color=#{PrimaryColor.ToHtml(false)}]@$1[/color]");

        return formatted;
    }
    #endregion

    #region Message Search
    private void SetupSearchPanel()
    {
        // Get references to child nodes (must access through _searchPanel instance)
        var closeButton = _searchPanel!.GetNode<Button>("%SearchCloseButton");

        // Setup close button
        closeButton.Pressed += () => _searchPanel!.Hide();

        // Setup search input events and styling
        _searchInput!.TextChanged += OnSearchTextChanged;

        var inputStyle = new StyleBoxFlat();
        inputStyle.BgColor = SurfaceDark;
        inputStyle.CornerRadiusTopLeft = 8;
        inputStyle.CornerRadiusTopRight = 8;
        inputStyle.CornerRadiusBottomLeft = 8;
        inputStyle.CornerRadiusBottomRight = 8;
        inputStyle.ContentMarginLeft = 12;
        inputStyle.ContentMarginRight = 12;
        inputStyle.ContentMarginTop = 8;
        inputStyle.ContentMarginBottom = 8;
        _searchInput.AddThemeStyleboxOverride("normal", inputStyle);

        // Setup results container styling
        _searchResults!.AddThemeConstantOverride("separation", 8);
    }

    private void OnSearchTextChanged(string searchText)
    {
        // Clear previous results
        foreach (var child in _searchResults!.GetChildren())
        {
            child.QueueFree();
        }

        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 2)
        {
            var hint = new Label();
            hint.Text = "Type at least 2 characters to search...";
            hint.AddThemeColorOverride("font_color", TextSecondary);
            _searchResults.AddChild(hint);
            return;
        }

        // Search through messages using service
        var results = _chatService!.SearchMessages(searchText, 20);

        if (results.Count == 0)
        {
            var noResults = new Label();
            noResults.Text = "No messages found";
            noResults.AddThemeColorOverride("font_color", TextSecondary);
            _searchResults.AddChild(noResults);
            return;
        }

        // Display results
        var resultCount = new Label();
        resultCount.Text = $"Found {results.Count} result(s)";
        resultCount.AddThemeFontSizeOverride("font_size", 12);
        resultCount.AddThemeColorOverride("font_color", SuccessColor);
        _searchResults.AddChild(resultCount);

        foreach (var message in results)
        {
            var resultItem = CreateSearchResultItem(message, searchText);
            _searchResults.AddChild(resultItem);
        }
    }

    private Control CreateSearchResultItem(MessageDto message, string searchText)
    {
        var item = new PanelContainer();

        var style = new StyleBoxFlat();
        style.BgColor = SurfaceDark;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        item.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);

        // Header
        var header = new HBoxContainer();
        var username = new Label();
        username.Text = message.Username;
        username.AddThemeColorOverride("font_color", GetUserColor(message.Username));
        header.AddChild(username);

        var timestamp = new Label();
        timestamp.Text = " • " + message.GetFormattedTime();
        timestamp.AddThemeFontSizeOverride("font_size", 11);
        timestamp.AddThemeColorOverride("font_color", TextSecondary);
        header.AddChild(timestamp);

        vbox.AddChild(header);

        // Message text with highlight
        var text = new Label();
        text.Text = HighlightSearchText(message.Text, searchText);
        text.AutowrapMode = TextServer.AutowrapMode.Word;
        text.AddThemeColorOverride("font_color", TextPrimary);
        vbox.AddChild(text);

        item.AddChild(vbox);

        // Make clickable to jump to message
        item.GuiInput += (inputEvent) =>
        {
            if (inputEvent is InputEventMouseButton mouseButton &&
                mouseButton.ButtonIndex == MouseButton.Left &&
                mouseButton.Pressed)
            {
                _searchPanel!.Hide();
                // TODO: Scroll to message in main view
                ShowToast("Jump to message coming soon!", PrimaryColor, "📍");
            }
        };

        return item;
    }

    private string HighlightSearchText(string text, string searchText)
    {
        // Simple highlight - could use BBCode for better highlighting
        var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            var before = text.Substring(0, index);
            var match = text.Substring(index, searchText.Length);
            var after = text.Substring(index + searchText.Length);
            return $"{before}[{match}]{after}";
        }
        return text;
    }

    private void ToggleSearchPanel()
    {
        if (_searchPanel!.Visible)
        {
            _searchPanel.Hide();
        }
        else
        {
            // Center on screen
            var viewportSize = GetViewportRect().Size;
            _searchPanel.Position = (Vector2I)((viewportSize - _searchPanel.Size) / 2);
            _searchPanel.Popup();
            _searchInput!.GrabFocus();
        }
    }
    #endregion

    #region Message Sending
    private void OnSendMessagePressed()
    {
        var text = _messageInput!.Text.Trim();
        GD.Print($"[ChatScene] OnSendMessagePressed() - Length: {text.Length}");

        if (string.IsNullOrEmpty(text))
        {
            ShowErrorToast("Message cannot be empty");
            return;
        }

        if (text.Length > 1000)
        {
            ShowErrorToast("Message exceeds 1000 character limit");
            return;
        }

        SendMessage(text);
    }

    private async void SendMessage(string text)
    {
        GD.Print($"[ChatScene] SendMessage() - Sending: '{text}'");

        // Disable send button
        _sendButton!.Disabled = true;

        // Clear input immediately (optimistic update)
        _messageInput!.Text = "";

        try
        {
            // Delegate to service
            var success = await _chatService!.SendMessageAsync(text);

            if (success)
            {
                ShowSuccessToast("Message sent!");
            }
            else
            {
                // Restore input on failure
                _messageInput.Text = text;
            }
        }
        catch (ConvexException ex)
        {
            GD.PrintErr($"[ChatScene] SendMessage() failed: {ex.Message}");
            _messageInput.Text = text; // Restore on error
        }
        finally
        {
            _sendButton.Disabled = false;
        }
    }

    private void AnimateSendButton()
    {
        var tween = CreateTween();
        tween.TweenProperty(_sendButton, "scale", new Vector2(0.9f, 0.9f), 0.1);
        tween.TweenProperty(_sendButton, "scale", Vector2.One, 0.1);
    }
    #endregion

    #region Status Updates
    private void OnConnectionStateChanged(int newStateInt)
    {
        var newState = (ConnectionState)newStateInt;
        var (statusText, statusColor) = newState switch
        {
            ConnectionState.Connected => ("Connected", SuccessColor),
            ConnectionState.Connecting => ("Connecting...", PrimaryColor),
            ConnectionState.Disconnected => ("Disconnected", ErrorColor),
            ConnectionState.Reconnecting => ("Reconnecting...", SecondaryColor),
            _ => ("Unknown", TextSecondary)
        };

        UpdateStatusBadge(statusText, statusColor);
    }

    private void UpdateStatusLabel(string text)
    {
        _statusLabel!.Text = text;
    }

    private void UpdateStatusBadge(string text, Color color)
    {
        _statusLabel!.Text = "● " + text;
        _statusLabel.AddThemeColorOverride("font_color", color);
    }
    #endregion

    #region Toast Notifications
    private void OnChatServiceError(string errorMessage)
    {
        GD.Print($"[ChatScene] ===== ChatService ERROR =====: {errorMessage}");
        GD.Print($"[ChatScene] Error contains 'Authentication': {errorMessage.Contains("Authentication", StringComparison.OrdinalIgnoreCase)}");
        GD.Print($"[ChatScene] Error contains 'auth': {errorMessage.Contains("auth", StringComparison.OrdinalIgnoreCase)}");

        // Check if this is an authentication error - show dialog for ANY auth-related error
        var isAuthError = errorMessage.Contains("Authentication required", StringComparison.OrdinalIgnoreCase) ||
                         errorMessage.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
                         errorMessage.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                         errorMessage.Contains("auth", StringComparison.OrdinalIgnoreCase);

        GD.Print($"[ChatScene] Is auth error: {isAuthError}");

        if (isAuthError)
        {
            GD.Print("[ChatScene] ===== AUTHENTICATION ERROR DETECTED - SHOWING DIALOG ===== ");
            // Always show auth dialog for auth errors, even if Clerk isn't configured in frontend
            // The backend requires auth, so we need to authenticate
            // Use CallDeferred to ensure we're on the main thread
            CallDeferred(nameof(ShowAuthDialog));
            // Also try showing immediately as backup
            ShowAuthDialog();
            return;
        }

        // For other errors, show error toast
        GD.Print($"[ChatScene] Not an auth error, showing error toast");
        ShowErrorToast(errorMessage);
    }

    private void ShowErrorToast(string message)
    {
        if (_toastScene == null) return;

        var toast = _toastScene.Instantiate<Toast>();
        AddChild(toast);
        toast.Show(message, Toast.ToastType.Error);
    }

    private void ShowSuccessToast(string message)
    {
        if (_toastScene == null) return;

        var toast = _toastScene.Instantiate<Toast>();
        AddChild(toast);
        toast.Show(message, Toast.ToastType.Success);
    }

    private void ShowToast(string message, Color bgColor, string icon)
    {
        if (_toastScene == null) return;

        var toast = _toastScene.Instantiate<Toast>();
        AddChild(toast);
        toast.ShowCustom(message, bgColor, icon);
    }
    #endregion

    #region User Management
    private void OnUsernameSelected(int index)
    {
        if (_usernameDropdown == null) return;
        var newUsername = _usernameDropdown.GetItemText(index);
        var oldUsername = _chatService!.Username;
        GD.Print($"[ChatScene] Username changed: {oldUsername} → {newUsername}");

        // Update service username
        _chatService.Username = newUsername;
        _userLabel!.Text = $"User: {newUsername}";
    }

    private void OnAuthButtonPressed()
    {
        ShowAuthDialog();
    }

    private void ShowAuthDialog()
    {
        GD.Print("[ChatScene] ===== ShowAuthDialog CALLED ===== ");

        // Prevent multiple dialogs
        if (_authDialog != null && GodotObject.IsInstanceValid(_authDialog))
        {
            GD.Print("[ChatScene] Auth dialog already exists, bringing to front");
            _authDialog.PopupCentered();
            return;
        }

        GD.Print("[ChatScene] ShowAuthDialog called - FORCING dialog to show");

        // Get or create auth service
        var authService = ConvexManager.Instance.BetterAuthService;
        if (authService == null)
        {
            GD.Print("[ChatScene] BetterAuthService is null, checking if we can create it");
            var config = ConvexManager.Instance.ChatConfig;

            if (config?.HasBetterAuth == true)
            {
                GD.Print("[ChatScene] Creating BetterAuthService from config");
                var options = new BetterAuthOptions
                {
                    SiteUrl = config.BetterAuthSiteUrl!
                };
                var sessionStorage = new GodotSessionStorage();
                var httpClient = new System.Net.Http.HttpClient();
                authService = new BetterAuthService(httpClient, sessionStorage, options);
                ConvexManager.Instance.BetterAuthService = authService;
            }
            else
            {
                GD.PrintErr("[ChatScene] Cannot show auth dialog - no Better Auth configuration at all");
                ShowErrorToast("Better Auth is required but not configured. Please set BetterAuth:SiteUrl in appsettings.json");
                return;
            }
        }

        GD.Print("[ChatScene] Loading BetterAuthDialog scene...");
        // Load BetterAuthDialog scene
        var dialogScene = GD.Load<PackedScene>("res://BetterAuthDialog.tscn");
        if (dialogScene == null)
        {
            GD.PrintErr("[ChatScene] BetterAuthDialog.tscn not found. Creating dialog programmatically...");
            // Fallback: Create dialog programmatically
            CreateAuthDialogProgrammatically(authService);
            return;
        }

        GD.Print("[ChatScene] Instantiating BetterAuthDialog...");
        _authDialog = dialogScene.Instantiate<BetterAuthDialog>();
        if (_authDialog == null)
        {
            GD.PrintErr("[ChatScene] Failed to instantiate BetterAuthDialog - using fallback");
            // Fallback to programmatic dialog
            CreateAuthDialogProgrammatically(authService);
            return;
        }

        GD.Print("[ChatScene] Adding dialog to scene tree...");
        AddChild(_authDialog);

        GD.Print("[ChatScene] Connecting signals...");
        _authDialog.Connect(BetterAuthDialog.SignalName.AuthenticationSucceeded, new Callable(this, nameof(OnAuthenticationSucceeded)));
        _authDialog.Connect(BetterAuthDialog.SignalName.AuthenticationFailed, new Callable(this, nameof(OnAuthenticationFailed)));

        GD.Print("[ChatScene] Initializing dialog...");
        _authDialog.Initialize(authService);

        GD.Print("[ChatScene] Showing BetterAuthDialog...");
        _authDialog.PopupCentered();

        // Ensure the dialog is visible and on top
        _authDialog.Visible = true;

        // Force it to be modal and on top
        if (_authDialog is Window window)
        {
            window.PopupWindow = true;
            window.AlwaysOnTop = true;
        }

        GD.Print("[ChatScene] ===== Dialog setup complete - it should be visible now! ===== ");
        GD.Print($"[ChatScene] Dialog visible: {_authDialog.Visible}, Dialog in scene tree: {_authDialog.GetParent() != null}");
    }

    private void CreateAuthDialogProgrammatically(IBetterAuthService authService)
    {
        GD.Print("[ChatScene] Creating auth dialog programmatically...");

        // Create a simple dialog programmatically as fallback
        var dialog = new Window();
        dialog.Title = "Sign In";
        dialog.Size = new Vector2I(400, 250);
        dialog.PopupWindow = true;
        dialog.AlwaysOnTop = true;
        dialog.Unresizable = false;

        var vbox = new VBoxContainer();
        vbox.AnchorLeft = 0.0f;
        vbox.AnchorTop = 0.0f;
        vbox.AnchorRight = 1.0f;
        vbox.AnchorBottom = 1.0f;
        vbox.OffsetLeft = 10;
        vbox.OffsetTop = 10;
        vbox.OffsetRight = -10;
        vbox.OffsetBottom = -10;
        dialog.AddChild(vbox);

        var label = new Label();
        label.Text = "Sign in with your email and password:";
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(label);

        var emailInput = new LineEdit();
        emailInput.PlaceholderText = "Email";
        emailInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddChild(emailInput);

        var passwordInput = new LineEdit();
        passwordInput.PlaceholderText = "Password";
        passwordInput.Secret = true;
        passwordInput.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddChild(passwordInput);

        var buttonContainer = new HBoxContainer();
        buttonContainer.Alignment = BoxContainer.AlignmentMode.End;
        vbox.AddChild(buttonContainer);

        var cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Pressed += () => dialog.QueueFree();
        buttonContainer.AddChild(cancelButton);

        var signInButton = new Button();
        signInButton.Text = "Sign In";
        signInButton.Pressed += async () =>
        {
            var email = emailInput.Text.Trim();
            var password = passwordInput.Text;
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                try
                {
                    var result = await authService.SignInAsync(email, password);
                    if (result.IsSuccess)
                    {
                        GD.Print("[ChatScene] Sign in successful");
                        OnAuthenticationSucceeded();
                        dialog.QueueFree();
                    }
                    else
                    {
                        GD.PrintErr($"[ChatScene] Sign in failed: {result.ErrorMessage}");
                        ShowErrorToast(result.ErrorMessage ?? "Sign in failed");
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[ChatScene] Failed to sign in: {ex.Message}");
                    ShowErrorToast($"Failed to sign in: {ex.Message}");
                }
            }
        };
        buttonContainer.AddChild(signInButton);

        GetTree().Root.AddChild(dialog);
        dialog.PopupCentered();
        GD.Print("[ChatScene] Programmatic auth dialog shown");
    }

    private void OnAuthenticationSucceeded()
    {
        GD.Print("[ChatScene] Authentication succeeded");
        UpdateUserLabel();
        _authDialog?.QueueFree();
        _authDialog = null;

        // Load and display messages after authentication
        _ = _chatService!.LoadInitialMessagesAsync();
        // Subscribe to real-time message updates
        _chatService.SubscribeToMessages();
    }

    private void OnAuthenticationFailed(string errorMessage)
    {
        GD.PrintErr($"[ChatScene] Authentication failed: {errorMessage}");
        ShowErrorToast($"Authentication failed: {errorMessage}");
    }

    private void UpdateUserLabel()
    {
        var authService = ConvexManager.Instance.BetterAuthService;
        if (authService == null || !authService.IsAuthenticated)
        {
            _userLabel!.Text = "User: Not authenticated";
            return;
        }

        // Show user info from Better Auth
        var user = authService.CurrentUser;
        if (_userLabel != null)
        {
            _userLabel.Text = user != null ? $"User: {user.Name ?? user.Email}" : "User: Authenticated";
        }

        // Update chat service username if needed
        if (_chatService != null)
        {
            _chatService.Username = user?.Name ?? user?.Email ?? "Authenticated User";
        }
    }

    private void OnConnectButtonPressed()
    {
        GD.Print("[ChatScene] Manual reconnection requested");
        _ = _chatService!.LoadInitialMessagesAsync();
    }
    #endregion

    #region Loading More Indicator
    private void SetupLoadingMoreIndicator()
    {
        // Create loading indicator panel
        _loadingMoreIndicator = new PanelContainer();
        _loadingMoreIndicator.Visible = false;
        _loadingMoreIndicator.CustomMinimumSize = new Vector2(0, 40);

        // Style the panel
        var style = new StyleBoxFlat();
        style.BgColor = SurfaceDark;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        _loadingMoreIndicator.AddThemeStyleboxOverride("panel", style);

        // Create content container
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;

        // Loading label
        var label = new Label();
        label.Text = "Loading older messages...";
        label.AddThemeColorOverride("font_color", TextSecondary);
        label.AddThemeFontSizeOverride("font_size", 12);
        hbox.AddChild(label);

        _loadingMoreIndicator.AddChild(hbox);
    }

    private void ShowLoadingMoreIndicator(bool show)
    {
        if (_loadingMoreIndicator != null && GodotObject.IsInstanceValid(_loadingMoreIndicator))
        {
            _loadingMoreIndicator.Visible = show;
        }
    }
    #endregion

    #region Load More Button
    private void SetupLoadMoreButton()
    {
        // Create load more button
        _loadMoreButton = new Button();
        _loadMoreButton.Text = "Load Older Messages";
        _loadMoreButton.Visible = false;
        _loadMoreButton.CustomMinimumSize = new Vector2(0, 40);

        // Style the button
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = SurfaceDark;
        buttonStyle.CornerRadiusTopLeft = 8;
        buttonStyle.CornerRadiusTopRight = 8;
        buttonStyle.CornerRadiusBottomLeft = 8;
        buttonStyle.CornerRadiusBottomRight = 8;
        buttonStyle.ContentMarginLeft = 16;
        buttonStyle.ContentMarginRight = 16;
        buttonStyle.ContentMarginTop = 8;
        buttonStyle.ContentMarginBottom = 8;
        _loadMoreButton.AddThemeStyleboxOverride("normal", buttonStyle);

        var buttonHoverStyle = new StyleBoxFlat();
        buttonHoverStyle.BgColor = SurfaceDark.Lightened(0.1f);
        buttonHoverStyle.CornerRadiusTopLeft = 8;
        buttonHoverStyle.CornerRadiusTopRight = 8;
        buttonHoverStyle.CornerRadiusBottomLeft = 8;
        buttonHoverStyle.CornerRadiusBottomRight = 8;
        buttonHoverStyle.ContentMarginLeft = 16;
        buttonHoverStyle.ContentMarginRight = 16;
        buttonHoverStyle.ContentMarginTop = 8;
        buttonHoverStyle.ContentMarginBottom = 8;
        _loadMoreButton.AddThemeStyleboxOverride("hover", buttonHoverStyle);

        _loadMoreButton.AddThemeColorOverride("font_color", TextPrimary);
        _loadMoreButton.AddThemeFontSizeOverride("font_size", 14);

        // Connect button press event
        _loadMoreButton.Pressed += OnLoadMoreButtonPressed;

        // Add button to message container at the top
        if (_messageContainer != null)
        {
            _messageContainer.AddChild(_loadMoreButton);
            _messageContainer.MoveChild(_loadMoreButton, 0);
        }
    }

    private void OnLoadMoreButtonPressed()
    {
        if (_chatService == null || _isLoadingOlderMessages) return;
        if (!_chatService.HasMoreMessages) return;

        GD.Print("[ChatScene] Load More button pressed, loading older messages...");

        // Capture current message count before loading - this will be a pagination boundary
        // This represents how many messages were visible before loading older ones
        // After loading, older messages are added at the beginning (oldest first), so this count
        // becomes the index where the previously visible messages start
        _previousMessageCount = _chatService.CurrentMessages.Count;
        _shouldAddSeparator = true; // Mark that we should add a separator
        GD.Print($"[ChatScene] Captured previous message count: {_previousMessageCount}, will add separator at this index after loading");

        _ = _chatService.LoadOlderMessagesAsync();
    }

    private void UpdateLoadMoreButton()
    {
        if (_loadMoreButton == null || _chatService == null) return;

        // Show button if there are more messages to load and we're not currently loading
        _loadMoreButton.Visible = _chatService.HasMoreMessages && !_isLoadingOlderMessages;
        _loadMoreButton.Disabled = _isLoadingOlderMessages;

        if (_isLoadingOlderMessages)
        {
            _loadMoreButton.Text = "Loading...";
        }
        else
        {
            _loadMoreButton.Text = "Load Older Messages";
        }
    }
    #endregion

    #region Cleanup
    public override void _ExitTree()
    {
        GD.Print("[ChatScene] Cleaning up resources");

        ConvexManager.Instance.ConnectionStateChanged -= OnConnectionStateChanged;

        // Unsubscribe from service events
        if (_chatService != null)
        {
            _chatService.MessagesUpdated -= OnMessagesUpdated;
            _chatService.LoadingStateChanged -= OnLoadingStateChanged;
            _chatService.LoadingMoreStateChanged -= OnLoadingMoreStateChanged;
            _chatService.ErrorOccurred -= OnChatServiceError;

            // Dispose service
            _chatService.Dispose();
        }

    }
    #endregion
}
