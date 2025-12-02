using System;
using Convex.Client.Extensions.Gaming.Sync;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class PredictedStateTests
{
    #region Test Helpers

    private sealed class TestPlayerState : IPredictable<TestPlayerState, MoveInput>
    {
        public float X { get; init; }
        public float Y { get; init; }

        /// <summary>Gets or sets the speed in units per second.</summary>
        public float Speed { get; init; } = 100f;

        public TestPlayerState ApplyInput(MoveInput input, double deltaTimeMs)
        {
            var dt = (float)(deltaTimeMs / 1000.0);
            return new TestPlayerState
            {
                X = X + (input.DirectionX * Speed * dt),
                Y = Y + (input.DirectionY * Speed * dt),
                Speed = Speed
            };
        }

        public TestPlayerState Clone() => new()
        {
            X = X,
            Y = Y,
            Speed = Speed
        };
    }

    private sealed record MoveInput(float DirectionX, float DirectionY);

    #endregion Test Helpers

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidInitialState_CreatesInstance()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };

        // Act
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Assert
        Assert.NotNull(predicted);
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(0, predicted.NextSequenceId);
        Assert.True(predicted.IsEnabled);
    }

    [Fact]
    public void Constructor_WithCustomMaxPendingInputs_CreatesInstance()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };

        // Act
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState, maxPendingInputs: 10);

        // Assert
        Assert.NotNull(predicted);
    }

    [Fact]
    public void Constructor_WithNullInitialState_ThrowsArgumentNullException()
    {
        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            new PredictedState<TestPlayerState, MoveInput>(null!));
    }

    [Fact]
    public void Constructor_WithZeroMaxPendingInputs_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PredictedState<TestPlayerState, MoveInput>(initialState, maxPendingInputs: 0));
    }

    #endregion Constructor Tests

    #region ApplyInput Tests

    [Fact]
    public void ApplyInput_WithValidInput_UpdatesPredictedState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);
        var input = new MoveInput(1, 0); // Move right

        // Act
        var timestamped = predicted.ApplyInput(input, deltaTimeMs: 100); // 100ms

        // Assert
        Assert.Equal(0, timestamped.SequenceId);
        Assert.Equal(input, timestamped.Input);
        Assert.Equal(100, timestamped.DeltaTimeMs);
        Assert.Equal(1, predicted.PendingInputCount);

        // 100 units/sec * 0.1 sec = 10 units
        Assert.Equal(10, predicted.CurrentState.X);
        Assert.Equal(0, predicted.CurrentState.Y);
    }

    [Fact]
    public void ApplyInput_MultipleInputs_AccumulatesCorrectly()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Act
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // Move right
        _ = predicted.ApplyInput(new MoveInput(0, 1), deltaTimeMs: 100); // Move up
        _ = predicted.ApplyInput(new MoveInput(1, 1), deltaTimeMs: 100); // Move diagonal

        // Assert
        Assert.Equal(3, predicted.PendingInputCount);
        Assert.Equal(3, predicted.NextSequenceId);

        // X: 10 + 0 + 10 = 20
        // Y: 0 + 10 + 10 = 20
        Assert.Equal(20, predicted.CurrentState.X);
        Assert.Equal(20, predicted.CurrentState.Y);
    }

    [Fact]
    public void ApplyInput_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => predicted.ApplyInput(null!));
    }

    [Fact]
    public void ApplyInput_WhenDisabled_DoesNotApplyLocally()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState)
        {
            IsEnabled = false
        };

        // Act
        var timestamped = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        // Assert
        Assert.Equal(0, timestamped.SequenceId); // Still assigns sequence ID
        Assert.Equal(0, predicted.PendingInputCount); // But doesn't queue
        Assert.Equal(0, predicted.CurrentState.X); // And doesn't apply
    }

    [Fact]
    public void ApplyInput_ExceedsMaxPending_TrimsOldInputs()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState, maxPendingInputs: 3);

        // Act - apply 5 inputs
        for (var i = 0; i < 5; i++)
        {
            _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        }

        // Assert - should only keep 3 inputs
        Assert.Equal(3, predicted.PendingInputCount);
        Assert.Equal(5, predicted.NextSequenceId);
    }

    #endregion ApplyInput Tests

    #region OnServerState Tests

    [Fact]
    public void OnServerState_WithNoUnacknowledgedInputs_UpdatesState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 0
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 1

        var serverState = new TestPlayerState { X = 20, Y = 0 };

        // Act - server acknowledges all inputs
        predicted.OnServerState(serverState, lastProcessedInputId: 1);

        // Assert
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(20, predicted.CurrentState.X);
        Assert.Equal(20, predicted.ConfirmedState.X);
    }

    [Fact]
    public void OnServerState_WithUnacknowledgedInputs_ReappliesThem()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 0
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 1
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 2

        // Server state after processing input 0 only
        var serverState = new TestPlayerState { X = 10, Y = 0 };

        // Act - server acknowledges only input 0
        predicted.OnServerState(serverState, lastProcessedInputId: 0);

        // Assert
        Assert.Equal(2, predicted.PendingInputCount); // IDs 1 and 2 still pending
        Assert.Equal(10, predicted.ConfirmedState.X);
        // Re-applied: 10 + 10 + 10 = 30
        Assert.Equal(30, predicted.CurrentState.X);
    }

    [Fact]
    public void OnServerState_WithMisprediction_SnapsToServerState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100); // ID 0

        // Server has different state (e.g., collision detected)
        var serverState = new TestPlayerState { X = 5, Y = 0 }; // Only moved 5 units

        // Act
        predicted.OnServerState(serverState, lastProcessedInputId: 0);

        // Assert
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(5, predicted.ConfirmedState.X);
        Assert.Equal(5, predicted.CurrentState.X); // Snapped to server
    }

    [Fact]
    public void OnServerState_WhenDisabled_ClearsPendingAndUsesServerState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        predicted.IsEnabled = false;

        var serverState = new TestPlayerState { X = 50, Y = 50 };

        // Act
        predicted.OnServerState(serverState, lastProcessedInputId: -1);

        // Assert
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(50, predicted.CurrentState.X);
        Assert.Equal(50, predicted.CurrentState.Y);
    }

    [Fact]
    public void OnServerState_WithNullServerState_ThrowsArgumentNullException()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => predicted.OnServerState(null!, 0));
    }

    #endregion OnServerState Tests

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllPendingInputsAndUpdatesState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        var newState = new TestPlayerState { X = 100, Y = 100 };

        // Act
        predicted.Reset(newState);

        // Assert
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(100, predicted.ConfirmedState.X);
        Assert.Equal(100, predicted.CurrentState.X);
    }

    [Fact]
    public void Reset_WithNullState_ThrowsArgumentNullException()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => predicted.Reset(null!));
    }

    #endregion Reset Tests

    #region ClearPendingInputs Tests

    [Fact]
    public void ClearPendingInputs_ClearsQueueAndResetsToConfirmed()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        // Current state is 20, confirmed is 0

        // Act
        predicted.ClearPendingInputs();

        // Assert
        Assert.Equal(0, predicted.PendingInputCount);
        Assert.Equal(0, predicted.CurrentState.X); // Reset to confirmed
        Assert.Equal(0, predicted.ConfirmedState.X);
    }

    #endregion ClearPendingInputs Tests

    #region ConfirmedState and CurrentState Tests

    [Fact]
    public void ConfirmedState_ReturnsLastServerState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        var serverState = new TestPlayerState { X = 15, Y = 0 };
        predicted.OnServerState(serverState, lastProcessedInputId: 0);

        // Act
        var confirmed = predicted.ConfirmedState;

        // Assert
        Assert.Equal(15, confirmed.X);
    }

    [Fact]
    public void CurrentState_ReturnsPredictedState()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        _ = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        // Act
        var current = predicted.CurrentState;

        // Assert
        Assert.Equal(10, current.X);
    }

    #endregion ConfirmedState and CurrentState Tests

    #region TimestampedInput Tests

    [Fact]
    public void TimestampedInput_ContainsCorrectData()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);
        var input = new MoveInput(1, 0);

        // Act
        var timestamped = predicted.ApplyInput(input, deltaTimeMs: 50);

        // Assert
        Assert.Equal(0, timestamped.SequenceId);
        Assert.Equal(input, timestamped.Input);
        Assert.Equal(50, timestamped.DeltaTimeMs);
        Assert.True(timestamped.TimestampMs > 0);
    }

    [Fact]
    public void TimestampedInput_SequenceIdsAreIncremental()
    {
        // Arrange
        var initialState = new TestPlayerState { X = 0, Y = 0 };
        var predicted = new PredictedState<TestPlayerState, MoveInput>(initialState);

        // Act
        var t1 = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        var t2 = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);
        var t3 = predicted.ApplyInput(new MoveInput(1, 0), deltaTimeMs: 100);

        // Assert
        Assert.Equal(0, t1.SequenceId);
        Assert.Equal(1, t2.SequenceId);
        Assert.Equal(2, t3.SequenceId);
    }

    #endregion TimestampedInput Tests
}
