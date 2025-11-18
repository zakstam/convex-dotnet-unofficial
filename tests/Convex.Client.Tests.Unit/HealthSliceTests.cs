using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Connection;
using Convex.Client.Shared.ErrorHandling;
using Convex.Client.Slices.Health;
using Xunit;

namespace Convex.Client.Tests.Unit;


public class HealthSliceTests
{
    private HealthSlice _healthSlice = null!;

    public HealthSliceTests()
    {
        _healthSlice = new HealthSlice();
    }

    #region HealthSlice Entry Point Tests

    [Fact]
    public void HealthSlice_InitialState_HasZeroMetrics()
    {
        // Assert
        Assert.Equal(0, _healthSlice.GetMessagesReceived());
        Assert.Equal(0, _healthSlice.GetMessagesSent());
        Assert.Equal(0, _healthSlice.GetReconnectionCount());
        Assert.Null(_healthSlice.GetAverageLatency());
        Assert.Null(_healthSlice.GetTimeSinceLastMessage());
        Assert.Null(_healthSlice.GetConnectionUptime());
        Assert.Empty(_healthSlice.GetRecentErrors());
    }

    [Fact]
    public void HealthSlice_RecordMessageReceived_IncrementsCounter()
    {
        // Act
        _healthSlice.RecordMessageReceived();
        _healthSlice.RecordMessageReceived();

        // Assert
        Assert.Equal(2, _healthSlice.GetMessagesReceived());
    }

    [Fact]
    public void HealthSlice_RecordMessageSent_IncrementsCounter()
    {
        // Act
        _healthSlice.RecordMessageSent();
        _healthSlice.RecordMessageSent();
        _healthSlice.RecordMessageSent();

        // Assert
        Assert.Equal(3, _healthSlice.GetMessagesSent());
    }

