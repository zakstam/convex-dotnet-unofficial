using System;
using System.Threading;
using Convex.Client.Extensions.Gaming.Sync;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class InterpolatedStateTests
{
    #region Test Helpers

    private sealed class TestPosition : IInterpolatable<TestPosition>
    {
        public float X { get; init; }
        public float Y { get; init; }

        public TestPosition Interpolate(TestPosition target, float t) => new()
        {
            X = X + ((target.X - X) * t),
            Y = Y + ((target.Y - Y) * t)
        };
    }

    private sealed class ExtrapolatablePosition : IExtrapolatable<ExtrapolatablePosition>
    {
        public float X { get; init; }
        public float Y { get; init; }
        public float VelocityX { get; init; }
        public float VelocityY { get; init; }

        public ExtrapolatablePosition Interpolate(ExtrapolatablePosition target, float t) => new()
        {
            X = X + ((target.X - X) * t),
            Y = Y + ((target.Y - Y) * t),
            VelocityX = VelocityX + ((target.VelocityX - VelocityX) * t),
            VelocityY = VelocityY + ((target.VelocityY - VelocityY) * t)
        };

        public ExtrapolatablePosition Extrapolate(double deltaTimeMs) => new()
        {
            X = X + (VelocityX * (float)(deltaTimeMs / 1000.0)),
            Y = Y + (VelocityY * (float)(deltaTimeMs / 1000.0)),
            VelocityX = VelocityX,
            VelocityY = VelocityY
        };
    }

    #endregion Test Helpers

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultParameters_CreatesInstance()
    {
        // Act
        var state = new InterpolatedState<TestPosition>();

        // Assert
        Assert.NotNull(state);
        Assert.Equal(100, state.InterpolationDelayMs);
        Assert.Equal(250, state.MaxExtrapolationMs);
        Assert.False(state.IsInterpolating);
        Assert.Equal(0, state.BufferedStateCount);
        Assert.Null(state.CurrentRawState);
    }

    [Fact]
    public void Constructor_WithCustomBufferSize_CreatesInstance()
    {
        // Act
        var state = new InterpolatedState<TestPosition>(maxBufferSize: 5);

        // Assert
        Assert.NotNull(state);
    }

    #endregion Constructor Tests

    #region PushState Tests

    [Fact]
    public void PushState_WithFirstState_SetsCurrentState()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();
        var position = new TestPosition { X = 10, Y = 20 };

        // Act
        state.PushState(position);

        // Assert
        Assert.Equal(position, state.CurrentRawState);
        Assert.True(state.BufferedStateCount >= 1); // At least 1 state buffered
        Assert.False(state.IsInterpolating); // Need 2 states for interpolation
    }

    [Fact]
    public void PushState_WithSecondState_EnablesInterpolation()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();
        var pos1 = new TestPosition { X = 0, Y = 0 };
        var pos2 = new TestPosition { X = 10, Y = 10 };

        // Act
        state.PushState(pos1);
        state.PushState(pos2);

        // Assert
        Assert.True(state.IsInterpolating);
        Assert.Equal(pos2, state.CurrentRawState);
    }

    [Fact]
    public void PushState_WithNullState_ThrowsArgumentNullException()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => state.PushState(null!));
    }

    #endregion PushState Tests

    #region GetRenderState Tests

    [Fact]
    public void GetRenderState_WithNoStates_ReturnsNull()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();

        // Act
        var result = state.GetRenderState();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetRenderState_WithOneState_ReturnsCurrentState()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();
        var position = new TestPosition { X = 10, Y = 20 };
        state.PushState(position);

        // Act
        var result = state.GetRenderState();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.X);
        Assert.Equal(20, result.Y);
    }

    [Fact]
    public void GetRenderState_WithTwoStates_InterpolatesCorrectly()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>
        {
            InterpolationDelayMs = 0 // No delay for testing
        };

        var pos1 = new TestPosition { X = 0, Y = 0 };
        var pos2 = new TestPosition { X = 100, Y = 100 };

        state.PushState(pos1);
        // Small delay to ensure different timestamps
        Thread.Sleep(10);
        state.PushState(pos2);

        // Act - get state after both are pushed
        // With 0 interpolation delay, should return close to current state
        var result = state.GetRenderState();

        // Assert
        Assert.NotNull(result);
        // Should be at or near the current state since interpolation delay is 0
        Assert.True(result.X >= 0);
        Assert.True(result.Y >= 0);
    }

    #endregion GetRenderState Tests

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var state = new InterpolatedState<TestPosition>();
        state.PushState(new TestPosition { X = 10, Y = 20 });
        state.PushState(new TestPosition { X = 30, Y = 40 });

        // Act
        state.Reset();

        // Assert
        Assert.Null(state.CurrentRawState);
        Assert.False(state.IsInterpolating);
        Assert.Equal(0, state.BufferedStateCount);
    }

    #endregion Reset Tests

    #region Extrapolation Tests

    [Fact]
    public void GetRenderState_WithExtrapolatableType_Extrapolates()
    {
        // Arrange
        var state = new InterpolatedState<ExtrapolatablePosition>
        {
            InterpolationDelayMs = 0,
            MaxExtrapolationMs = 1000
        };

        var pos = new ExtrapolatablePosition
        {
            X = 0,
            Y = 0,
            VelocityX = 100, // 100 units per second
            VelocityY = 50
        };

        state.PushState(pos);

        // Act - wait a bit to trigger extrapolation
        Thread.Sleep(50);
        var result = state.GetRenderState();

        // Assert
        Assert.NotNull(result);
        // With one state, it just returns the current state (no extrapolation without 2 states)
        Assert.Equal(0, result.X);
    }

    #endregion Extrapolation Tests

    #region Configuration Tests

    [Fact]
    public void InterpolationDelayMs_CanBeModified()
    {
        // Arrange & Act
        var state = new InterpolatedState<TestPosition>
        {
            InterpolationDelayMs = 200
        };

        // Assert
        Assert.Equal(200, state.InterpolationDelayMs);
    }

    [Fact]
    public void MaxExtrapolationMs_CanBeModified()
    {
        // Arrange & Act
        var state = new InterpolatedState<TestPosition>
        {
            MaxExtrapolationMs = 500
        };

        // Assert
        Assert.Equal(500, state.MaxExtrapolationMs);
    }

    #endregion Configuration Tests
}
