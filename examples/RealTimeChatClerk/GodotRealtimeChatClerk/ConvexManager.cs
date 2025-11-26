using Godot;
using Convex.Client;
using Convex.Client.Infrastructure.Connection;
using RealtimeChatClerk.Shared.Configuration;
using Convex.Client.Extensions.Clerk;
using Convex.Client.Extensions.Clerk.Godot;
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
    /// The Clerk token service for authentication.
    /// </summary>
    public GodotClerkTokenService? ClerkTokenService { get; internal set; }

    /// <summary>
    /// Signal emitted when connection state changes.
    /// Parameter is the ConnectionState enum value cast to int.
    /// </summary>
    [Signal]
    public delegate void ConnectionStateChangedEventHandler(int newState);

    /// <summary>
    /// Signal emitted when authentication state changes.
    /// Parameter is true if authenticated, false otherwise.
    /// </summary>
    [Signal]
    public delegate void AuthenticationStateChangedEventHandler(bool isAuthenticated);

    /// <summary>
    /// Signal emitted when Clerk is ready (initialized).
    /// </summary>
    [Signal]
    public delegate void ClerkReadyEventHandler();

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

            // Log Clerk configuration status
            GD.Print($"[ConvexManager] Config loaded - ClerkPublishableKey: {(string.IsNullOrWhiteSpace(config.ClerkPublishableKey) ? "null/empty" : string.Concat(config.ClerkPublishableKey.AsSpan(0, Math.Min(30, config.ClerkPublishableKey.Length)), "..."))}");

            // Initialize Clerk authentication if configured
            if (!string.IsNullOrWhiteSpace(config.ClerkPublishableKey) &&
                config.ClerkPublishableKey != "pk_test_YOUR_CLERK_PUBLISHABLE_KEY_HERE")
            {
                GD.Print($"[ConvexManager] Clerk publishable key found: {config.ClerkPublishableKey.Substring(0, Math.Min(20, config.ClerkPublishableKey.Length))}...");
                var clerkOptions = new ClerkOptions
                {
                    PublishableKey = config.ClerkPublishableKey,
                    TokenTemplate = config.ClerkTokenTemplate,
                    ClerkDomain = config.ClerkDomain,
                    CallbackPort = config.ClerkCallbackPort,
                    CallbackPath = config.ClerkCallbackPath,
                    OAuthClientId = config.ClerkOAuthClientId,
                    EnableTokenCaching = true,
                    TokenCacheExpiration = TimeSpan.FromMinutes(5)
                };

                ClerkTokenService = new GodotClerkTokenService(clerkOptions);
                GD.Print("[ConvexManager] ClerkTokenService created");

                // Initialize Convex client WITHOUT PreConnect to allow authentication to be configured first
                Client = config.CreateClientBuilder()
                    .Build();

                GD.Print("[ConvexManager] Client initialized with deployment: " + config.DeploymentUrl);

                // Configure Convex client with Clerk authentication BEFORE any queries
                _ = Task.Run(async () =>
                {
                    try
                    {
                        GD.Print("[ConvexManager] Task.Run started - configuring Clerk authentication...");
                        GD.Print($"[ConvexManager] Client type: {Client.GetType().Name}");
                        GD.Print($"[ConvexManager] ClerkTokenService type: {ClerkTokenService.GetType().Name}");
                        GD.Print($"[ConvexManager] About to call AddClerkAuthToConvexClientAsync...");

                        await Client.AddClerkAuthToConvexClientAsync(ClerkTokenService, clerkOptions);

                        GD.Print("[ConvexManager] AddClerkAuthToConvexClientAsync returned successfully");
                        GD.Print("[ConvexManager] Clerk authentication configured successfully");
                        // Emit signal that Clerk is ready
                        CallDeferred(nameof(OnClerkReady));
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"[ConvexManager] EXCEPTION in Task.Run: {ex.GetType().Name}: {ex.Message}");
                        GD.PrintErr($"[ConvexManager] Stack trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            GD.PrintErr($"[ConvexManager] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                        }
                        // Still emit signal so UI can show auth dialog even if config failed
                        GD.Print("[ConvexManager] Emitting ClerkReady signal despite error");
                        CallDeferred(nameof(OnClerkReady));
                    }
                });
            }
            else
            {
                GD.Print("[ConvexManager] No Clerk publishable key found in configuration");
                // Initialize Convex client without authentication
                Client = config.CreateClientBuilder()
                    .Build();

                GD.Print($"[ConvexManager] Client initialized (no auth) with deployment: {config.DeploymentUrl}");
            }

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

            // Connection will be established lazily when first query is made
            // This ensures authentication is configured before connecting
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ConvexManager] Failed to initialize: {ex.Message}");
        }
    }

    /// <summary>
    /// Called when Clerk is ready (via CallDeferred to ensure main thread).
    /// </summary>
    private void OnClerkReady()
    {
        EmitSignal("ClerkReady");
        GD.Print("[ConvexManager] Clerk ready signal emitted");
    }

    /// <summary>
    /// Called when connection state changes (via CallDeferred to ensure main thread).
    /// </summary>
    private void OnConnectionStateChanged(int stateInt)
    {
        CurrentConnectionState = (ConnectionState)stateInt;
        EmitSignal(nameof(ConnectionStateChanged), (int)CurrentConnectionState);

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
