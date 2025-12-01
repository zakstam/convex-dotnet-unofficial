using System;
using System.Net.Http;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Resilience;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class CircuitBreakerPolicyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_SetsDefaultValues()
    {
        // Act
        var policy = new CircuitBreakerPolicy();

        // Assert
        Assert.Equal(5, policy.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.BreakDuration);
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void Constructor_WithCustomThreshold_SetsThreshold()
    {
        // Act
        var policy = new CircuitBreakerPolicy(failureThreshold: 10);

        // Assert
        Assert.Equal(10, policy.FailureThreshold);
    }

    [Fact]
    public void Constructor_WithCustomBreakDuration_SetsDuration()
    {
        // Act
        var policy = new CircuitBreakerPolicy(breakDuration: TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), policy.BreakDuration);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsAllValues()
    {
        // Act
        var policy = new CircuitBreakerPolicy(failureThreshold: 3, breakDuration: TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(3, policy.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(10), policy.BreakDuration);
    }

    #endregion Constructor Tests

    #region Initial State Tests

    [Fact]
    public void State_Initially_IsClosed()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void AllowRequest_Initially_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy();

        // Act
        var result = policy.AllowRequest();

        // Assert
        Assert.True(result);
    }

    #endregion Initial State Tests

    #region RecordSuccess Tests

    [Fact]
    public void RecordSuccess_WhenClosed_RemainsClosedAndResetsFailureCount()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 5);

        // Add some failures but not enough to open
        policy.RecordFailure(new HttpRequestException());
        policy.RecordFailure(new HttpRequestException());

        // Act
        policy.RecordSuccess();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, policy.State);

        // Add failures again - should need all 5 to open (count was reset)
        for (int i = 0; i < 4; i++)
        {
            policy.RecordFailure(new HttpRequestException());
        }
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void RecordSuccess_WhenHalfOpen_TransitionsToClosed()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1, breakDuration: TimeSpan.FromMilliseconds(1));

        // Open the circuit
        policy.RecordFailure(new HttpRequestException());
        Assert.Equal(CircuitBreakerState.Open, policy.State);

        // Wait for break duration and allow request to transition to half-open
        System.Threading.Thread.Sleep(10);
        _ = policy.AllowRequest(); // This transitions to HalfOpen
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);

        // Act
        policy.RecordSuccess();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    #endregion RecordSuccess Tests

    #region RecordFailure Tests

    [Fact]
    public void RecordFailure_BelowThreshold_RemainsClosedState()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 5);

        // Act - Record 4 failures (below threshold of 5)
        for (int i = 0; i < 4; i++)
        {
            policy.RecordFailure(new HttpRequestException());
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void RecordFailure_AtThreshold_OpensCircuit()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 3);

        // Act - Record exactly 3 failures
        policy.RecordFailure(new HttpRequestException());
        policy.RecordFailure(new HttpRequestException());
        policy.RecordFailure(new HttpRequestException());

        // Assert
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void RecordFailure_AboveThreshold_StaysOpen()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 2);

        // Act - Record more failures than threshold
        for (int i = 0; i < 5; i++)
        {
            policy.RecordFailure(new HttpRequestException());
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void RecordFailure_WithHttpRequestException_CountsAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act
        policy.RecordFailure(new HttpRequestException());

        // Assert
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void RecordFailure_WithTaskCanceledException_CountsAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act
        policy.RecordFailure(new TaskCanceledException());

        // Assert
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void RecordFailure_WithConvexNetworkException_CountsAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act - Network error that's not DNS resolution should count
        policy.RecordFailure(new ConvexNetworkException("Timeout", NetworkErrorType.Timeout));

        // Assert
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void RecordFailure_WithConvexNetworkExceptionDns_DoesNotCountAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act - DNS resolution errors don't count
        policy.RecordFailure(new ConvexNetworkException("DNS failed", NetworkErrorType.DnsResolution));

        // Assert - Circuit should remain closed
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void RecordFailure_WithConvexFunctionException_DoesNotCountAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act - Function errors don't count
        policy.RecordFailure(new ConvexFunctionException("Function failed", "myFunction"));

        // Assert - Circuit should remain closed
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void RecordFailure_WithConvexArgumentException_DoesNotCountAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act - Argument errors don't count
        policy.RecordFailure(new ConvexArgumentException("Bad argument", "arg1"));

        // Assert - Circuit should remain closed
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void RecordFailure_WithGenericException_DoesNotCountAsFailure()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1);

        // Act - Generic exceptions don't count
        policy.RecordFailure(new InvalidOperationException("Some error"));

        // Assert - Circuit should remain closed
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    #endregion RecordFailure Tests

    #region AllowRequest Tests

    [Fact]
    public void AllowRequest_WhenClosed_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy();

        // Act
        var result = policy.AllowRequest();

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
    }

    [Fact]
    public void AllowRequest_WhenOpen_ReturnsFalse()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1, breakDuration: TimeSpan.FromMinutes(5));
        policy.RecordFailure(new HttpRequestException());
        Assert.Equal(CircuitBreakerState.Open, policy.State);

        // Act
        var result = policy.AllowRequest();

        // Assert
        Assert.False(result);
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    [Fact]
    public void AllowRequest_WhenOpenAndBreakDurationExpired_TransitionsToHalfOpen()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1, breakDuration: TimeSpan.FromMilliseconds(1));
        policy.RecordFailure(new HttpRequestException());
        Assert.Equal(CircuitBreakerState.Open, policy.State);

        // Wait for break duration to expire
        System.Threading.Thread.Sleep(10);

        // Act
        var result = policy.AllowRequest();

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);
    }

    [Fact]
    public void AllowRequest_WhenHalfOpen_ReturnsTrue()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1, breakDuration: TimeSpan.FromMilliseconds(1));
        policy.RecordFailure(new HttpRequestException());

        // Wait and transition to half-open
        System.Threading.Thread.Sleep(10);
        _ = policy.AllowRequest();
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);

        // Act
        var result = policy.AllowRequest();

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);
    }

    [Fact]
    public void AllowRequest_HalfOpenWithFailure_ReopensCircuit()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 1, breakDuration: TimeSpan.FromMilliseconds(1));
        policy.RecordFailure(new HttpRequestException());

        // Wait and transition to half-open
        System.Threading.Thread.Sleep(10);
        _ = policy.AllowRequest();
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);

        // Act - Record another failure while half-open
        policy.RecordFailure(new HttpRequestException());

        // Assert - Circuit should reopen
        Assert.Equal(CircuitBreakerState.Open, policy.State);
    }

    #endregion AllowRequest Tests

    #region Thread Safety Tests

    [Fact]
    public async Task CircuitBreaker_IsThreadSafe()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 100, breakDuration: TimeSpan.FromMilliseconds(10));
        var tasks = new Task[10];

        // Act - Concurrent record failures and successes
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    if (index % 2 == 0)
                    {
                        policy.RecordFailure(new HttpRequestException());
                    }
                    else
                    {
                        policy.RecordSuccess();
                    }
                    _ = policy.AllowRequest();
                }
            });
        }

        // Should not throw or deadlock
        await Task.WhenAll(tasks);

        // Assert - Just verify we didn't crash or deadlock
        Assert.True(true);
    }

    #endregion Thread Safety Tests

    #region State Transitions Tests

    [Fact]
    public void StateTransitions_FullCycle_Works()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy(failureThreshold: 2, breakDuration: TimeSpan.FromMilliseconds(1));

        // Assert initial state
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
        Assert.True(policy.AllowRequest());

        // Step 1: Trigger failures to open the circuit
        policy.RecordFailure(new HttpRequestException());
        Assert.Equal(CircuitBreakerState.Closed, policy.State); // Still closed

        policy.RecordFailure(new HttpRequestException());
        Assert.Equal(CircuitBreakerState.Open, policy.State); // Now open
        Assert.False(policy.AllowRequest());

        // Step 2: Wait for break duration and transition to half-open
        System.Threading.Thread.Sleep(10);
        Assert.True(policy.AllowRequest());
        Assert.Equal(CircuitBreakerState.HalfOpen, policy.State);

        // Step 3: Record success to close the circuit
        policy.RecordSuccess();
        Assert.Equal(CircuitBreakerState.Closed, policy.State);
        Assert.True(policy.AllowRequest());
    }

    #endregion State Transitions Tests
}
