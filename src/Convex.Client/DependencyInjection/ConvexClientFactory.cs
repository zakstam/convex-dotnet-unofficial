using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Convex.Client.Extensions.DependencyInjection;

/// <summary>
/// Default implementation of IConvexClientFactory that creates and caches named client instances.
/// </summary>
internal sealed class ConvexClientFactory(
    IOptionsMonitor<ConvexOptions> optionsMonitor,
    ILoggerFactory? loggerFactory = null) : IConvexClientFactory, IDisposable
{
    private readonly IOptionsMonitor<ConvexOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILoggerFactory? _loggerFactory = loggerFactory;
    private readonly ConcurrentDictionary<string, IConvexClient> _clients = new();
    private bool _disposed;

    /// <inheritdoc />
    public IConvexClient CreateClient(string? name = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ConvexClientFactory));
        }

        var clientName = name ?? string.Empty;

        return _clients.GetOrAdd(clientName, n =>
        {
            var options = _optionsMonitor.Get(n);
            options.Validate();

            var builder = new ConvexClientBuilder()
                .UseDeployment(options.DeploymentUrl!);

            // Configure logging if enabled
            if (options.EnableLogging && _loggerFactory != null)
            {
                var logger = _loggerFactory.CreateLogger<ConvexClient>();
                _ = builder.WithLogging(logger);
            }

            // Configure debug logging if enabled
            if (options.EnableDebugLogging)
            {
                _ = builder.EnableDebugLogging(true);
            }

            // Configure auto-reconnect if enabled
            if (options.EnableAutoReconnect)
            {
                _ = builder.WithAutoReconnect(
                    maxAttempts: options.MaxReconnectAttempts,
                    delayMs: options.ReconnectDelayMs);
            }

            // Apply custom builder configuration if provided
            options.ConfigureBuilder?.Invoke(builder);

            return builder.Build();
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var client in _clients.Values)
        {
            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _clients.Clear();
    }
}
