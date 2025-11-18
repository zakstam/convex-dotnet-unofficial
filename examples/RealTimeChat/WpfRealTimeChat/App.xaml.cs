using System.IO;
using System.Reflection;
using System.Windows;
using Convex.Client;
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
            ConvexClient = config.CreateClient();
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

        base.OnExit(e);
    }
}

