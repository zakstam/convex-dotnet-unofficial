using Godot;

namespace GodotRealtimeChat;

public partial class Toast : PanelContainer
{
    // UI References
    private Label? _iconLabel;
    private Label? _messageLabel;

    // Toast colors
    private static readonly Color SuccessColor = new(0.2f, 0.8f, 0.4f);
    private static readonly Color ErrorColor = new(0.9f, 0.3f, 0.3f);
    private static readonly Color InfoColor = new(0.3f, 0.7f, 0.9f);
    private static readonly Color SurfaceDark = new(0.15f, 0.15f, 0.2f);

    public enum ToastType
    {
        Success,
        Error,
        Info
    }

    public override void _Ready()
    {
        // Get references
        _iconLabel = GetNode<Label>("%IconLabel");
        _messageLabel = GetNode<Label>("%MessageLabel");
    }

    public void Show(string message, ToastType type = ToastType.Info, float duration = 3.0f)
    {
        // Set message and icon based on type
        switch (type)
        {
            case ToastType.Success:
                _iconLabel!.Text = "✓";
                SetupStyle(SuccessColor);
                break;
            case ToastType.Error:
                _iconLabel!.Text = "❌";
                SetupStyle(ErrorColor);
                break;
            case ToastType.Info:
                _iconLabel!.Text = "ℹ️";
                SetupStyle(InfoColor);
                break;
        }

        _messageLabel!.Text = message;
        _messageLabel?.AddThemeColorOverride("font_color", Colors.White);
        _iconLabel?.AddThemeColorOverride("font_color", Colors.White);

        // Set initial position (below viewport)
        Position = new Vector2(20, -80);

        // Animate toast entrance and exit
        var tween = CreateTween();
        _ = tween.TweenProperty(this, "position:y", 20, 0.3)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _ = tween.TweenInterval(duration);
        _ = tween.TweenProperty(this, "modulate:a", 0, 0.3);
        _ = tween.TweenCallback(Callable.From(QueueFree));
    }

    public void ShowCustom(string message, Color bgColor, string icon, float duration = 3.0f)
    {
        _iconLabel!.Text = icon;
        _messageLabel!.Text = message;

        _messageLabel?.AddThemeColorOverride("font_color", Colors.White);
        _iconLabel?.AddThemeColorOverride("font_color", Colors.White);

        SetupStyle(bgColor);

        // Set initial position (below viewport)
        Position = new Vector2(20, -80);

        // Animate toast entrance and exit
        var tween = CreateTween();
        _ = tween.TweenProperty(this, "position:y", 20, 0.3)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _ = tween.TweenInterval(duration);
        _ = tween.TweenProperty(this, "modulate:a", 0, 0.3);
        _ = tween.TweenCallback(Callable.From(QueueFree));
    }

    private void SetupStyle(Color bgColor)
    {
        var style = new StyleBoxFlat();
        style.BgColor = bgColor;
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 12;
        style.ContentMarginBottom = 12;
        style.ShadowColor = new Color(0, 0, 0, 0.4f);
        style.ShadowSize = 4;
        AddThemeStyleboxOverride("panel", style);
    }
}
