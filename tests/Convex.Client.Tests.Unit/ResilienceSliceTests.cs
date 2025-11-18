using System;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Resilience;
using Convex.Client.Slices.Resilience;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class ResilienceSliceTests
{
    private Mock<ILogger> _mockLogger = null!;
    private ResilienceSlice _resilienceSlice = null!;

    public ResilienceSliceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _resilienceSlice = new ResilienceSlice(_mockLogger.Object, enableDebugLogging: false);
    }

    #region ResilienceSlice Entry Point Tests

    [Fact]
    public void ResilienceSlice_InitialState_HasNullRetryPolicy()
    {
        // Assert
        Assert.Null(_resilienceSlice.RetryPolicy);
    }

    [Fact]
    public void ResilienceSlice_InitialState_HasNullCircuitBreakerPolicy()
    {
        // Assert
        Assert.Null(_resilienceSlice.CircuitBreakerPolicy);
    }

    [Fact]
    public void ResilienceSlice_RetryPolicy_SetAndGet_Works()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(100))
            .Build();

        // Act
        _resilienceSlice.RetryPolicy = retryPolicy;

        // Assert
        Assert.NotNull(_resilienceSlice.RetryPolicy);
        Assert.Equal(3, _resilienceSlice.RetryPolicy.MaxRetries);
    }

    [Fact]
    public void ResilienceSlice_CircuitBreakerPolicy_SetAndGet_Works()
    {
        // Arrange
        var circuitBreakerPolicy = new CircuitBreakerPolicy(5, TimeSpan.FromSeconds(30));

        // Act
        _resilienceSlice.CircuitBreakerPolicy = circuitBreakerPolicy;

        // Assert
        Assert.NotNull(_resilienceSlice.CircuitBreakerPolicy);
    }

    [Fact]
    public async Task ResilienceSlice_ExecuteAsync_WithSuccess_ReturnsResult()
    {
        // Arrange
        var expectedResult = "success";

        // Act
        var result = await _resilienceSlice.ExecuteAsync(async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ResilienceSlice_ExecuteAsync_WithVoidOperation_Executes()
    {
        // Arrange
        bool executed = false;

        // Act
        await _resilienceSlice.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ResilienceSlice_ExecuteAsync_WithException_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _resilienceSlice.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                throw expectedException;
            }));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task ResilienceSlice_ExecuteAsync_WithRetryPolicy_RetriesOnFailure()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10))
            .RetryOn<InvalidOperationException>()
            .Build();

        _resilienceSlice.RetryPolicy = retryPolicy;

        int attemptCount = 0;

        // Act
        var result = await _resilienceSlice.ExecuteAsync(async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("Temporary failure");
            }
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ResilienceSlice_ExecuteAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _resilienceSlice.ExecuteAsync(async () =>
            {
                await Task.Delay(100);
                return "result";
            }, cts.Token));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ResilienceSlice_RetryPolicy_SetToNull_ClearsPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .Build();
        _resilienceSlice.RetryPolicy = retryPolicy;

        // Act
        _resilienceSlice.RetryPolicy = null;

        // Assert
        Assert.Null(_resilienceSlice.RetryPolicy);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ResilienceSlice_CircuitBreakerPolicy_SetToNull_ClearsPolicy()
    {
        // Arrange
        var circuitBreakerPolicy = new CircuitBreakerPolicy(5, TimeSpan.FromSeconds(30));
        _resilienceSlice.CircuitBreakerPolicy = circuitBreakerPolicy;

        // Act
        _resilienceSlice.CircuitBreakerPolicy = null;

        // Assert
        Assert.Null(_resilienceSlice.CircuitBreakerPolicy);
    }

    #endregion
}


