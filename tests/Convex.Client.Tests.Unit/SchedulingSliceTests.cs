using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Http;
using Convex.Client.Shared.Serialization;
using Convex.Client.Slices.Scheduling;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class SchedulingSliceTests
{
    private Mock<IHttpClientProvider> _mockHttpProvider = null!;
    private Mock<IConvexSerializer> _mockSerializer = null!;
    private Mock<ILogger> _mockLogger = null!;
    private SchedulingSlice _schedulingSlice = null!;
    private const string TestDeploymentUrl = "https://test.convex.cloud";
    private const string TestFunctionName = "test:scheduledFunction";
    private const string TestJobId = "job-id-123";

    public SchedulingSliceTests()
    {
        _mockHttpProvider = new Mock<IHttpClientProvider>();
        _mockSerializer = new Mock<IConvexSerializer>();
        _mockLogger = new Mock<ILogger>();

        _mockHttpProvider.Setup(p => p.DeploymentUrl).Returns(TestDeploymentUrl);

        _schedulingSlice = new SchedulingSlice(
            _mockHttpProvider.Object,
            _mockSerializer.Object,
            _mockLogger.Object,
            enableDebugLogging: false);
    }

    #region SchedulingSlice Entry Point Tests

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void SchedulingSlice_Constructor_WithNullHttpProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SchedulingSlice(null!, _mockSerializer.Object));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void SchedulingSlice_Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SchedulingSlice(_mockHttpProvider.Object, null!));
    }

    [Fact]
    public async Task SchedulingSlice_ScheduleAsync_WithValidDelay_ReturnsJobId()
    {
        // Arrange
        var delay = TimeSpan.FromMinutes(5);
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobId = await _schedulingSlice.ScheduleAsync(TestFunctionName, delay);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    public async Task SchedulingSlice_ScheduleAsync_WithArgs_ReturnsJobId()
    {
        // Arrange
        var delay = TimeSpan.FromMinutes(5);
        var args = new { id = 123 };
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobId = await _schedulingSlice.ScheduleAsync(TestFunctionName, delay, args);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task SchedulingSlice_ScheduleAsync_WithNegativeDelay_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConvexSchedulingException>(() =>
            _schedulingSlice.ScheduleAsync(TestFunctionName, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task SchedulingSlice_ScheduleAtAsync_WithValidTime_ReturnsJobId()
    {
        // Arrange
        var scheduledTime = DateTimeOffset.UtcNow.AddHours(1);
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobId = await _schedulingSlice.ScheduleAtAsync(TestFunctionName, scheduledTime);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task SchedulingSlice_ScheduleAtAsync_WithPastTime_MayStillSchedule()
    {
        // Arrange - Implementation now validates and throws exception for past dates
        var pastTime = DateTimeOffset.UtcNow.AddHours(-1);

        // Act & Assert - Should throw ConvexSchedulingException
        await Assert.ThrowsAsync<ConvexSchedulingException>(() =>
            _schedulingSlice.ScheduleAtAsync(TestFunctionName, pastTime));
    }

    [Fact]
    public async Task SchedulingSlice_ScheduleRecurringAsync_WithValidCron_ReturnsJobId()
    {
        // Arrange
        var cronExpression = "0 0 * * *"; // Daily at midnight
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobId = await _schedulingSlice.ScheduleRecurringAsync(TestFunctionName, cronExpression);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task SchedulingSlice_ScheduleRecurringAsync_WithInvalidCron_MayStillSchedule()
    {
        // BUG: Invalid cron expressions should probably be validated
        // Arrange
        var invalidCron = "invalid cron";
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act - Currently doesn't validate cron expressions client-side
        var jobId = await _schedulingSlice.ScheduleRecurringAsync(TestFunctionName, invalidCron);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    public async Task SchedulingSlice_ScheduleIntervalAsync_WithValidInterval_ReturnsJobId()
    {
        // Arrange
        var interval = TimeSpan.FromHours(1);
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobId\":\"" + TestJobId + "\"}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobId = await _schedulingSlice.ScheduleIntervalAsync(TestFunctionName, interval);

        // Assert
        Assert.Equal(TestJobId, jobId);
    }

    [Fact]
    public async Task SchedulingSlice_CancelAsync_WithValidJobId_ReturnsTrue()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"cancelled\":true}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _schedulingSlice.CancelAsync(TestJobId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SchedulingSlice_GetJobAsync_WithValidJobId_ReturnsJob()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var responseJson = "{\"status\":\"success\",\"value\":{\"id\":\"" + TestJobId + "\",\"functionName\":\"" + TestFunctionName + "\",\"status\":\"pending\",\"schedule\":{\"type\":\"oneTime\",\"scheduledTime\":1234567890},\"createdAt\":" + now + ",\"updatedAt\":" + now + "}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var job = await _schedulingSlice.GetJobAsync(TestJobId);

        // Assert
        Assert.NotNull(job);
        Assert.Equal(TestJobId, job.Id);
    }

    [Fact]
    public async Task SchedulingSlice_ListJobsAsync_ReturnsJobs()
    {
        // Arrange
        var responseJson = "{\"status\":\"success\",\"value\":{\"jobs\":[]}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var jobs = await _schedulingSlice.ListJobsAsync();

        // Assert
        Assert.NotNull(jobs);
    }

    [Fact]
    public async Task SchedulingSlice_UpdateScheduleAsync_WithValidSchedule_ReturnsTrue()
    {
        // Arrange
        var newSchedule = ConvexScheduleConfig.OneTime(DateTimeOffset.UtcNow.AddHours(1));
        var responseJson = "{\"status\":\"success\",\"value\":{\"updated\":true}}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockSerializer.Setup(s => s.Serialize(It.IsAny<object>())).Returns("{}");
        _mockSerializer.Setup(s => s.Deserialize<JsonElement>(It.IsAny<string>())).Returns<string>(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        });
        _mockHttpProvider.Setup(p => p.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _schedulingSlice.UpdateScheduleAsync(TestJobId, newSchedule);

        // Assert
        Assert.True(result);
    }

    #endregion
}


