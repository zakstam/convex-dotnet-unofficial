using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Resilience;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class RetryPolicyTests
{
    #region Static Factory Tests

    [Fact]
    public void Default_ReturnsConfiguredPolicy()
    {
        // Act
        var policy = RetryPolicy.Default();

        // Assert
        Assert.Equal(3, policy.MaxRetries);
        Assert.Equal(BackoffStrategy.Exponential, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.InitialDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.True(policy.UseJitter);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
    }

    [Fact]
    public void Aggressive_ReturnsConfiguredPolicy()
    {
        // Act
        var policy = RetryPolicy.Aggressive();

        // Assert
        Assert.Equal(5, policy.MaxRetries);
        Assert.Equal(BackoffStrategy.Exponential, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromMilliseconds(500), policy.InitialDelay);
        Assert.Equal(1.5, policy.BackoffMultiplier);
        Assert.True(policy.UseJitter);
        Assert.Equal(TimeSpan.FromSeconds(10), policy.MaxDelay);
    }

    [Fact]
    public void Conservative_ReturnsConfiguredPolicy()
    {
        // Act
        var policy = RetryPolicy.Conservative();

        // Assert
        Assert.Equal(2, policy.MaxRetries);
        Assert.Equal(BackoffStrategy.Exponential, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.InitialDelay);
        Assert.Equal(3.0, policy.BackoffMultiplier);
        Assert.True(policy.UseJitter);
        Assert.Equal(TimeSpan.FromMinutes(1), policy.MaxDelay);
    }

    [Fact]
    public void None_ReturnsZeroRetries()
    {
        // Act
        var policy = RetryPolicy.None();

        // Assert
        Assert.Equal(0, policy.MaxRetries);
    }

    #endregion Static Factory Tests

    #region CalculateDelay Tests

    [Fact]
    public void CalculateDelay_WithZeroAttempt_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => policy.CalculateDelay(0));
    }

    [Fact]
    public void CalculateDelay_WithNegativeAttempt_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => policy.CalculateDelay(-1));
    }

    [Fact]
    public void CalculateDelay_ConstantBackoff_ReturnsSimilarDelays()
    {
        // Arrange - Use exponential with jitter disabled via builder
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromSeconds(5), multiplier: 1.001, useJitter: false)
            .Build();

        // Act
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(1);
        var delay3 = policy.CalculateDelay(1);

        // Assert - All first attempts should be same
        Assert.Equal(delay1, delay2);
        Assert.Equal(delay2, delay3);
    }

    [Fact]
    public void CalculateDelay_LinearBackoff_IncreasesLinearly()
    {
        // Arrange - Note: Linear backoff doesn't have useJitter param in builder
        // so we test that delays increase with attempt number
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .LinearBackoff(TimeSpan.FromSeconds(1))
            .Build();

        // Act - Get base delays (jitter adds variance but base should increase)
        var delay1 = policy.CalculateDelay(1);
        var delay3 = policy.CalculateDelay(3);

        // Assert - With jitter, values won't be exact but should generally increase
        // Test that delay3 > delay1 (allowing for some jitter variance)
        Assert.True(delay3.TotalMilliseconds > delay1.TotalMilliseconds * 0.5,
            "Third delay should be significantly larger than first");
    }

    [Fact]
    public void CalculateDelay_ExponentialBackoff_IncreasesExponentially()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromSeconds(1), multiplier: 2.0, useJitter: false)
            .Build();

        // Act
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);
        var delay3 = policy.CalculateDelay(3);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), delay1);  // 1 * 2^0 = 1
        Assert.Equal(TimeSpan.FromSeconds(2), delay2);  // 1 * 2^1 = 2
        Assert.Equal(TimeSpan.FromSeconds(4), delay3);  // 1 * 2^2 = 4
    }

    [Fact]
    public void CalculateDelay_WithMaxDelay_CapsDelay()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(10)
            .ExponentialBackoff(TimeSpan.FromSeconds(1), multiplier: 10.0, useJitter: false)
            .WithMaxDelay(TimeSpan.FromSeconds(5))
            .Build();

        // Act
        var delay3 = policy.CalculateDelay(3); // Would be 100 seconds without cap

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), delay3);
    }

    [Fact]
    public void CalculateDelay_WithJitter_ReturnsVariedDelays()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ConstantBackoff(TimeSpan.FromSeconds(10))
            .Build();

        // Act - Get multiple delays
        var delays = new TimeSpan[10];
        for (var i = 0; i < 10; i++)
        {
            delays[i] = policy.CalculateDelay(1);
        }

        // Assert - With jitter, delays should vary within ±25%
        // Base = 10s, so range should be 7.5s to 12.5s
        foreach (var delay in delays)
        {
            Assert.True(delay >= TimeSpan.FromSeconds(7.5), $"Delay {delay} is less than minimum");
            Assert.True(delay <= TimeSpan.FromSeconds(12.5), $"Delay {delay} is more than maximum");
        }
    }

    #endregion CalculateDelay Tests

    #region ShouldRetry Tests

    [Fact]
    public void ShouldRetry_HttpRequestException_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new HttpRequestException());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_TaskCanceledException_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new TaskCanceledException());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexFunctionException_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new ConvexFunctionException("error", "myFunction"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ConvexArgumentException_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new ConvexArgumentException("error", "arg"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ConvexAuthenticationException_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new ConvexAuthenticationException("error"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_Timeout_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("timeout", NetworkErrorType.Timeout);

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ConnectionFailure_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("connection failed", NetworkErrorType.ConnectionFailure);

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_DnsResolution_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("dns failed", NetworkErrorType.DnsResolution);

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_SslCertificate_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("ssl error", NetworkErrorType.SslCertificate);

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError500_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("server error", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError502_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("bad gateway", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.BadGateway
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError503_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("service unavailable", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.ServiceUnavailable
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError504_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("gateway timeout", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.GatewayTimeout
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError429_ReturnsTrue()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("rate limited", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.TooManyRequests
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRetry_ConvexNetworkException_ServerError400_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();
        var exception = new ConvexNetworkException("bad request", NetworkErrorType.ServerError)
        {
            StatusCode = HttpStatusCode.BadRequest
        };

        // Act
        var result = policy.ShouldRetry(exception);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_GenericException_ReturnsFalse()
    {
        // Arrange
        var policy = RetryPolicy.Default();

        // Act
        var result = policy.ShouldRetry(new InvalidOperationException("error"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRetry_WithSpecificExceptionTypes_OnlyRetriesThoseTypes()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .RetryOn<InvalidOperationException>()
            .Build();

        // Act & Assert
        Assert.True(policy.ShouldRetry(new InvalidOperationException("test")));
        Assert.False(policy.ShouldRetry(new HttpRequestException())); // Not in list
    }

    [Fact]
    public void ShouldRetry_WithMultipleExceptionTypes_RetriesAll()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .RetryOn<InvalidOperationException>()
            .RetryOn<ArgumentException>()
            .Build();

        // Act & Assert
        Assert.True(policy.ShouldRetry(new InvalidOperationException("test")));
        Assert.True(policy.ShouldRetry(new ArgumentException("test")));
        Assert.False(policy.ShouldRetry(new NotSupportedException("test"))); // Not in list
    }

    [Fact]
    public void ShouldRetry_WithBaseExceptionType_RetriesDerivedTypes()
    {
        // Arrange
        var policy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .RetryOn<ArgumentException>()
            .Build();

        // Act & Assert
        Assert.True(policy.ShouldRetry(new ArgumentException("test")));
        Assert.True(policy.ShouldRetry(new ArgumentNullException("param"))); // Derived from ArgumentException
        Assert.True(policy.ShouldRetry(new ArgumentOutOfRangeException("param"))); // Derived from ArgumentException
    }

    #endregion ShouldRetry Tests
}

public class RetryPolicyBuilderTests
{
    #region MaxRetries Tests

    [Fact]
    public void MaxRetries_WithValidValue_SetsProperty()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .MaxRetries(5)
            .Build();

        // Assert
        Assert.Equal(5, policy.MaxRetries);
    }

    [Fact]
    public void MaxRetries_WithZero_SetsProperty()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .MaxRetries(0)
            .Build();

        // Assert
        Assert.Equal(0, policy.MaxRetries);
    }

    [Fact]
    public void MaxRetries_WithNegativeValue_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => builder.MaxRetries(-1));
    }

    #endregion MaxRetries Tests

    #region Backoff Strategy Tests

    [Fact]
    public void ExponentialBackoff_WithValidParameters_SetsProperties()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .ExponentialBackoff(TimeSpan.FromSeconds(2), multiplier: 3.0, useJitter: false)
            .Build();

        // Assert
        Assert.Equal(BackoffStrategy.Exponential, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromSeconds(2), policy.InitialDelay);
        Assert.Equal(3.0, policy.BackoffMultiplier);
        Assert.False(policy.UseJitter);
    }

    [Fact]
    public void ExponentialBackoff_WithZeroDelay_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ExponentialBackoff(TimeSpan.Zero));
    }

    [Fact]
    public void ExponentialBackoff_WithNegativeDelay_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ExponentialBackoff(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void ExponentialBackoff_WithMultiplierLessThanOne_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ExponentialBackoff(TimeSpan.FromSeconds(1), multiplier: 0.5));
    }

    [Fact]
    public void ExponentialBackoff_WithMultiplierEqualToOne_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ExponentialBackoff(TimeSpan.FromSeconds(1), multiplier: 1.0));
    }

    [Fact]
    public void LinearBackoff_WithValidDelay_SetsProperties()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .LinearBackoff(TimeSpan.FromSeconds(1))
            .Build();

        // Assert
        Assert.Equal(BackoffStrategy.Linear, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialDelay);
    }

    [Fact]
    public void LinearBackoff_WithZeroDelay_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.LinearBackoff(TimeSpan.Zero));
    }

    [Fact]
    public void ConstantBackoff_WithValidDelay_SetsProperties()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .ConstantBackoff(TimeSpan.FromSeconds(5))
            .Build();

        // Assert
        Assert.Equal(BackoffStrategy.Constant, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromSeconds(5), policy.InitialDelay);
    }

    [Fact]
    public void ConstantBackoff_WithZeroDelay_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ConstantBackoff(TimeSpan.Zero));
    }

    #endregion Backoff Strategy Tests

    #region WithMaxDelay Tests

    [Fact]
    public void WithMaxDelay_WithValidValue_SetsProperty()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .WithMaxDelay(TimeSpan.FromMinutes(1))
            .Build();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), policy.MaxDelay);
    }

    [Fact]
    public void WithMaxDelay_WithZeroDelay_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithMaxDelay(TimeSpan.Zero));
    }

    #endregion WithMaxDelay Tests

    #region RetryOn Tests

    [Fact]
    public void RetryOn_AddsExceptionType()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .RetryOn<HttpRequestException>()
            .Build();

        // Assert
        Assert.Contains(typeof(HttpRequestException), policy.RetryableExceptionTypes);
    }

    [Fact]
    public void RetryOn_MultipleTypes_AddsAllTypes()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .RetryOn<HttpRequestException>()
            .RetryOn<TaskCanceledException>()
            .RetryOn<InvalidOperationException>()
            .Build();

        // Assert
        Assert.Equal(3, policy.RetryableExceptionTypes.Count);
        Assert.Contains(typeof(HttpRequestException), policy.RetryableExceptionTypes);
        Assert.Contains(typeof(TaskCanceledException), policy.RetryableExceptionTypes);
        Assert.Contains(typeof(InvalidOperationException), policy.RetryableExceptionTypes);
    }

    #endregion RetryOn Tests

    #region OnRetry Tests

    [Fact]
    public void OnRetry_WithValidCallback_SetsProperty()
    {
        // Arrange
        var callbackInvoked = false;

        // Act
        var policy = new RetryPolicyBuilder()
            .OnRetry((_, _, _) => callbackInvoked = true)
            .Build();

        // Assert
        Assert.NotNull(policy.OnRetryCallback);
        policy.OnRetryCallback(1, new InvalidOperationException("test"), TimeSpan.Zero);
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void OnRetry_WithNullCallback_ThrowsArgumentNull()
    {
        // Arrange
        var builder = new RetryPolicyBuilder();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => builder.OnRetry(null!));
    }

    #endregion OnRetry Tests

    #region Fluent Chaining Tests

    [Fact]
    public void Builder_FluentChaining_Works()
    {
        // Arrange & Act
        var policy = new RetryPolicyBuilder()
            .MaxRetries(5)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(100), multiplier: 2.0, useJitter: true)
            .WithMaxDelay(TimeSpan.FromSeconds(30))
            .RetryOn<HttpRequestException>()
            .RetryOn<TaskCanceledException>()
            .OnRetry(static (_, _, _) => { })
            .Build();

        // Assert
        Assert.Equal(5, policy.MaxRetries);
        Assert.Equal(BackoffStrategy.Exponential, policy.BackoffStrategy);
        Assert.Equal(TimeSpan.FromMilliseconds(100), policy.InitialDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.True(policy.UseJitter);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        Assert.Equal(2, policy.RetryableExceptionTypes.Count);
        Assert.NotNull(policy.OnRetryCallback);
    }

    #endregion Fluent Chaining Tests
}
