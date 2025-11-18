using System.Text.Json;
using Convex.Client;

namespace RealtimeChatClerk.Shared.Configuration;

/// <summary>
/// Configuration class for chat application settings.
/// Supports loading from environment variables, appsettings.json, or direct assignment.
/// </summary>
public class ChatConfiguration
{
    /// <summary>
    /// The Convex deployment URL.
    /// </summary>
    public string DeploymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// The initial number of messages to load when opening the chat.
    /// Defaults to 10 if not specified.
    /// </summary>
    public int InitialMessageLimit { get; set; } = 10;

    /// <summary>
    /// Whether to enable debug-level logging for the Convex client.
    /// Defaults to false.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Clerk publishable key for authentication (optional).
    /// </summary>
    public string? ClerkPublishableKey { get; set; }

    /// <summary>
    /// Clerk token template name (default: "convex").
    /// </summary>
    public string ClerkTokenTemplate { get; set; } = "convex";

    /// <summary>
    /// Clerk domain (Frontend API URL without https://).
    /// Example: "your-instance.clerk.accounts.dev"
    /// </summary>
    public string? ClerkDomain { get; set; }

    /// <summary>
    /// Port for OAuth callback server (default: 8080).
    /// </summary>
    public int ClerkCallbackPort { get; set; } = 8080;

    /// <summary>
    /// Path for OAuth callback (default: "/callback").
    /// </summary>
    public string ClerkCallbackPath { get; set; } = "/callback";

    /// <summary>
    /// OAuth Client ID from Clerk Dashboard OAuth Application.
    /// Required for OAuth Authorization Code Flow in desktop applications.
    /// Different from ClerkPublishableKey - this is specifically for OAuth flows.
    /// </summary>
    public string? ClerkOAuthClientId { get; set; }

    /// <summary>
    /// Loads configuration from environment variables.
    /// Checks CONVEX_DEPLOYMENT_URL and CONVEX_URL environment variables.
    /// </summary>
    public static ChatConfiguration FromEnvironment()
    {
        var deploymentUrl = Environment.GetEnvironmentVariable("CONVEX_DEPLOYMENT_URL")
                         ?? Environment.GetEnvironmentVariable("CONVEX_URL")
                         ?? string.Empty;

        var initialMessageLimit = 10;
        if (int.TryParse(Environment.GetEnvironmentVariable("CONVEX_INITIAL_MESSAGE_LIMIT"), out var limit))
        {
            initialMessageLimit = limit;
        }

        var enableDebugLogging = false;
        if (bool.TryParse(Environment.GetEnvironmentVariable("CONVEX_ENABLE_DEBUG_LOGGING"), out var debugLogging))
        {
            enableDebugLogging = debugLogging;
        }

        var clerkPublishableKey = Environment.GetEnvironmentVariable("CLERK_PUBLISHABLE_KEY");
        var clerkTokenTemplate = Environment.GetEnvironmentVariable("CLERK_TOKEN_TEMPLATE") ?? "convex";

        return new ChatConfiguration
        {
            DeploymentUrl = deploymentUrl,
            InitialMessageLimit = initialMessageLimit,
            EnableDebugLogging = enableDebugLogging,
            ClerkPublishableKey = clerkPublishableKey,
            ClerkTokenTemplate = clerkTokenTemplate
        };
    }

    /// <summary>
    /// Loads configuration from a JSON file (e.g., appsettings.json).
    /// Expected format: { "Convex": { "DeploymentUrl": "..." } }
    /// </summary>
    public static ChatConfiguration FromJsonFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(json);

        string? deploymentUrl = null;

        int initialMessageLimit = 10;
        bool enableDebugLogging = false;

        // Clerk configuration
        string? clerkPublishableKey = null;
        string clerkTokenTemplate = "convex";
        string? clerkDomain = null;
        int clerkCallbackPort = 8080;
        string clerkCallbackPath = "/callback";
        string? clerkOAuthClientId = null;

        // Try to read from "Convex:DeploymentUrl" path
        if (jsonDoc.RootElement.TryGetProperty("Convex", out var convexElement))
        {
            if (convexElement.TryGetProperty(nameof(DeploymentUrl), out var urlElement))
            {
                deploymentUrl = urlElement.GetString();
            }

            // Read InitialMessageLimit from Convex section
            if (convexElement.TryGetProperty(nameof(InitialMessageLimit), out var limitElement))
            {
                if (limitElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    initialMessageLimit = limitElement.GetInt32();
                }
            }

            // Read EnableDebugLogging from Convex section
            if (convexElement.TryGetProperty(nameof(EnableDebugLogging), out var debugLoggingElement))
            {
                if (debugLoggingElement.ValueKind == System.Text.Json.JsonValueKind.True ||
                    debugLoggingElement.ValueKind == System.Text.Json.JsonValueKind.False)
                {
                    enableDebugLogging = debugLoggingElement.GetBoolean();
                }
            }
        }

