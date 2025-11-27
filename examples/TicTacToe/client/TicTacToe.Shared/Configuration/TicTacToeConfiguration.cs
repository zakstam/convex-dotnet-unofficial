using System.Text.Json;
using Convex.Client;

namespace TicTacToe.Shared.Configuration;

/// <summary>
/// Configuration class for Tic-Tac-Toe game settings.
/// Supports loading from environment variables, appsettings.json, or direct assignment.
/// </summary>
public class TicTacToeConfiguration
{
    /// <summary>
    /// The Convex deployment URL.
    /// </summary>
    public string DeploymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable debug-level logging for the Convex client.
    /// Defaults to false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Loads configuration from environment variables.
    /// Checks CONVEX_DEPLOYMENT_URL and CONVEX_URL environment variables.
    /// </summary>
    public static TicTacToeConfiguration FromEnvironment()
    {
        var deploymentUrl = Environment.GetEnvironmentVariable("CONVEX_DEPLOYMENT_URL")
                         ?? Environment.GetEnvironmentVariable("CONVEX_URL")
                         ?? string.Empty;

        var enableDebugLogging = false;
        if (bool.TryParse(Environment.GetEnvironmentVariable("CONVEX_ENABLE_DEBUG_LOGGING"), out var debugLogging))
        {
            enableDebugLogging = debugLogging;
        }

        return new TicTacToeConfiguration
        {
            DeploymentUrl = deploymentUrl,
            EnableDebugLogging = enableDebugLogging
        };
    }

    /// <summary>
    /// Loads configuration from a JSON file (e.g., appsettings.json).
    /// Expected format: { "Convex": { "DeploymentUrl": "..." } }
    /// </summary>
    public static TicTacToeConfiguration FromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(json);

        string? deploymentUrl = null;
        var enableDebugLogging = false;

        // Try to read from "Convex" section
        if (jsonDoc.RootElement.TryGetProperty("Convex", out var convexElement))
        {
            if (convexElement.TryGetProperty(nameof(DeploymentUrl), out var urlElement))
            {
                deploymentUrl = urlElement.GetString();
            }

            // Read EnableDebugLogging from Convex section
            if (convexElement.TryGetProperty(nameof(EnableDebugLogging), out var debugLoggingElement))
            {
                if (debugLoggingElement.ValueKind is JsonValueKind.True or
                    JsonValueKind.False)
                {
                    enableDebugLogging = debugLoggingElement.GetBoolean();
                }
            }
        }

        // Fallback: try direct "DeploymentUrl" at root
        if (string.IsNullOrWhiteSpace(deploymentUrl)
            && jsonDoc.RootElement.TryGetProperty(nameof(DeploymentUrl), out var rootUrlElement))
        {
            deploymentUrl = rootUrlElement.GetString();
        }

        return new TicTacToeConfiguration
        {
            DeploymentUrl = deploymentUrl ?? string.Empty,
            EnableDebugLogging = enableDebugLogging
        };
    }

    /// <summary>
    /// Loads configuration from Microsoft.Extensions.Configuration.IConfiguration (for Blazor/ASP.NET Core).
    /// </summary>
    public static TicTacToeConfiguration FromConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var deploymentUrl = configuration["Convex:DeploymentUrl"]
                         ?? configuration[nameof(DeploymentUrl)]
                         ?? string.Empty;

        var enableDebugLogging = false;
        if (bool.TryParse(configuration["Convex:EnableDebugLogging"] ?? configuration[nameof(EnableDebugLogging)], out var debugLogging))
        {
            enableDebugLogging = debugLogging;
        }

        return new TicTacToeConfiguration
        {
            DeploymentUrl = deploymentUrl,
            EnableDebugLogging = enableDebugLogging
        };
    }

    /// <summary>
    /// Loads configuration with fallback priority:
    /// 1. Environment variables
    /// 2. appsettings.json file (if path provided and exists)
    /// 3. Default value (if provided)
    /// </summary>
    public static TicTacToeConfiguration Load(string? appsettingsPath = null, string? defaultDeploymentUrl = null)
    {
        // Try environment variables first
        var envConfig = FromEnvironment();
        if (!string.IsNullOrWhiteSpace(envConfig.DeploymentUrl))
        {
            return envConfig;
        }

        // Try appsettings.json if path provided
        if (!string.IsNullOrWhiteSpace(appsettingsPath) && File.Exists(appsettingsPath))
        {
            try
            {
                var fileConfig = FromJsonFile(appsettingsPath);
                if (!string.IsNullOrWhiteSpace(fileConfig.DeploymentUrl))
                {
                    return fileConfig;
                }
            }
            catch
            {
                // Ignore errors, fall through to default
            }
        }

        // Use default if provided
        return new TicTacToeConfiguration
        {
            DeploymentUrl = defaultDeploymentUrl ?? string.Empty,
            EnableDebugLogging = false
        };
    }

    /// <summary>
    /// Creates a ConvexClient instance from the configuration.
    /// </summary>
    public IConvexClient CreateClient()
    {
        return string.IsNullOrWhiteSpace(DeploymentUrl)
            ? throw new InvalidOperationException(
                $"{nameof(DeploymentUrl)} must be set before creating a client. " +
                "Set it directly, or use FromEnvironment(), FromJsonFile(), or Load() methods.")
            : (IConvexClient)CreateClientBuilder().Build();
    }

    /// <summary>
    /// Creates a ConvexClientBuilder instance from the configuration.
    /// </summary>
    public ConvexClientBuilder CreateClientBuilder()
    {
        if (string.IsNullOrWhiteSpace(DeploymentUrl))
        {
            throw new InvalidOperationException(
                $"{nameof(DeploymentUrl)} must be set before creating a client builder. " +
                "Set it directly, or use FromEnvironment(), FromJsonFile(), or Load() methods.");
        }

        var builder = new ConvexClientBuilder().UseDeployment(DeploymentUrl);

        if (EnableDebugLogging)
        {
            _ = builder.EnableDebugLogging(true);
        }

        return builder;
    }
}

