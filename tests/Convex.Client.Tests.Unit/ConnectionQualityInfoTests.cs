using System;
using System.Collections.Generic;
using Convex.Client.Infrastructure.Quality;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ConnectionQualityInfoTests
{
    private static readonly int[] TestArray = [1, 2, 3];

    #region Constructor Tests

    [Fact]
    public void Constructor_WithRequiredParameters_SetsProperties()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Excellent,
            "Excellent connection");

        // Assert
        Assert.Equal(ConnectionQuality.Excellent, info.Quality);
        Assert.Equal("Excellent connection", info.Description);
    }

    [Fact]
    public void Constructor_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good connection");

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(info.Timestamp >= before);
        Assert.True(info.Timestamp <= after);
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var additionalData = new Dictionary<string, object> { { "key", "value" } };

        // Act
        var info = new ConnectionQualityInfo(
            quality: ConnectionQuality.Fair,
            description: "Fair connection",
            averageLatencyMs: 350.5,
            latencyVarianceMs: 50.0,
            packetLossRate: 2.5,
            reconnectionCount: 3,
            errorCount: 5,
            timeSinceLastMessage: TimeSpan.FromSeconds(10),
            uptimePercentage: 98.5,
            qualityScore: 75,
            additionalData: additionalData);

        // Assert
        Assert.Equal(ConnectionQuality.Fair, info.Quality);
        Assert.Equal("Fair connection", info.Description);
        Assert.Equal(350.5, info.AverageLatencyMs);
        Assert.Equal(50.0, info.LatencyVarianceMs);
        Assert.Equal(2.5, info.PacketLossRate);
        Assert.Equal(3, info.ReconnectionCount);
        Assert.Equal(5, info.ErrorCount);
        Assert.Equal(TimeSpan.FromSeconds(10), info.TimeSinceLastMessage);
        Assert.Equal(98.5, info.UptimePercentage);
        Assert.Equal(75, info.QualityScore);
        Assert.Same(additionalData, info.AdditionalData);
    }

    [Fact]
    public void Constructor_WithNullAdditionalData_UsesEmptyDictionary()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good connection",
            additionalData: null);

        // Assert
        Assert.NotNull(info.AdditionalData);
        Assert.Empty(info.AdditionalData);
    }

    #endregion Constructor Tests

    #region Default Values Tests

    [Fact]
    public void Constructor_WithDefaults_HasNullAverageLatency()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Null(info.AverageLatencyMs);
    }

    [Fact]
    public void Constructor_WithDefaults_HasNullLatencyVariance()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Null(info.LatencyVarianceMs);
    }

    [Fact]
    public void Constructor_WithDefaults_HasNullPacketLossRate()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Null(info.PacketLossRate);
    }

    [Fact]
    public void Constructor_WithDefaults_HasZeroReconnectionCount()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Equal(0, info.ReconnectionCount);
    }

    [Fact]
    public void Constructor_WithDefaults_HasZeroErrorCount()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Equal(0, info.ErrorCount);
    }

    [Fact]
    public void Constructor_WithDefaults_HasNullTimeSinceLastMessage()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Null(info.TimeSinceLastMessage);
    }

    [Fact]
    public void Constructor_WithDefaults_HasFullUptime()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Equal(100.0, info.UptimePercentage);
    }

    [Fact]
    public void Constructor_WithDefaults_HasFullQualityScore()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "Unknown");

        // Assert
        Assert.Equal(100, info.QualityScore);
    }

    #endregion Default Values Tests

    #region ConnectionQuality Enum Tests

    [Theory]
    [InlineData(ConnectionQuality.Unknown)]
    [InlineData(ConnectionQuality.Excellent)]
    [InlineData(ConnectionQuality.Good)]
    [InlineData(ConnectionQuality.Fair)]
    [InlineData(ConnectionQuality.Poor)]
    [InlineData(ConnectionQuality.Terrible)]
    public void Constructor_AcceptsAllQualityLevels(ConnectionQuality quality)
    {
        // Act
        var info = new ConnectionQualityInfo(quality, "Test");

        // Assert
        Assert.Equal(quality, info.Quality);
    }

    [Fact]
    public void ConnectionQuality_HasCorrectValues()
    {
        // Assert - Verify enum values match documentation
        Assert.Equal(0, (int)ConnectionQuality.Unknown);
        Assert.Equal(1, (int)ConnectionQuality.Excellent);
        Assert.Equal(2, (int)ConnectionQuality.Good);
        Assert.Equal(3, (int)ConnectionQuality.Fair);
        Assert.Equal(4, (int)ConnectionQuality.Poor);
        Assert.Equal(5, (int)ConnectionQuality.Terrible);
    }

    #endregion ConnectionQuality Enum Tests

    #region ToString Tests

    [Fact]
    public void ToString_IncludesQuality()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Excellent,
            "Great");

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Quality: Excellent", result);
    }

    [Fact]
    public void ToString_IncludesScore()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good",
            qualityScore: 85);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Score: 85/100", result);
    }

    [Fact]
    public void ToString_IncludesDescription()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Fair,
            "Test Description");

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Description: Test Description", result);
    }

    [Fact]
    public void ToString_IncludesAverageLatency_WhenProvided()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good",
            averageLatencyMs: 150.5);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Avg Latency: 150.50ms", result);
    }

    [Fact]
    public void ToString_ExcludesAverageLatency_WhenNull()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good");

        // Act
        var result = info.ToString();

        // Assert
        Assert.DoesNotContain("Avg Latency", result);
    }

    [Fact]
    public void ToString_IncludesLatencyVariance_WhenProvided()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good",
            latencyVarianceMs: 25.5);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Latency Variance: 25.50ms", result);
    }

    [Fact]
    public void ToString_IncludesPacketLoss_WhenProvided()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Fair,
            "Fair",
            packetLossRate: 5.5);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Packet Loss: 5.50%", result);
    }

    [Fact]
    public void ToString_IncludesUptime()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good",
            uptimePercentage: 99.5);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Uptime: 99.5%", result);
    }

    [Fact]
    public void ToString_IncludesReconnections()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Poor,
            "Poor",
            reconnectionCount: 7);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Reconnections: 7", result);
    }

    [Fact]
    public void ToString_IncludesErrors()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Poor,
            "Poor",
            errorCount: 12);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Errors: 12", result);
    }

    [Fact]
    public void ToString_WithAllValues_FormatsCorrectly()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            quality: ConnectionQuality.Fair,
            description: "Fair connection",
            averageLatencyMs: 350.0,
            latencyVarianceMs: 50.0,
            packetLossRate: 2.0,
            reconnectionCount: 3,
            errorCount: 5,
            uptimePercentage: 95.0,
            qualityScore: 60);

        // Act
        var result = info.ToString();

        // Assert
        Assert.Contains("Quality: Fair", result);
        Assert.Contains("Score: 60/100", result);
        Assert.Contains("Description: Fair connection", result);
        Assert.Contains("Avg Latency: 350.00ms", result);
        Assert.Contains("Latency Variance: 50.00ms", result);
        Assert.Contains("Packet Loss: 2.00%", result);
        Assert.Contains("Uptime: 95.0%", result);
        Assert.Contains("Reconnections: 3", result);
        Assert.Contains("Errors: 5", result);
    }

    #endregion ToString Tests

    #region Edge Cases

    [Fact]
    public void Constructor_WithZeroValues_Works()
    {
        // Act
        var info = new ConnectionQualityInfo(
            quality: ConnectionQuality.Excellent,
            description: "Perfect",
            averageLatencyMs: 0,
            latencyVarianceMs: 0,
            packetLossRate: 0,
            reconnectionCount: 0,
            errorCount: 0,
            uptimePercentage: 0,
            qualityScore: 0);

        // Assert
        Assert.Equal(0, info.AverageLatencyMs);
        Assert.Equal(0, info.QualityScore);
    }

    [Fact]
    public void Constructor_WithNegativeValues_Works()
    {
        // Act - Negative values might be used for special indicators
        var info = new ConnectionQualityInfo(
            quality: ConnectionQuality.Unknown,
            description: "Unknown",
            averageLatencyMs: -1,
            reconnectionCount: -1);

        // Assert
        Assert.Equal(-1, info.AverageLatencyMs);
        Assert.Equal(-1, info.ReconnectionCount);
    }

    [Fact]
    public void Constructor_WithLargeValues_Works()
    {
        // Act
        var info = new ConnectionQualityInfo(
            quality: ConnectionQuality.Terrible,
            description: "Terrible",
            averageLatencyMs: double.MaxValue,
            reconnectionCount: int.MaxValue,
            errorCount: int.MaxValue);

        // Assert
        Assert.Equal(double.MaxValue, info.AverageLatencyMs);
        Assert.Equal(int.MaxValue, info.ReconnectionCount);
        Assert.Equal(int.MaxValue, info.ErrorCount);
    }

    [Fact]
    public void Constructor_WithEmptyDescription_Works()
    {
        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Unknown,
            "");

        // Assert
        Assert.Equal("", info.Description);
    }

    [Fact]
    public void AdditionalData_IsReadOnly()
    {
        // Arrange
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good");

        // Assert
        _ = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(info.AdditionalData);
    }

    [Fact]
    public void AdditionalData_CanContainVariousTypes()
    {
        // Arrange
        var additionalData = new Dictionary<string, object>
        {
            { "string", "value" },
            { "int", 42 },
            { "double", 3.14 },
            { "bool", true },
            { "array", TestArray }
        };

        // Act
        var info = new ConnectionQualityInfo(
            ConnectionQuality.Good,
            "Good",
            additionalData: additionalData);

        // Assert
        Assert.Equal(5, info.AdditionalData.Count);
        Assert.Equal("value", info.AdditionalData["string"]);
        Assert.Equal(42, info.AdditionalData["int"]);
    }

    #endregion Edge Cases
}
