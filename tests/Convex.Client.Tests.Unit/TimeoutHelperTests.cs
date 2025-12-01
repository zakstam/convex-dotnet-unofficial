using System;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.Http;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class TimeoutHelperTests
{
    #region CreateTimeoutToken Tests - No Timeout

    [Fact]
    public void CreateTimeoutToken_WithNullTimeout_ReturnsUserToken()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var userToken = userCts.Token;

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(null, userToken);

        // Assert
        Assert.Equal(userToken, wrapper.Token);
    }

    [Fact]
    public void CreateTimeoutToken_WithNullTimeout_WasTimeoutIsFalse()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(null, userCts.Token);

        // Assert
        Assert.False(wrapper.WasTimeout);
    }

    [Fact]
    public void CreateTimeoutToken_WithNullTimeoutAndCanceledToken_WasTimeoutIsFalse()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        userCts.Cancel();

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(null, userCts.Token);

        // Assert
        Assert.False(wrapper.WasTimeout);
        Assert.True(wrapper.Token.IsCancellationRequested);
    }

    #endregion CreateTimeoutToken Tests - No Timeout

    #region CreateTimeoutToken Tests - With Timeout

    [Fact]
    public void CreateTimeoutToken_WithTimeout_ReturnsLinkedToken()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        Assert.NotEqual(userCts.Token, wrapper.Token);
    }

    [Fact]
    public void CreateTimeoutToken_WithTimeout_WasTimeoutIsFalseInitially()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        Assert.False(wrapper.WasTimeout);
    }

    [Fact]
    public void CreateTimeoutToken_WithTimeout_TokenIsNotCanceledInitially()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        Assert.False(wrapper.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task CreateTimeoutToken_WithShortTimeout_TimeoutTriggersAsync()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Wait for timeout
        await Task.Delay(100);

        // Assert
        Assert.True(wrapper.WasTimeout);
        Assert.True(wrapper.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateTimeoutToken_UserCancellation_CancelsLinkedToken()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);
        userCts.Cancel();

        // Assert
        Assert.True(wrapper.Token.IsCancellationRequested);
        Assert.False(wrapper.WasTimeout); // User canceled, not timeout
    }

    [Fact]
    public void CreateTimeoutToken_WithAlreadyCanceledUserToken_TokenIsCanceled()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        userCts.Cancel();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        Assert.True(wrapper.Token.IsCancellationRequested);
    }

    #endregion CreateTimeoutToken Tests - With Timeout

    #region TimeoutTokenWrapper Dispose Tests

    [Fact]
    public void Dispose_WithNullTimeout_DoesNotThrow()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var wrapper = TimeoutHelper.CreateTimeoutToken(null, userCts.Token);

        // Act & Assert - Should not throw
        wrapper.Dispose();
    }

    [Fact]
    public void Dispose_WithTimeout_DoesNotThrow()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);
        var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Act & Assert - Should not throw
        wrapper.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);
        var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Act & Assert - Multiple disposals should not throw
        wrapper.Dispose();
        wrapper.Dispose();
        wrapper.Dispose();
    }

    #endregion TimeoutTokenWrapper Dispose Tests

    #region Integration Tests

    [Fact]
    public async Task CreateTimeoutToken_UsedForTaskDelay_ThrowsOnTimeoutAsync()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Task.Delay(TimeSpan.FromSeconds(10), wrapper.Token));

        Assert.True(wrapper.WasTimeout);
    }

    [Fact]
    public async Task CreateTimeoutToken_UsedForTaskDelay_ThrowsOnUserCancel()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            userCts.Cancel();
        });

        // Assert
        _ = await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Task.Delay(TimeSpan.FromSeconds(10), wrapper.Token));

        Assert.False(wrapper.WasTimeout);
    }

    [Fact]
    public async Task CreateTimeoutToken_TaskCompletesBeforeTimeout_WorksAsync()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);
        await Task.Delay(10, wrapper.Token);

        // Assert
        Assert.False(wrapper.WasTimeout);
        Assert.False(wrapper.Token.IsCancellationRequested);
    }

    #endregion Integration Tests

    #region Edge Cases

    [Fact]
    public void CreateTimeoutToken_WithZeroTimeout_TimeoutImmediately()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.Zero;

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Small wait for the CTS to trigger
        Thread.Sleep(10);

        // Assert
        Assert.True(wrapper.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateTimeoutToken_WithVeryLongTimeout_DoesNotTimeoutImmediately()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromHours(24);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert
        Assert.False(wrapper.WasTimeout);
        Assert.False(wrapper.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateTimeoutToken_WithDefaultCancellationToken_Works()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, default);

        // Assert
        Assert.False(wrapper.WasTimeout);
        Assert.False(wrapper.Token.IsCancellationRequested);
    }

    [Fact]
    public void CreateTimeoutToken_WithNullTimeoutAndDefaultToken_ReturnsDefaultToken()
    {
        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(null, default);

        // Assert
        Assert.Equal(default, wrapper.Token);
        Assert.False(wrapper.WasTimeout);
    }

    #endregion Edge Cases

    #region Token Property Tests

    [Fact]
    public void Token_CanBeUsedForRegistration()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);
        var callbackInvoked = false;

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);
        using var registration = wrapper.Token.Register(() => callbackInvoked = true);
        userCts.Cancel();

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public void Token_CanRegisterCallback()
    {
        // Arrange
        using var userCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        using var wrapper = TimeoutHelper.CreateTimeoutToken(timeout, userCts.Token);

        // Assert - Just ensure the token can be used
        Assert.True(wrapper.Token.CanBeCanceled);
    }

    #endregion Token Property Tests
}
