using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Convex.Client.Shared.Connection;
using RealtimeChat.Shared.Models;
using RealtimeChat.Shared.Services;

namespace WpfRealTimeChat;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ChatService? _chatService;
    private readonly Dictionary<string, SolidColorBrush> _userColors = [];
    private bool _isAutoScrolling = true;

    public MainWindow()
    {
        InitializeComponent();
        InitializeChat();
    }

    private void InitializeChat()
    {
        try
        {
            // Initialize chat service with configurable initial message limit
            _chatService = new ChatService(
                App.ConvexClient,
                initialMessageLimit: App.ChatConfig.InitialMessageLimit);

            // Subscribe to service events
            // Note: Callbacks are already on UI thread thanks to WithUIThreadMarshalling() in ChatService
            _chatService.MessagesUpdated += OnMessagesUpdated;
            _chatService.LoadingStateChanged += OnLoadingStateChanged;
            _chatService.LoadingMoreStateChanged += OnLoadingMoreStateChanged;
            _chatService.ErrorOccurred += OnErrorOccurred;

            // Subscribe to connection state changes
            App.ConvexClient.ConnectionStateChanges.Subscribe(
                state => Dispatcher.Invoke(() => UpdateConnectionStatus(state)),
                error => Dispatcher.Invoke(() => OnErrorOccurred($"Connection error: {error}")));

            // Load initial messages
            _ = _chatService.LoadInitialMessagesAsync();

            // Set initial username
            _chatService.Username = "Anonymous";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize chat: {ex.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #region Event Handlers

    // Callback is already on UI thread, no need for Dispatcher.Invoke
    private void OnMessagesUpdated(List<MessageDto> messages) => DisplayMessages(messages);

    private void OnLoadingStateChanged(bool isLoading)
    {
        // Callback is already on UI thread, no need for Dispatcher.Invoke
        LoadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        SendButton.IsEnabled = !isLoading;
    }

    private void OnLoadingMoreStateChanged(bool isLoading)
    {
        // Callback is already on UI thread, no need for Dispatcher.Invoke
        LoadMoreButton.IsEnabled = !isLoading;
        LoadMoreButton.Content = isLoading ? "Loading..." : "Load Older Messages";
    }

    private void OnErrorOccurred(string error)
    {
        // Callback is already on UI thread, no need for Dispatcher.Invoke
        StatusLabel.Text = $"Error: {error}";
        StatusLabel.Foreground = new SolidColorBrush(Colors.Red);
    }

    private void UpdateConnectionStatus(ConnectionState state)
    {
        var (statusText, statusColor) = state switch
        {
            ConnectionState.Connected => ("Connected", Colors.Green),
            ConnectionState.Connecting => ("Connecting...", Colors.Orange),
            ConnectionState.Disconnected => ("Disconnected", Colors.Red),
            ConnectionState.Reconnecting => ("Reconnecting...", Colors.Orange),
            ConnectionState.Failed => ("Connection Failed", Colors.Red),
            _ => ("Unknown", Colors.Gray)
        };

        StatusLabel.Text = $"● {statusText}";
        StatusLabel.Foreground = new SolidColorBrush(statusColor);
    }

    #endregion

    #region UI Event Handlers

    private void SendButton_Click(object sender, RoutedEventArgs e) => SendMessage();

    private void MessageTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && !e.KeyboardDevice.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
        {
            e.Handled = true;
            SendMessage();
        }
    }

    private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (_chatService != null && _chatService.HasMoreMessages)
        {
            _isAutoScrolling = false;
            _ = _chatService.LoadOlderMessagesAsync();
        }
    }

    private void UsernameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsernameComboBox.SelectedItem is ComboBoxItem selectedItem && _chatService != null)
        {
            var username = selectedItem.Content.ToString() ?? "Anonymous";
            _chatService.Username = username;
            UsernameLabel.Text = $"User: {username}";
        }
    }

    #endregion

    #region Message Display

    private void DisplayMessages(List<MessageDto> messages)
    {
        // Clear existing messages (except load more button and loading indicator)
        var childrenToRemove = MessagePanel.Children
            .OfType<FrameworkElement>()
            .Where(c => c != LoadMoreButton && c != LoadingIndicator)
            .ToList();

        foreach (var child in childrenToRemove)
        {
            MessagePanel.Children.Remove(child);
        }

        // Sort messages by timestamp (oldest first)
        var sortedMessages = messages.OrderBy(m => m.Timestamp).ToList();

        // Add messages
        MessageDto? previousMessage = null;
        foreach (var message in sortedMessages)
        {
            var shouldGroup = ShouldGroupWithPrevious(message, previousMessage);
            var messageElement = shouldGroup
                ? CreateCompactMessageElement(message)
                : CreateFullMessageElement(message);

            MessagePanel.Children.Add(messageElement);
            previousMessage = message;
        }

        // Update load more button visibility
        if (_chatService != null)
        {
            LoadMoreButton.Visibility = _chatService.HasMoreMessages ? Visibility.Visible : Visibility.Collapsed;
        }

        // Auto-scroll to bottom if enabled
        if (_isAutoScrolling)
        {
            ScrollToBottom();
        }
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

    private FrameworkElement CreateFullMessageElement(MessageDto message)
    {
        var container = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var messageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // Avatar circle
        var avatar = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(20),
            Background = GetUserColor(message.Username),
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var avatarText = new TextBlock
        {
            Text = message.Username.Length > 0 ? message.Username[0].ToString().ToUpper() : "?",
            Foreground = new SolidColorBrush(Colors.White),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        avatar.Child = avatarText;
        messageRow.Children.Add(avatar);

        // Message content
        var contentBox = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        // Header (username + timestamp)
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var username = new TextBlock
        {
            Text = message.Username,
            Foreground = GetUserColor(message.Username),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 8, 0)
        };
        header.Children.Add(username);

        var timestamp = new TextBlock
        {
            Text = message.GetFormattedTime(),
            Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
            FontSize = 11
        };
        header.Children.Add(timestamp);

        contentBox.Children.Add(header);

        // Message bubble
        var bubble = CreateMessageBubble(message);
        contentBox.Children.Add(bubble);

        messageRow.Children.Add(contentBox);
        container.Children.Add(messageRow);

        return container;
    }

    private FrameworkElement CreateCompactMessageElement(MessageDto message)
    {
        var container = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(52, 4, 0, 0) // Offset by avatar width + margin
        };

        // Message bubble only
        var bubble = CreateMessageBubble(message);
        container.Children.Add(bubble);

        return container;
    }

    private Border CreateMessageBubble(MessageDto message)
    {
        var bubble = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(44, 44, 44)),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 8, 12, 8),
            MaxWidth = 600
        };

        var contentBox = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        // Message text
        var textLabel = new TextBlock
        {
            Text = message.Text,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };
        contentBox.Children.Add(textLabel);

        // Attachments indicator
        if (message.Attachments != null && message.Attachments.Count > 0)
        {
            var attachmentLabel = new TextBlock
            {
                Text = $"📎 {message.Attachments.Count} attachment(s)",
                Foreground = new SolidColorBrush(Colors.LightGreen),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };
            contentBox.Children.Add(attachmentLabel);
        }

        bubble.Child = contentBox;
        return bubble;
    }

    private SolidColorBrush GetUserColor(string username)
    {
        if (_userColors.TryGetValue(username, out var existingBrush))
        {
            return existingBrush;
        }

        // Generate deterministic color based on username hash
        var hash = username.GetHashCode();
        var hue = Math.Abs(hash) % 360 / 360.0;
        var color = ColorFromHSV(hue, 0.6, 0.85);
        var brush = new SolidColorBrush(color);
        _userColors[username] = brush;
        return brush;
    }

    private Color ColorFromHSV(double hue, double saturation, double value)
    {
        var hi = Convert.ToInt32(Math.Floor(hue * 6)) % 6;
        var f = (hue * 6) - Math.Floor(hue * 6);
        var p = value * (1 - saturation);
        var q = value * (1 - (f * saturation));
        var t = value * (1 - ((1 - f) * saturation));

        var (r, g, b) = hi switch
        {
            0 => (value, t, p),
            1 => (q, value, p),
            2 => (p, value, t),
            3 => (p, q, value),
            4 => (t, p, value),
            _ => (value, p, q)
        };

        return Color.FromRgb(
            (byte)(r * 255),
            (byte)(g * 255),
            (byte)(b * 255));
    }

    private void ScrollToBottom()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            MessageScrollViewer.ScrollToEnd();
        }), DispatcherPriority.Loaded);
    }

    #endregion

    #region Message Sending

    private async void SendMessage()
    {
        var text = MessageTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(text) || _chatService == null)
        {
            return;
        }

        if (text.Length > 1000)
        {
            MessageBox.Show(
                "Message cannot exceed 1000 characters.",
                "Message Too Long",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Disable send button
        SendButton.IsEnabled = false;

        // Clear input immediately (optimistic update)
        MessageTextBox.Text = "";

        try
        {
            var success = await _chatService.SendMessageAsync(text);

            if (!success)
            {
                // Restore input on failure
                MessageTextBox.Text = text;
            }
            else
            {
                // Re-enable auto-scrolling after sending
                _isAutoScrolling = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to send message: {ex.Message}",
                "Send Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            MessageTextBox.Text = text; // Restore on error
        }
        finally
        {
            SendButton.IsEnabled = true;
            MessageTextBox.Focus();
        }
    }

    #endregion

    #region Cleanup

    protected override void OnClosed(EventArgs e)
    {
        _chatService?.Dispose();
        base.OnClosed(e);
    }

    #endregion
}
