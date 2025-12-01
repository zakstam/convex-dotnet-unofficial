using Microsoft.Extensions.Configuration;
using Xunit;

namespace Convex.Client.Tests.Integration.Fixtures;

/// <summary>
/// Shared fixture for integration tests that provides a configured ConvexClient instance.
/// Each test run uses a unique TestRunId to isolate test data.
/// </summary>
public class ConvexFixture : IAsyncLifetime
{
    private IConvexClient? _client;

    /// <summary>
    /// Gets the shared ConvexClient instance.
    /// </summary>
    public IConvexClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// Gets the unique identifier for this test run, used to isolate test data.
    /// </summary>
    public string TestRunId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the deployment URL being used for tests.
    /// </summary>
    public string DeploymentUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Load configuration with priority: Environment > appsettings.Development.json > appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get deployment URL from environment variable or configuration
        DeploymentUrl = Environment.GetEnvironmentVariable("CONVEX_DEPLOYMENT_URL")
            ?? configuration["Convex:DeploymentUrl"]
            ?? throw new InvalidOperationException(
                "Convex deployment URL not configured. " +
                "Set CONVEX_DEPLOYMENT_URL environment variable or configure in appsettings.json");

        if (DeploymentUrl.Contains("YOUR_TEST_DEPLOYMENT"))
        {
            throw new InvalidOperationException(
                "Please configure a valid deployment URL. " +
                "Set CONVEX_DEPLOYMENT_URL environment variable or update appsettings.Development.json");
        }

        // Create the client
        _client = new ConvexClientBuilder()
            .UseDeployment(DeploymentUrl)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .Build();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            // Clean up test data for this test run
            try
            {
                await _client.Mutate<int>("testMutations:cleanup")
                    .WithArgs(new { testRunId = TestRunId })
                    .ExecuteAsync();
            }
            catch
            {
                // Ignore cleanup errors - the test backend might not be deployed
            }

            // Dispose the client
            if (_client is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

/// <summary>
/// Collection definition for sharing the ConvexFixture across tests.
/// </summary>
[CollectionDefinition("Convex")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class ConvexTestCollection : ICollectionFixture<ConvexFixture>
{
}
