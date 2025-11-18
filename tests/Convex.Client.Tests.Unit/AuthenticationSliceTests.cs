using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Shared.Common;
using Convex.Client.Slices.Authentication;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Convex.Client.Tests.Unit;


public class AuthenticationSliceTests
{
    private Mock<ILogger> _mockLogger = null!;
    private AuthenticationSlice _authenticationSlice = null!;
    private const string TestToken = "test-token-123";
    private const string TestAdminKey = "test-admin-key-456";

    public AuthenticationSliceTests()
    {
        _mockLogger = new Mock<ILogger>();
        _authenticationSlice = new AuthenticationSlice(_mockLogger.Object, enableDebugLogging: false);
    }

    #region AuthenticationSlice Entry Point Tests

    [Fact]
    public void AuthenticationSlice_InitialState_IsUnauthenticated()
    {
        // Assert
        Assert.Equal(AuthenticationState.Unauthenticated, _authenticationSlice.AuthenticationState);
        Assert.Null(_authenticationSlice.CurrentAuthToken);
    }

    [Fact]
    public async Task AuthenticationSlice_SetAuthTokenAsync_WithValidToken_SetsToken()
    {
        // Act
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
        Assert.Equal(TestToken, _authenticationSlice.CurrentAuthToken);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_SetAuthTokenAsync_WithNullToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAuthTokenAsync(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_SetAuthTokenAsync_WithEmptyToken_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAuthTokenAsync(""));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAuthTokenAsync("   "));
    }

    [Fact]
    public async Task AuthenticationSlice_SetAdminAuthAsync_WithValidKey_SetsAdminAuth()
    {
        // Act
        await _authenticationSlice.SetAdminAuthAsync(TestAdminKey);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_SetAdminAuthAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAdminAuthAsync(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_SetAdminAuthAsync_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAdminAuthAsync(""));
    }

    [Fact]
    public async Task AuthenticationSlice_SetAuthTokenProviderAsync_WithValidProvider_SetsProvider()
    {
        // Arrange
        var mockProvider = new Mock<IAuthTokenProvider>();
        mockProvider.Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestToken);

        // Act
        await _authenticationSlice.SetAuthTokenProviderAsync(mockProvider.Object);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_SetAuthTokenProviderAsync_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _authenticationSlice.SetAuthTokenProviderAsync(null!));
    }

    [Fact]
    public async Task AuthenticationSlice_ClearAuthAsync_ClearsAuthentication()
    {
        // Arrange
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Act
        await _authenticationSlice.ClearAuthAsync();

        // Assert
        Assert.Equal(AuthenticationState.Unauthenticated, _authenticationSlice.AuthenticationState);
        Assert.Null(_authenticationSlice.CurrentAuthToken);
    }

    [Fact]
    public async Task AuthenticationSlice_GetAuthTokenAsync_WithToken_ReturnsToken()
    {
        // Arrange
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Act
        var token = await _authenticationSlice.GetAuthTokenAsync();

        // Assert
        Assert.Equal(TestToken, token);
    }

    [Fact]
    public async Task AuthenticationSlice_GetAuthTokenAsync_WithProvider_ReturnsTokenFromProvider()
    {
        // Arrange
        var mockProvider = new Mock<IAuthTokenProvider>();
        mockProvider.Setup(p => p.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestToken);

        await _authenticationSlice.SetAuthTokenProviderAsync(mockProvider.Object);

        // Act
        var token = await _authenticationSlice.GetAuthTokenAsync();

        // Assert
        Assert.Equal(TestToken, token);
        mockProvider.Verify(p => p.GetTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthenticationSlice_GetAuthHeadersAsync_WithToken_ReturnsHeaders()
    {
        // Arrange
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Act
        var headers = await _authenticationSlice.GetAuthHeadersAsync();

        // Assert
        Assert.NotNull(headers);
        Assert.True(headers.ContainsKey("Authorization"));
        Assert.StartsWith("Bearer ", headers["Authorization"]);
    }

    [Fact]
    public async Task AuthenticationSlice_GetAuthHeadersAsync_WithAdminKey_ReturnsHeaders()
    {
        // Arrange
        await _authenticationSlice.SetAdminAuthAsync(TestAdminKey);

        // Act
        var headers = await _authenticationSlice.GetAuthHeadersAsync();

        // Assert
        Assert.NotNull(headers);
        Assert.True(headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task AuthenticationSlice_GetAuthHeadersAsync_WhenUnauthenticated_ReturnsEmptyHeaders()
    {
        // Act
        var headers = await _authenticationSlice.GetAuthHeadersAsync();

        // Assert
        Assert.NotNull(headers);
        Assert.Empty(headers);
    }

    #endregion

    #region AuthenticationStateChanged Event Tests

    [Fact]
    public async Task AuthenticationSlice_AuthenticationStateChanged_WhenSettingToken_FiresEvent()
    {
        // Arrange
        AuthenticationStateChangedEventArgs? capturedArgs = null;
        _authenticationSlice.AuthenticationStateChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        // Act
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(AuthenticationState.Authenticated, capturedArgs.State);
    }

    [Fact]
    public async Task AuthenticationSlice_AuthenticationStateChanged_WhenClearing_FiresEvent()
    {
        // Arrange
        await _authenticationSlice.SetAuthTokenAsync(TestToken);
        AuthenticationStateChangedEventArgs? capturedArgs = null;
        _authenticationSlice.AuthenticationStateChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        // Act
        await _authenticationSlice.ClearAuthAsync();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(AuthenticationState.Unauthenticated, capturedArgs.State);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_AuthenticationStateChanged_MultipleSubscriptions_AllFire()
    {
        // Arrange
        int eventCount = 0;
        _authenticationSlice.AuthenticationStateChanged += (sender, args) => eventCount++;
        _authenticationSlice.AuthenticationStateChanged += (sender, args) => eventCount++;

        // Act
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Assert
        Assert.Equal(2, eventCount);
    }

    #endregion

    #region State Transition Tests

    [Fact]
    public async Task AuthenticationSlice_SetAuthToken_ThenSetAdminAuth_ReplacesToken()
    {
        // Arrange
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Act
        await _authenticationSlice.SetAdminAuthAsync(TestAdminKey);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
        Assert.Null(_authenticationSlice.CurrentAuthToken); // Admin auth doesn't set CurrentAuthToken
    }

    [Fact]
    public async Task AuthenticationSlice_SetAdminAuth_ThenSetAuthToken_ReplacesAdminAuth()
    {
        // Arrange
        await _authenticationSlice.SetAdminAuthAsync(TestAdminKey);

        // Act
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
        Assert.Equal(TestToken, _authenticationSlice.CurrentAuthToken);
    }

    [Fact]
    public async Task AuthenticationSlice_SetAuthTokenProvider_ThenSetAuthToken_ReplacesProvider()
    {
        // Arrange
        var mockProvider = new Mock<IAuthTokenProvider>();
        await _authenticationSlice.SetAuthTokenProviderAsync(mockProvider.Object);

        // Act
        await _authenticationSlice.SetAuthTokenAsync(TestToken);

        // Assert
        Assert.Equal(AuthenticationState.Authenticated, _authenticationSlice.AuthenticationState);
        Assert.Equal(TestToken, _authenticationSlice.CurrentAuthToken);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task AuthenticationSlice_ConcurrentSetAuthToken_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Multiple threads setting tokens concurrently
        for (int i = 0; i < 10; i++)
        {
            var token = $"token-{i}";
            tasks.Add(Task.Run(async () =>
            {
                await _authenticationSlice.SetAuthTokenAsync(token);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and should have a valid state
        Assert.True(_authenticationSlice.AuthenticationState == AuthenticationState.Authenticated ||
                     _authenticationSlice.AuthenticationState == AuthenticationState.Unauthenticated);
    }

    #endregion
}


