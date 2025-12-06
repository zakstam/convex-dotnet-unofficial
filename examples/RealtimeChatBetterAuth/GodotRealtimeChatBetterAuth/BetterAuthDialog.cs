using Godot;
using Convex.BetterAuth;
using Convex.BetterAuth.Models;
using System;
using System.Threading.Tasks;

namespace GodotRealtimeChat;

/// <summary>
/// Godot dialog for Better Auth authentication using email/password.
/// </summary>
public partial class BetterAuthDialog : AcceptDialog
{
    private IBetterAuthService? _authService;

    // UI References
    private Label? _statusLabel;
    private LineEdit? _emailInput;
    private LineEdit? _passwordInput;
    private LineEdit? _nameInput;
    private Button? _signInButton;
    private Button? _signUpButton;
    private Button? _switchModeButton;
    private ProgressBar? _progressBar;
    private VBoxContainer? _nameContainer;

    private bool _isAuthenticating = false;
    private bool _isSignUpMode = false;

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
        // Find UI nodes
        _statusLabel = GetNodeOrNull<Label>("%StatusLabel");
        _emailInput = GetNodeOrNull<LineEdit>("%EmailInput");
        _passwordInput = GetNodeOrNull<LineEdit>("%PasswordInput");
        _nameInput = GetNodeOrNull<LineEdit>("%NameInput");
        _signInButton = GetNodeOrNull<Button>("%SignInButton");
        _signUpButton = GetNodeOrNull<Button>("%SignUpButton");
        _switchModeButton = GetNodeOrNull<Button>("%SwitchModeButton");
        _progressBar = GetNodeOrNull<ProgressBar>("%ProgressBar");
        _nameContainer = GetNodeOrNull<VBoxContainer>("%NameContainer");

        // Connect button signals
        if (_signInButton != null)
        {
            _signInButton.Pressed += OnSignInPressed;
        }

        if (_signUpButton != null)
        {
            _signUpButton.Pressed += OnSignUpPressed;
        }

        if (_switchModeButton != null)
        {
            _switchModeButton.Pressed += OnSwitchModePressed;
        }

        // Setup password field
        if (_passwordInput != null)
        {
            _passwordInput.Secret = true;
        }

        // Hide progress bar initially
        if (_progressBar != null)
        {
            _progressBar.Visible = false;
        }

        // Hide name field initially (sign in mode)
        if (_nameContainer != null)
        {
            _nameContainer.Visible = false;
        }

        // Set up dialog
        Title = "Sign In";
        SetText("Please sign in to continue.");

        // Set initial status
        UpdateStatus("Enter your email and password");
        UpdateModeUI();
    }

    /// <summary>
    /// Initializes the dialog with a Better Auth service.
    /// </summary>
    public void Initialize(IBetterAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        UpdateStatus("Ready to sign in");
    }

    private async void OnSignInPressed()
    {
        if (_authService == null || _isAuthenticating)
        {
            return;
        }

        var email = _emailInput?.Text.Trim() ?? "";
        var password = _passwordInput?.Text ?? "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password", isError: true);
            return;
        }

        await PerformAuthAsync(async () =>
        {
            var result = await _authService.SignInAsync(email, password);
            return result;
        }, "Signing in...", "Sign in successful!");
    }

    private async void OnSignUpPressed()
    {
        if (_authService == null || _isAuthenticating)
        {
            return;
        }

        var email = _emailInput?.Text.Trim() ?? "";
        var password = _passwordInput?.Text ?? "";
        var name = _nameInput?.Text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password", isError: true);
            return;
        }

        await PerformAuthAsync(async () =>
        {
            var result = await _authService.SignUpAsync(email, password, name);
            return result;
        }, "Creating account...", "Account created successfully!");
    }

    private async Task PerformAuthAsync(Func<Task<AuthResult>> authAction, string progressMessage, string successMessage)
    {
        _isAuthenticating = true;

        try
        {
            // Disable buttons
            SetButtonsEnabled(false);

            // Show progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = true;
                var tween = CreateTween();
                tween.SetLoops();
                tween.TweenProperty(_progressBar, "value", 100.0, 2.0);
            }

            UpdateStatus(progressMessage);
            await Task.Delay(100); // Brief delay to update UI

            var result = await authAction();

            if (result.IsSuccess)
            {
                UpdateStatus(successMessage);
                await Task.Delay(500); // Brief delay to show success message
                EmitSignal(SignalName.AuthenticationSucceeded);
                Hide();
            }
            else
            {
                UpdateStatus(result.ErrorMessage ?? "Authentication failed", isError: true);
                EmitSignal(SignalName.AuthenticationFailed, result.ErrorMessage ?? "Authentication failed");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[BetterAuthDialog] Authentication error: {ex.Message}");
            UpdateStatus($"Error: {ex.Message}", isError: true);
            EmitSignal(SignalName.AuthenticationFailed, ex.Message);
        }
        finally
        {
            _isAuthenticating = false;

            // Re-enable buttons
            SetButtonsEnabled(true);

            // Hide progress bar
            if (_progressBar != null)
            {
                _progressBar.Visible = false;
            }
        }
    }

    private void OnSwitchModePressed()
    {
        _isSignUpMode = !_isSignUpMode;
        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        if (_isSignUpMode)
        {
            Title = "Create Account";
            SetText("Create a new account to continue.");
            if (_signInButton != null) _signInButton.Visible = false;
            if (_signUpButton != null) _signUpButton.Visible = true;
            if (_switchModeButton != null) _switchModeButton.Text = "Already have an account? Sign In";
            if (_nameContainer != null) _nameContainer.Visible = true;
            UpdateStatus("Enter your details to create an account");
        }
        else
        {
            Title = "Sign In";
            SetText("Please sign in to continue.");
            if (_signInButton != null) _signInButton.Visible = true;
            if (_signUpButton != null) _signUpButton.Visible = false;
            if (_switchModeButton != null) _switchModeButton.Text = "Don't have an account? Sign Up";
            if (_nameContainer != null) _nameContainer.Visible = false;
            UpdateStatus("Enter your email and password");
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (_signInButton != null) _signInButton.Disabled = !enabled;
        if (_signUpButton != null) _signUpButton.Disabled = !enabled;
        if (_switchModeButton != null) _switchModeButton.Disabled = !enabled;
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

        GD.Print($"[BetterAuthDialog] {message}");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            EmitSignal(SignalName.AuthenticationFailed, "Dialog closed by user");
            QueueFree();
        }
    }
}
