using Godot;
using Convex.Client.Extensions.Clerk.Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Convex.Client.Extensions.Clerk.Godot;

/// <summary>
/// Godot dialog for Clerk authentication using device code flow.
/// </summary>
public partial class ClerkAuthDialog : AcceptDialog
{
    private GodotClerkTokenService? _tokenService;
    private CancellationTokenSource? _pollingCancellation;
    
    // UI References (set via scene)
    private Label? _statusLabel;
    private Label? _userCodeLabel;
    private Label? _verificationUrlLabel;
    private Button? _openBrowserButton;
    private LineEdit? _manualTokenInput;
    private Button? _manualTokenButton;
    private ProgressBar? _progressBar;
    private VBoxContainer? _deviceCodeContainer;
    private VBoxContainer? _manualTokenContainer;
    private Button? _switchToManualButton;

    private ClerkDeviceCodeFlow.DeviceCodeResponse? _deviceCodeResponse;
    private bool _isPolling = false;

    /// <summary>
    /// Signal emitted when authentication succeeds.
    /// </summary>
    [Signal]
    public delegate void AuthenticationSucceeded();

    /// <summary>
    /// Signal emitted when authentication fails.
    /// </summary>
    [Signal]
    public delegate void AuthenticationFailed(string errorMessage);

    public override void _Ready()
    {
        // Find UI nodes (these should be set up in the scene)
        _statusLabel = GetNodeOrNull<Label>("%StatusLabel");
        _userCodeLabel = GetNodeOrNull<Label>("%UserCodeLabel");
        _verificationUrlLabel = GetNodeOrNull<Label>("%VerificationUrlLabel");
        _openBrowserButton = GetNodeOrNull<Button>("%OpenBrowserButton");
        _manualTokenInput = GetNodeOrNull<LineEdit>("%ManualTokenInput");
        _manualTokenButton = GetNodeOrNull<Button>("%ManualTokenButton");
        _progressBar = GetNodeOrNull<ProgressBar>("%ProgressBar");
        _deviceCodeContainer = GetNodeOrNull<VBoxContainer>("%DeviceCodeContainer");
        _manualTokenContainer = GetNodeOrNull<VBoxContainer>("%ManualTokenContainer");
        _switchToManualButton = GetNodeOrNull<Button>("%SwitchToManualButton");

        // Connect button signals
        if (_openBrowserButton != null)
        {
            _openBrowserButton.Pressed += OnOpenBrowserPressed;
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

        // Set up dialog
        Title = "Sign In with Clerk";
        SetText("Please sign in to continue.");
    }

    /// <summary>
    /// Initializes the dialog with a token service and starts the device code flow.
    /// </summary>
    public async void Initialize(GodotClerkTokenService tokenService)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));

        try
        {
            UpdateStatus("Requesting authentication code...");
            
            _deviceCodeResponse = await _tokenService.StartDeviceCodeFlowAsync();
            
            DisplayDeviceCode(_deviceCodeResponse);
            StartPolling();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ClerkAuthDialog] Failed to start device code flow: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
            ShowManualTokenOption();
        }
    }

    private void DisplayDeviceCode(ClerkDeviceCodeFlow.DeviceCodeResponse response)
    {
        if (_userCodeLabel != null)
        {
            _userCodeLabel.Text = $"Your code: {response.UserCode}";
        }

        if (_verificationUrlLabel != null)
        {
            _verificationUrlLabel.Text = $"Visit: {response.VerificationUri}";
        }

        UpdateStatus($"Enter code {response.UserCode} at the verification URL");
    }

    private void OnOpenBrowserPressed()
    {
        if (_deviceCodeResponse != null && !string.IsNullOrEmpty(_deviceCodeResponse.VerificationUriComplete))
        {
            OS.ShellOpen(_deviceCodeResponse.VerificationUriComplete);
        }
        else if (_deviceCodeResponse != null && !string.IsNullOrEmpty(_deviceCodeResponse.VerificationUri))
        {
            OS.ShellOpen(_deviceCodeResponse.VerificationUri);
        }
    }

    private async void StartPolling()
    {
        if (_tokenService == null || _deviceCodeResponse == null || _isPolling)
        {
            return;
        }

        _isPolling = true;
        _pollingCancellation = new CancellationTokenSource();

        try
        {
            UpdateStatus("Waiting for you to sign in...");
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                // Animate progress bar
                var tween = CreateTween();
                tween.SetLoops();
                tween.TweenProperty(_progressBar, "value", 100.0, 2.0);
            }

            var success = await _tokenService.CompleteDeviceCodeFlowAsync(
                _deviceCodeResponse.DeviceCode,
                _deviceCodeResponse.Interval,
                _deviceCodeResponse.ExpiresIn,
                _pollingCancellation.Token);

            if (success)
            {
                UpdateStatus("Authentication successful!");
                await Task.Delay(500); // Brief delay to show success message
                EmitSignal(nameof(AuthenticationSucceeded));
                Hide();
            }
            else
            {
                UpdateStatus("Authentication failed or was cancelled.", isError: true);
                ShowManualTokenOption();
            }
        }
        catch (TaskCanceledException)
        {
            UpdateStatus("Authentication cancelled.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ClerkAuthDialog] Polling error: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
            ShowManualTokenOption();
        }
        finally
        {
            _isPolling = false;
            if (_progressBar != null)
            {
                _progressBar.Visible = false;
            }
        }
    }

    private void OnSwitchToManualPressed()
    {
        ShowManualTokenOption();
    }

    private void ShowManualTokenOption()
    {
        if (_deviceCodeContainer != null)
        {
            _deviceCodeContainer.Visible = false;
        }

        if (_manualTokenContainer != null)
        {
            _manualTokenContainer.Visible = true;
        }

        UpdateStatus("Enter your Clerk token manually:");
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
            EmitSignal(nameof(AuthenticationSucceeded));
            Hide();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ClerkAuthDialog] Failed to set manual token: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
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
    }

    public override void _ExitTree()
    {
        // Cancel polling if still active
        _pollingCancellation?.Cancel();
        _pollingCancellation?.Dispose();
    }
}

