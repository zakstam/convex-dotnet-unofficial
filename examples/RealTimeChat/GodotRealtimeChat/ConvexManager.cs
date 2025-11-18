using Godot;
using Convex.Client;
using Convex.Client.Shared.Connection;
using RealtimeChat.Shared.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GodotRealtimeChat;

/// <summary>
/// Global ConvexClient manager as an autoload singleton.
/// Handles initialization, lifecycle, and global access to the Convex client.
/// </summary>
public partial class ConvexManager : Node
{
    private static ConvexManager? _instance;

    /// <summary>
    /// The singleton instance of ConvexManager.
    /// </summary>
    public static ConvexManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GD.PrintErr("ConvexManager not initialized. Add it as an autoload in project settings.");
            }
            return _instance!;
        }
    }

    /// <summary>
    /// The global Convex client instance.
    /// </summary>
    public IConvexClient Client { get; private set; } = null!;

    /// <summary>
    /// The chat configuration.
    /// </summary>
    public ChatConfiguration ChatConfig { get; private set; } = null!;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public ConnectionState CurrentConnectionState { get; private set; } = ConnectionState.Disconnected;

    /// <summary>
    /// Signal emitted when connection state changes.
    /// Parameter is the ConnectionState enum value cast to int.
    /// </summary>
    [Signal]
    public delegate void ConnectionStateChangedEventHandler(int newState);

    public override void _Ready()
    {
        _instance = this;

        try
        {
            // Load configuration from environment variables, shared appsettings.json, or use default
            // Priority: Environment variables > shared appsettings.json > default
            // Try multiple paths to find the shared appsettings.json
            string? sharedAppSettingsPath = null;

            // Try 1: Relative to project directory (when running from editor)
            var projectPath = ProjectSettings.GlobalizePath("res://");
            if (!string.IsNullOrEmpty(projectPath))
            {
                var projectDir = Path.GetDirectoryName(projectPath);
                var candidatePath = Path.GetFullPath(Path.Combine(projectDir ?? "", "..", "appsettings.json"));
                if (File.Exists(candidatePath))
                {
                    sharedAppSettingsPath = candidatePath;
                    GD.Print($"[ConvexManager] Found appsettings.json at: {sharedAppSettingsPath}");
                }
            }

            // Try 2: Relative to executable (when running exported game)
            if (sharedAppSettingsPath == null || !File.Exists(sharedAppSettingsPath))
            {
                var executablePath = OS.GetExecutablePath();
                if (!string.IsNullOrEmpty(executablePath))
                {
                    var executableDir = Path.GetDirectoryName(executablePath);
                    var candidatePath = Path.GetFullPath(Path.Combine(executableDir ?? "", "..", "..", "..", "..", "appsettings.json"));
                    if (File.Exists(candidatePath))
                    {
                        sharedAppSettingsPath = candidatePath;
                        GD.Print($"[ConvexManager] Found appsettings.json at: {sharedAppSettingsPath}");
                    }
                }
            }

            // Try 3: Try from current working directory
            if (sharedAppSettingsPath == null || !File.Exists(sharedAppSettingsPath))
            {
                var candidatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "appsettings.json"));
                if (File.Exists(candidatePath))
                {
                    sharedAppSettingsPath = candidatePath;
                    GD.Print($"[ConvexManager] Found appsettings.json at: {sharedAppSettingsPath}");
                }
            }

            if (sharedAppSettingsPath == null || !File.Exists(sharedAppSettingsPath))
            {
                GD.Print($"[ConvexManager] appsettings.json not found, using environment variables or default");
            }

            var config = ChatConfiguration.Load(
                appsettingsPath: sharedAppSettingsPath,
                defaultDeploymentUrl: "https://your-deployment.convex.cloud");

            ChatConfig = config;

            // Initialize Convex client with PreConnect enabled to establish connection immediately
            Client = config.CreateClientBuilder()
                .PreConnect() // Enable pre-connection to establish WebSocket immediately
                .Build();

            GD.Print($"[ConvexManager] Client initialized with deployment: {config.DeploymentUrl}");

            // Monitor connection state changes
            Client.ConnectionStateChanges.Subscribe(
                state =>
                {
                    CallDeferred(nameof(OnConnectionStateChanged), (int)state);
                },
                error =>
                {
                    GD.PrintErr($"[ConvexManager] Connection state error: {error}");
                }
            );

            GD.Print("[ConvexManager] Event listeners registered");

            // Explicitly ensure connection is established
            _ = Task.Run(async () =>
            {
                try
                {
                    if (Client is ConvexClient convexClient)
                    {
                        await convexClient.EnsureConnectedAsync();
                        GD.Print("[ConvexManager] Connection explicitly established");
                    }
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[ConvexManager] Failed to establish connection: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ConvexManager] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when connection state changes (via CallDeferred to ensure main thread).
    /// </summary>
    private void OnConnectionStateChanged(int stateInt)
    {
        CurrentConnectionState = (ConnectionState)stateInt;
        EmitSignal(SignalName.ConnectionStateChanged, (int)CurrentConnectionState);

        GD.Print($"[ConvexManager] Connection state changed: {CurrentConnectionState}");
    }

    public override void _ExitTree()
    {
        // Cleanup
        if (Client is IDisposable disposable)
        {
            disposable.Dispose();
            GD.Print("[ConvexManager] Client disposed");
        }

        _instance = null;
    }
}
