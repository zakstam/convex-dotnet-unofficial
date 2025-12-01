using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ResilienceCoordinatorTests
{
    private readonly Mock<ICircuitBreakerPolicy> _mockCircuitBreaker;
    private readonly Mock<ILogger> _mockLogger;

    public ResilienceCoordinatorTests()
    {
        _mockCircuitBreaker = new Mock<ICircuitBreakerPolicy>();
        _mockLogger = new Mock<ILogger>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_CreatesInstance()
    {
        // Act
        var coordinator = new ResilienceCoordinator();

        // Assert
        Assert.NotNull(coordinator);
        Assert.Null(coordinator.RetryPolicy);
        Assert.Null(coordinator.CircuitBreakerPolicy);
    }

    [Fact]
    public void Constructor_WithRetryPolicy_SetsRetryPolicy()
    {
        // Arrange
        var retryPolicy = RetryPolicy.Default();

        // Act
        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy);

        // Assert
        Assert.NotNull(coordinator.RetryPolicy);
        Assert.Equal(3, coordinator.RetryPolicy.MaxRetries);
    }

    [Fact]
    public void Constructor_WithCircuitBreaker_SetsCircuitBreaker()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        // Act
        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Assert
        Assert.NotNull(coordinator.CircuitBreakerPolicy);
    }

    [Fact]
    public void Constructor_WithLogger_SetsLogger()
    {
        // Act - Should not throw
        var coordinator = new ResilienceCoordinator(logger: _mockLogger.Object);

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    public void Constructor_WithDebugLogging_EnablesDebugLogging()
    {
        // Act - Should not throw
        var coordinator = new ResilienceCoordinator(enableDebugLogging: true);

        // Assert
        Assert.NotNull(coordinator);
    }

    #endregion Constructor Tests

    #region ExecuteAsync<T> Tests

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.ExecuteAsync<int>(null!));
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();

        // Act
        var result = await coordinator.ExecuteAsync(static async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            coordinator.ExecuteAsync(static async () =>
            {
                await Task.Delay(100);
                return 42;
            }, cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitBreakerBlocks_ThrowsCircuitBreakerException()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(false);
        _ = _mockCircuitBreaker.Setup(cb => cb.State).Returns(CircuitBreakerState.Open);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConvexCircuitBreakerException>(() =>
            coordinator.ExecuteAsync(static async () =>
            {
                await Task.Delay(1);
                return 42;
            }));

        Assert.Contains("Circuit breaker is open", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulOperation_RecordsSuccess()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act
        _ = await coordinator.ExecuteAsync(static async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        // Assert
        _mockCircuitBreaker.Verify(cb => cb.RecordSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_FailedOperation_RecordsFailure()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.ExecuteAsync<int>(static async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test error");
            }));

        _mockCircuitBreaker.Verify(cb => cb.RecordFailure(It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryPolicy_RetriesOnTransientFailure()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10), useJitter: false)
            .RetryOn<HttpRequestException>()
            .Build();

        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy);
        var attemptCount = 0;

        // Act
        var result = await coordinator.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            attemptCount++;
            return attemptCount < 3 ? throw new HttpRequestException("Transient failure") : 42;
        });

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryPolicy_DoesNotRetryNonTransientException()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10), useJitter: false)
            .RetryOn<HttpRequestException>()
            .Build();

        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy);
        var attemptCount = 0;

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.ExecuteAsync<int>(async () =>
            {
                await Task.Delay(1);
                attemptCount++;
                throw new InvalidOperationException("Non-transient failure");
            }));

        Assert.Equal(1, attemptCount); // Should not retry
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxRetries_ThrowsException()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10), useJitter: false)
            .RetryOn<HttpRequestException>()
            .Build();

        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy);
        var attemptCount = 0;

        // Act & Assert
        _ = await Assert.ThrowsAsync<HttpRequestException>(() =>
            coordinator.ExecuteAsync<int>(async () =>
            {
                await Task.Delay(1);
                attemptCount++;
                throw new HttpRequestException("Persistent failure");
            }));

        Assert.Equal(3, attemptCount); // Initial + 2 retries
    }

    [Fact]
    public async Task ExecuteAsync_WithBothPolicies_AppliesBoth()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(2)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10), useJitter: false)
            .RetryOn<HttpRequestException>()
            .Build();

        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        var coordinator = new ResilienceCoordinator(
            retryPolicy: retryPolicy,
            circuitBreakerPolicy: _mockCircuitBreaker.Object);

        var attemptCount = 0;

        // Act
        var result = await coordinator.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            attemptCount++;
            return attemptCount < 2 ? throw new HttpRequestException("Transient failure") : 42;
        });

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(2, attemptCount);
        _mockCircuitBreaker.Verify(cb => cb.RecordFailure(It.IsAny<Exception>()), Times.Once);
        _mockCircuitBreaker.Verify(cb => cb.RecordSuccess(), Times.Once);
    }

    #endregion ExecuteAsync<T> Tests

    #region ExecuteAsync (void) Tests

    [Fact]
    public async Task ExecuteAsyncVoid_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            coordinator.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsyncVoid_SuccessfulOperation_Completes()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();
        var executed = false;

        // Act
        await coordinator.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteAsyncVoid_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            coordinator.ExecuteAsync(static async () => await Task.Delay(100), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsyncVoid_WhenCircuitBreakerBlocks_ThrowsCircuitBreakerException()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(false);
        _ = _mockCircuitBreaker.Setup(cb => cb.State).Returns(CircuitBreakerState.Open);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConvexCircuitBreakerException>(() =>
            coordinator.ExecuteAsync(static async () => await Task.Delay(1)));

        Assert.Contains("Circuit breaker is open", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsyncVoid_SuccessfulOperation_RecordsSuccess()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act
        await coordinator.ExecuteAsync(static async () => await Task.Delay(1));

        // Assert
        _mockCircuitBreaker.Verify(cb => cb.RecordSuccess(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsyncVoid_FailedOperation_RecordsFailure()
    {
        // Arrange
        _ = _mockCircuitBreaker.Setup(cb => cb.AllowRequest()).Returns(true);

        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object);

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.ExecuteAsync(static async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Test error");
            }));

        _mockCircuitBreaker.Verify(cb => cb.RecordFailure(It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsyncVoid_WithRetryPolicy_RetriesOnTransientFailure()
    {
        // Arrange
        var retryPolicy = new RetryPolicyBuilder()
            .MaxRetries(3)
            .ExponentialBackoff(TimeSpan.FromMilliseconds(10), useJitter: false)
            .RetryOn<HttpRequestException>()
            .Build();

        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy);
        var attemptCount = 0;

        // Act
        await coordinator.ExecuteAsync(async () =>
        {
            await Task.Delay(1);
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient failure");
            }
        });

        // Assert
        Assert.Equal(3, attemptCount);
    }

    #endregion ExecuteAsync (void) Tests

    #region Property Setters Tests

    [Fact]
    public void RetryPolicy_CanBeSetAfterConstruction()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();
        var retryPolicy = new RetryPolicyBuilder().MaxRetries(5).Build();

        // Act
        coordinator.RetryPolicy = retryPolicy;

        // Assert
        Assert.NotNull(coordinator.RetryPolicy);
        Assert.Equal(5, coordinator.RetryPolicy.MaxRetries);
    }

    [Fact]
    public void CircuitBreakerPolicy_CanBeSetAfterConstruction()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator { CircuitBreakerPolicy = _mockCircuitBreaker.Object };

        // Assert
        Assert.NotNull(coordinator.CircuitBreakerPolicy);
    }

    [Fact]
    public void RetryPolicy_CanBeSetToNull()
    {
        // Arrange
        var retryPolicy = RetryPolicy.Default();
        var coordinator = new ResilienceCoordinator(retryPolicy: retryPolicy) { RetryPolicy = null };

        // Assert
        Assert.Null(coordinator.RetryPolicy);
    }

    [Fact]
    public void CircuitBreakerPolicy_CanBeSetToNull()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: _mockCircuitBreaker.Object) { CircuitBreakerPolicy = null };

        // Assert
        Assert.Null(coordinator.CircuitBreakerPolicy);
    }

    #endregion Property Setters Tests

    #region Thread Safety Tests

    [Fact]
    public async Task ExecuteAsync_ConcurrentOperations_AllComplete()
    {
        // Arrange
        var coordinator = new ResilienceCoordinator();
        var tasks = new Task<int>[10];
        var completedCount = 0;

        // Act
        for (var i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = coordinator.ExecuteAsync(async () =>
            {
                await Task.Delay(10);
                _ = Interlocked.Increment(ref completedCount);
                return index;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, completedCount);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i, await tasks[i]);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithCircuitBreaker_ThreadSafe()
    {
        // Arrange
        var circuitBreaker = new CircuitBreakerPolicy(failureThreshold: 100, breakDuration: TimeSpan.FromMinutes(1));
        var coordinator = new ResilienceCoordinator(circuitBreakerPolicy: circuitBreaker);

        var tasks = new Task<int>[10];

        // Act
        for (var i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = coordinator.ExecuteAsync(async () =>
            {
                await Task.Delay(5);
                return index;
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(i, await tasks[i]);
        }
    }

    #endregion Thread Safety Tests
}
