using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Godot dialog for Clerk authentication using OAuth 2.0 Authorization Code Flow with PKCE.
/// </summary>
public partial class ClerkAuthDialog : AcceptDialog
{
    private GodotClerkTokenService? _tokenService;
    private CancellationTokenSource? _authCancellation;

    // UI References (set via scene)
    private Label? _statusLabel;
    private Button? _signInButton;
    private LineEdit? _manualTokenInput;
    private Button? _manualTokenButton;
    private ProgressBar? _progressBar;
    private VBoxContainer? _authContainer;
    private VBoxContainer? _manualTokenContainer;
    private Button? _switchToManualButton;
    private Label? _manualUrlLabel;

    private bool _isAuthenticating = false;

    /// <summary>
    /// Signal emitted when authentication succeeds.
    /// </summary>
    [Signal]
    public delegate void AuthenticationSucceededEventHandler();

    /// <summary>
    /// Signal emitted when authentication fails.
    /// </summary>
    [Signal]
    public delegate void AuthenticationFailedEventHandler(string errorMessage);

    public override void _Ready()
    {
        // Find UI nodes (these should be set up in the scene)
        _statusLabel = GetNodeOrNull<Label>("%StatusLabel");
        _signInButton = GetNodeOrNull<Button>("%SignInButton");
        _manualTokenInput = GetNodeOrNull<LineEdit>("%ManualTokenInput");
        _manualTokenButton = GetNodeOrNull<Button>("%ManualTokenButton");
        _progressBar = GetNodeOrNull<ProgressBar>("%ProgressBar");
        _authContainer = GetNodeOrNull<VBoxContainer>("%AuthContainer");
        _manualTokenContainer = GetNodeOrNull<VBoxContainer>("%ManualTokenContainer");
        _switchToManualButton = GetNodeOrNull<Button>("%SwitchToManualButton");
        _manualUrlLabel = GetNodeOrNull<Label>("%ManualUrlLabel");

        // Connect button signals
        if (_signInButton != null)
        {
            _signInButton.Pressed += OnSignInPressed;
        }

        if (_manualTokenButton != null)
        {
            _manualTokenButton.Pressed += OnManualTokenPressed;
        }

        if (_switchToManualButton != null)
        {
            _switchToManualButton.Pressed += OnSwitchToManualPressed;
        }

        // Hide manual token container initially
        if (_manualTokenContainer != null)
        {
            _manualTokenContainer.Visible = false;
        }

        // Hide progress bar initially
        if (_progressBar != null)
        {
            _progressBar.Visible = false;
        }

        // Hide manual URL label initially
        if (_manualUrlLabel != null)
        {
            _manualUrlLabel.Visible = false;
        }

        // Set up dialog
        Title = "Sign In with Clerk";
        SetText("Please sign in to continue.");

        // Set initial status
        UpdateStatus("Click 'Sign In' to authenticate with Clerk");
    }

    /// <summary>
    /// Initializes the dialog with a token service.
    /// </summary>
    public void Initialize(GodotClerkTokenService tokenService)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        UpdateStatus("Ready to sign in");
    }

    private async void OnSignInPressed()
    {
        if (_tokenService == null || _isAuthenticating)
        {
            return;
        }

        _isAuthenticating = true;
        _authCancellation = new CancellationTokenSource();

        try
        {
            // Disable sign in button
            if (_signInButton != null)
            {
                _signInButton.Disabled = true;
            }

            // Show progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                // Animate progress bar
                var tween = CreateTween();
                tween.SetLoops();
                tween.TweenProperty(_progressBar, "value", 100.0, 2.0);
            }

            UpdateStatus("Opening browser for authentication...");
            await Task.Delay(100); // Brief delay to update UI

            // Start authorization flow
            var result = await _tokenService.StartAuthorizationFlowAsync(_authCancellation.Token);

            if (result.Success)
            {
                UpdateStatus("Authentication successful!");
                await Task.Delay(500); // Brief delay to show success message
                EmitSignal(SignalName.AuthenticationSucceeded);
                Hide();
            }
            else
            {
                // Check if browser failed to open
                if (!string.IsNullOrEmpty(result.AuthorizationUrl))
                {
                    // Show manual URL option
                    UpdateStatus("Failed to open browser automatically.", isError: true);
                    ShowManualUrlOption(result.AuthorizationUrl);
                }
                else
                {
                    UpdateStatus($"Authentication failed: {result.ErrorMessage}", isError: true);
                    EmitSignal(SignalName.AuthenticationFailed, result.ErrorMessage ?? "Authentication failed");
                    ShowManualTokenOption();
                }
            }
        }
        catch (TaskCanceledException)
        {
            UpdateStatus("Authentication cancelled.");
            EmitSignal(SignalName.AuthenticationFailed, "Authentication cancelled");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ClerkAuthDialog] Authentication error: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
            EmitSignal(SignalName.AuthenticationFailed, ex.Message);
            ShowManualTokenOption();
        }
        finally
        {
            _isAuthenticating = false;

            // Re-enable sign in button
            if (_signInButton != null)
            {
                _signInButton.Disabled = false;
            }

            // Hide progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = false;
            }
        }
    }

    private void ShowManualUrlOption(string url)
    {
        if (_manualUrlLabel != null)
        {
            _manualUrlLabel.Text = $"Please visit this URL manually:\n{url}";
            _manualUrlLabel.Visible = true;
        }

        // Add button to copy URL to clipboard
        var copyButton = GetNodeOrNull<Button>("%CopyUrlButton");
        if (copyButton != null)
        {
            copyButton.Visible = true;
            copyButton.Pressed += () =>
            {
                DisplayServer.ClipboardSet(url);
                UpdateStatus("URL copied to clipboard!");
            };
        }
    }

    private void OnSwitchToManualPressed()
    {
        ShowManualTokenOption();
    }

    private void ShowManualTokenOption()
    {
        if (_authContainer != null)
        {
            _authContainer.Visible = false;
        }

        if (_manualTokenContainer != null)
        {
            _manualTokenContainer.Visible = true;
        }

        UpdateStatus("Enter your Clerk JWT token manually:");
    }

    private void OnManualTokenPressed()
    {
        if (_tokenService == null || _manualTokenInput == null)
        {
            return;
        }

        var token = _manualTokenInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            UpdateStatus("Please enter a token.", isError: true);
            return;
        }

        try
        {
            _tokenService.SetTokenManually(token);
            UpdateStatus("Token set successfully!");
            EmitSignal(SignalName.AuthenticationSucceeded);
            Hide();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ClerkAuthDialog] Failed to set manual token: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
            EmitSignal(SignalName.AuthenticationFailed, ex.Message);
        }
    }

    private void UpdateStatus(string message, bool isError = false)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            if (isError)
            {
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
            }
            else
            {
                _statusLabel.RemoveThemeColorOverride("font_color");
            }
        }

        GD.Print($"[ClerkAuthDialog] {message}");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _authCancellation?.Cancel();
            EmitSignal(SignalName.AuthenticationFailed, "Dialog closed by user");
            QueueFree();
        }
    }

    public override void _ExitTree()
    {
        // Cancel authentication if still active
        _authCancellation?.Cancel();
        _authCancellation?.Dispose();
    }
}
