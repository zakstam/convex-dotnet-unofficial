using System.IO;
using System.Reflection;
using System.Windows;
using Convex.Client;
using Microsoft.Extensions.Logging;
using RealtimeChat.Shared.Configuration;

namespace WpfRealTimeChat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Global Convex client instance.
    /// </summary>
    public static IConvexClient ConvexClient { get; private set; } = null!;

    /// <summary>
    /// Global chat configuration.
    /// </summary>
    public static ChatConfiguration ChatConfig { get; private set; } = null!;

    /// <summary>
    /// Logger factory for debug logging.
    /// </summary>
    private static ILoggerFactory? _loggerFactory;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Load configuration from environment variables, shared appsettings.json, or use default
            // Priority: Environment variables > shared appsettings.json > default
            // Look for shared appsettings.json in the RealTimeChat directory (relative to solution)
            var executableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            var sharedAppSettingsPath = Path.GetFullPath(
                Path.Combine(executableDir, "..", "..", "..", "..", "appsettings.json"));

            var config = ChatConfiguration.Load(
                appsettingsPath: sharedAppSettingsPath,
                defaultDeploymentUrl: "https://your-deployment.convex.cloud");

            ChatConfig = config;

            // Create logger factory for debug output (visible in Visual Studio Output window)
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddDebug(); // Outputs to Debug window in Visual Studio
            });

            var logger = _loggerFactory.CreateLogger<ConvexClient>();

            // Create client with logging enabled
            ConvexClient = config.CreateClientBuilder()
                .WithLogging(logger)
                .EnableDebugLogging(true)
                .Build();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize Convex client: {ex.Message}\n\n" +
                "Please set the deployment URL using one of these methods:\n" +
                "1. Set CONVEX_DEPLOYMENT_URL or CONVEX_URL environment variable\n" +
                "2. Update examples/RealTimeChat/appsettings.json\n" +
                "3. Update the default value in App.xaml.cs",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose Convex client if it implements IDisposable
        if (ConvexClient is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Dispose logger factory
        _loggerFactory?.Dispose();

        base.OnExit(e);
    }
}