        // Read Clerk configuration
        if (jsonDoc.RootElement.TryGetProperty("Clerk", out var clerkElement))
        {
            if (clerkElement.TryGetProperty("PublishableKey", out var publishableKeyElement))
            {
                clerkPublishableKey = publishableKeyElement.GetString();
            }

            if (clerkElement.TryGetProperty("TokenTemplate", out var tokenTemplateElement))
            {
                var template = tokenTemplateElement.GetString();
                if (!string.IsNullOrEmpty(template))
                {
                    clerkTokenTemplate = template;
                }
            }

            if (clerkElement.TryGetProperty(nameof(ClerkDomain), out var domainElement))
            {
                clerkDomain = domainElement.GetString();
            }

            if (clerkElement.TryGetProperty("CallbackPort", out var portElement))
            {
                if (portElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    clerkCallbackPort = portElement.GetInt32();
                }
            }

            if (clerkElement.TryGetProperty("CallbackPath", out var pathElement))
            {
                var path = pathElement.GetString();
                if (!string.IsNullOrEmpty(path))
                {
                    clerkCallbackPath = path;
                }
            }

            if (clerkElement.TryGetProperty("OAuthClientId", out var oauthClientIdElement))
            {
                clerkOAuthClientId = oauthClientIdElement.GetString();
            }
        }

        // Fallback: try direct "DeploymentUrl" at root
        if (string.IsNullOrWhiteSpace(deploymentUrl)
            && jsonDoc.RootElement.TryGetProperty(nameof(DeploymentUrl), out var rootUrlElement))
        {
            deploymentUrl = rootUrlElement.GetString();
        }

        return new ChatConfiguration
        {
            DeploymentUrl = deploymentUrl ?? string.Empty,
            InitialMessageLimit = initialMessageLimit,
            EnableDebugLogging = enableDebugLogging,
            ClerkPublishableKey = clerkPublishableKey,
            ClerkTokenTemplate = clerkTokenTemplate,
            ClerkDomain = clerkDomain,
            ClerkCallbackPort = clerkCallbackPort,
            ClerkCallbackPath = clerkCallbackPath,
            ClerkOAuthClientId = clerkOAuthClientId
        };
    }

    /// <summary>
    /// Loads configuration from Microsoft.Extensions.Configuration.IConfiguration (for Blazor/ASP.NET Core).
    /// </summary>
    public static ChatConfiguration FromConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var deploymentUrl = configuration["Convex:DeploymentUrl"]
                         ?? configuration[nameof(DeploymentUrl)]
                         ?? string.Empty;

        var initialMessageLimit = 10;
        if (int.TryParse(configuration["Convex:InitialMessageLimit"] ?? configuration[nameof(InitialMessageLimit)], out var limit))
        {
            initialMessageLimit = limit;
        }

        var enableDebugLogging = false;
        if (bool.TryParse(configuration["Convex:EnableDebugLogging"] ?? configuration[nameof(EnableDebugLogging)], out var debugLogging))
        {
            enableDebugLogging = debugLogging;
        }

        var clerkPublishableKey = configuration["Clerk:PublishableKey"];
        var clerkTokenTemplate = configuration["Clerk:TokenTemplate"] ?? "convex";

        return new ChatConfiguration
        {
            DeploymentUrl = deploymentUrl,
            InitialMessageLimit = initialMessageLimit,
            EnableDebugLogging = enableDebugLogging,
            ClerkPublishableKey = clerkPublishableKey,
            ClerkTokenTemplate = clerkTokenTemplate
        };
    }

    /// <summary>
    /// Loads configuration with fallback priority:
    /// 1. Environment variables
    /// 2. appsettings.json file (if path provided and exists)
    /// 3. Default value (if provided)
    /// </summary>
    public static ChatConfiguration Load(string? appsettingsPath = null, string? defaultDeploymentUrl = null)
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
        return new ChatConfiguration
        {
            DeploymentUrl = defaultDeploymentUrl ?? string.Empty,
            InitialMessageLimit = 10
        };
    }

    /// <summary>
    /// Creates a ConvexClient instance from the configuration.
    /// </summary>
    public IConvexClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(DeploymentUrl))
        {
            throw new InvalidOperationException(
                $"{nameof(DeploymentUrl)} must be set before creating a client. " +
                "Set it directly, or use FromEnvironment(), FromJsonFile(), or Load() methods.");
        }

        return CreateClientBuilder().Build();
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
            builder.EnableDebugLogging(true);
        }

        return builder;
    }
}

