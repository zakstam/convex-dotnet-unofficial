namespace RealtimeChat.Frontend.Services;

/// <summary>
/// Holds configuration validation state for the application.
/// </summary>
public class ConfigurationState
{
    public bool IsValid { get; set; } = true;
    public List<ConfigurationError> Errors { get; set; } = [];
}

public record ConfigurationError(string Title, string Description, string ConfigExample, string DashboardUrl, string DashboardName);