    [Fact]
    public void HealthSlice_RecordLatency_TracksLatency()
    {
        // Act
        _healthSlice.RecordLatency(10.5);
        _healthSlice.RecordLatency(20.0);
        _healthSlice.RecordLatency(15.5);

        // Assert
        var avgLatency = _healthSlice.GetAverageLatency();
        Assert.NotNull(avgLatency);
        Assert.Equal(15.33, avgLatency.Value, 0.1);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HealthSlice_RecordLatency_WithNoSamples_ReturnsNull()
    {
        // Assert
        Assert.Null(_healthSlice.GetAverageLatency());
    }

    [Fact]
    public void HealthSlice_RecordReconnection_IncrementsCounter()
    {
        // Act
        _healthSlice.RecordReconnection();
        _healthSlice.RecordReconnection();

        // Assert
        Assert.Equal(2, _healthSlice.GetReconnectionCount());
    }

    [Fact]
    public void HealthSlice_RecordConnectionEstablished_SetsConnectionTime()
    {
        // Act
        _healthSlice.RecordConnectionEstablished();

        // Assert
        var uptime = _healthSlice.GetConnectionUptime();
        Assert.NotNull(uptime);
        Assert.True(uptime.Value.TotalMilliseconds >= 0);
    }

    [Fact]
    public void HealthSlice_RecordError_AddsError()
    {
        // Arrange
        var error1 = new InvalidOperationException("Error 1");
        var error2 = new InvalidOperationException("Error 2");

        // Act
        _healthSlice.RecordError(error1);
        _healthSlice.RecordError(error2);

        // Assert
        var errors = _healthSlice.GetRecentErrors();
        Assert.Equal(2, errors.Count);
        Assert.Contains(error1, errors);
        Assert.Contains(error2, errors);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HealthSlice_RecordError_WithManyErrors_LimitsToMaxRecentErrors()
    {
        // Arrange - Add more than max recent errors (default is 10)
        for (int i = 0; i < 15; i++)
        {
            _healthSlice.RecordError(new InvalidOperationException($"Error {i}"));
        }

        // Assert - Should only keep the most recent 10
        var errors = _healthSlice.GetRecentErrors();
        Assert.True(errors.Count <= 10);
    }

    [Fact]
    public void HealthSlice_GetTimeSinceLastMessage_AfterRecordingMessage_ReturnsTimeSpan()
    {
        // Act
        _healthSlice.RecordMessageReceived();
        Thread.Sleep(10); // Small delay to ensure time difference

        // Assert
        var timeSince = _healthSlice.GetTimeSinceLastMessage();
        Assert.NotNull(timeSince);
        Assert.True(timeSince.Value.TotalMilliseconds >= 0);
    }

    [Fact]
    public void HealthSlice_GetTimeSinceLastMessage_WithNoMessages_ReturnsNull()
    {
        // Assert
        Assert.Null(_healthSlice.GetTimeSinceLastMessage());
    }

    [Fact]
    public void HealthSlice_GetConnectionUptime_AfterConnectionEstablished_ReturnsTimeSpan()
    {
        // Act
        _healthSlice.RecordConnectionEstablished();
        Thread.Sleep(10); // Small delay

        // Assert
        var uptime = _healthSlice.GetConnectionUptime();
        Assert.NotNull(uptime);
        Assert.True(uptime.Value.TotalMilliseconds >= 0);
    }

    [Fact]
    public void HealthSlice_GetConnectionUptime_WithNoConnection_ReturnsNull()
    {
        // Assert
        Assert.Null(_healthSlice.GetConnectionUptime());
    }

    [Fact]
    public void HealthSlice_Reset_ClearsAllMetrics()
    {
        // Arrange
        _healthSlice.RecordMessageReceived();
        _healthSlice.RecordMessageSent();
        _healthSlice.RecordLatency(10.0);
        _healthSlice.RecordReconnection();
        _healthSlice.RecordError(new InvalidOperationException("Test error"));

        // Act
        _healthSlice.Reset();

        // Assert
        Assert.Equal(0, _healthSlice.GetMessagesReceived());
        Assert.Equal(0, _healthSlice.GetMessagesSent());
        Assert.Equal(0, _healthSlice.GetReconnectionCount());
        Assert.Null(_healthSlice.GetAverageLatency());
        Assert.Empty(_healthSlice.GetRecentErrors());
    }

    [Fact]
    public void HealthSlice_CreateHealthCheck_WithConnectedState_CreatesHealthCheck()
    {
        // Arrange
        _healthSlice.RecordConnectionEstablished();
        _healthSlice.RecordMessageReceived();
        _healthSlice.RecordMessageSent();
        _healthSlice.RecordLatency(10.0);

        // Act
        var healthCheck = _healthSlice.CreateHealthCheck(ConnectionState.Connected, 5);

        // Assert
        Assert.NotNull(healthCheck);
        Assert.Equal(ConnectionState.Connected, healthCheck.ConnectionState);
        Assert.Equal(5, healthCheck.ActiveSubscriptions);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HealthSlice_CreateHealthCheck_WithDisconnectedState_CreatesHealthCheck()
    {
        // Act
        var healthCheck = _healthSlice.CreateHealthCheck(ConnectionState.Disconnected, 0);

        // Assert
        Assert.NotNull(healthCheck);
        Assert.Equal(ConnectionState.Disconnected, healthCheck.ConnectionState);
        Assert.Equal(0, healthCheck.ActiveSubscriptions);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task HealthSlice_ConcurrentRecordOperations_IsThreadSafe()
    {
        // Arrange
        var tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>();

        // Act - Multiple threads recording metrics concurrently
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                _healthSlice.RecordMessageReceived();
                _healthSlice.RecordMessageSent();
                _healthSlice.RecordLatency(10.0);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and should have correct counts
        Assert.Equal(10, _healthSlice.GetMessagesReceived());
        Assert.Equal(10, _healthSlice.GetMessagesSent());
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HealthSlice_RecordLatency_WithNegativeValue_StillRecords()
    {
        // BUG: Negative latency values should probably be validated
        // Act
        _healthSlice.RecordLatency(-10.0);

        // Assert - Currently accepts negative values
        var avgLatency = _healthSlice.GetAverageLatency();
        Assert.NotNull(avgLatency);
        Assert.Equal(-10.0, avgLatency.Value);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void HealthSlice_RecordLatency_WithVeryLargeValue_StillRecords()
    {
        // Act
        _healthSlice.RecordLatency(double.MaxValue);

        // Assert - Should handle large values
        var avgLatency = _healthSlice.GetAverageLatency();
        Assert.NotNull(avgLatency);
    }

    #endregion
}


