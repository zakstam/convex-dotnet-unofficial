using System;

namespace Convex.Client.Extensions.DependencyInjection;

/// <summary>
/// Configuration options for Convex client.
/// </summary>
public class ConvexOptions
{
    /// <summary>
    /// The Convex deployment URL (e.g., "https://happy-animal-123.convex.cloud").
    /// </summary>
    public string? DeploymentUrl { get; set; }

    /// <summary>
    /// Enable automatic reconnection on connection loss. Default is true.
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum number of reconnection attempts. Default is 5.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    /// Delay in milliseconds between reconnection attempts. Default is 1000ms.
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable logging integration. Default is true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable debug-level logging. When enabled, debug-level logs will be emitted if logging is enabled and a logger factory is available.
    /// Debug logs include detailed information about requests, responses, and internal operations.
    /// Default is false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Custom configuration action for the ConvexClientBuilder.
    /// Allows advanced configuration beyond the standard options.
    /// </summary>
    public Action<ConvexClientBuilder>? ConfigureBuilder { get; set; }

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(DeploymentUrl))
        {
            throw new InvalidOperationException(
                "DeploymentUrl must be configured. " +
                "Set it via options.DeploymentUrl or in configuration.");
        }

        if (MaxReconnectAttempts < 0)
        {
            throw new InvalidOperationException(
                $"MaxReconnectAttempts must be >= 0, got {MaxReconnectAttempts}.");
        }

        if (ReconnectDelayMs < 0)
        {
            throw new InvalidOperationException(
                $"ReconnectDelayMs must be >= 0, got {ReconnectDelayMs}.");
        }
    }
}
