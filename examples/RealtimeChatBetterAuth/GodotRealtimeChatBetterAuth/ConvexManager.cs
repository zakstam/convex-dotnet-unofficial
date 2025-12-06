using Godot;
using Convex.Client;
using Convex.Client.Infrastructure.Connection;
using Convex.BetterAuth;
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
    /// The Better Auth service for authentication.
    /// </summary>
    public IBetterAuthService? BetterAuthService { get; internal set; }

    /// <summary>
    /// The Better Auth token provider for Convex authentication.
    /// </summary>
    public BetterAuthTokenProvider? BetterAuthTokenProvider { get; internal set; }

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
    /// Signal emitted when Better Auth is ready (initialized).
    /// </summary>
    [Signal]
    public delegate void BetterAuthReadyEventHandler();

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

            // Log Better Auth configuration status
            GD.Print($"[ConvexManager] Config loaded - BetterAuthSiteUrl: {(string.IsNullOrWhiteSpace(config.BetterAuthSiteUrl) ? "null/empty" : string.Concat(config.BetterAuthSiteUrl.AsSpan(0, Math.Min(30, config.BetterAuthSiteUrl.Length)), "..."))}");

            // Initialize Convex client
            Client = config.CreateClientBuilder().Build();
            GD.Print("[ConvexManager] Client initialized with deployment: " + config.DeploymentUrl);

            // Initialize Better Auth if configured
            if (config.HasBetterAuth)
            {
                GD.Print($"[ConvexManager] Better Auth site URL found: {config.BetterAuthSiteUrl?.Substring(0, Math.Min(30, config.BetterAuthSiteUrl?.Length ?? 0))}...");

                var options = new BetterAuthOptions
                {
                    SiteUrl = config.BetterAuthSiteUrl!
                };

                // Create session storage (in-memory for Godot, can be replaced with file-based)
                var sessionStorage = new GodotSessionStorage();

                // Create HTTP client
                var httpClient = new System.Net.Http.HttpClient();

                // Create Better Auth service
                BetterAuthService = new Convex.BetterAuth.BetterAuthService(httpClient, sessionStorage, options);
                GD.Print("[ConvexManager] BetterAuthService created");

                // Create token provider
                BetterAuthTokenProvider = new Convex.BetterAuth.BetterAuthTokenProvider(BetterAuthService, httpClient, options);
                GD.Print("[ConvexManager] BetterAuthTokenProvider created");

                // Configure Convex client with Better Auth authentication
                _ = Task.Run(async () =>
                {
                    try
                    {
                        GD.Print("[ConvexManager] Task.Run started - configuring Better Auth authentication...");

                        // Try to restore existing session
                        await BetterAuthService.TryRestoreSessionAsync();

                        // Set up the token provider
                        await Client.Auth.SetAuthTokenProviderAsync(BetterAuthTokenProvider);

                        GD.Print("[ConvexManager] Better Auth authentication configured successfully");
                        GD.Print($"[ConvexManager] IsAuthenticated: {BetterAuthService.IsAuthenticated}");

                        // Emit signal that Better Auth is ready
                        CallDeferred(nameof(OnBetterAuthReady));
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
                        GD.Print("[ConvexManager] Emitting BetterAuthReady signal despite error");
                        CallDeferred(nameof(OnBetterAuthReady));
                    }
                });
            }
            else
            {
                GD.Print("[ConvexManager] No Better Auth site URL found in configuration");
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
    /// Called when Better Auth is ready (via CallDeferred to ensure main thread).
    /// </summary>
    private void OnBetterAuthReady()
    {
        EmitSignal("BetterAuthReady");
        GD.Print("[ConvexManager] Better Auth ready signal emitted");
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

        if (BetterAuthService is IDisposable authDisposable)
        {
            authDisposable.Dispose();
            GD.Print("[ConvexManager] BetterAuthService disposed");
        }

        _instance = null;
    }
}

/// <summary>
/// Godot-specific session storage using ConfigFile for persistence.
/// </summary>
public class GodotSessionStorage : ISessionStorage
{
    private const string ConfigPath = "user://better_auth_session.cfg";
    private string? _cachedToken;

    public Task StoreTokenAsync(string token)
    {
        _cachedToken = token;
        var config = new ConfigFile();
        config.SetValue("auth", "token", token);
        config.Save(ConfigPath);
        GD.Print("[GodotSessionStorage] Token stored");
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
    {
        if (_cachedToken != null)
        {
            return Task.FromResult<string?>(_cachedToken);
        }

        var config = new ConfigFile();
        if (config.Load(ConfigPath) == Error.Ok)
        {
            _cachedToken = config.GetValue("auth", "token").AsString();
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                GD.Print("[GodotSessionStorage] Token loaded from storage");
                return Task.FromResult<string?>(_cachedToken);
            }
        }
        return Task.FromResult<string?>(null);
    }

    public Task RemoveTokenAsync()
    {
        _cachedToken = null;
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(ConfigPath));
        GD.Print("[GodotSessionStorage] Token removed");
        return Task.CompletedTask;
    }
}
