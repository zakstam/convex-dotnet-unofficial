using Convex.Client.Infrastructure.Validation;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class SchemaValidationOptionsTests
{
    #region Default Constructor Tests

    [Fact]
    public void Constructor_Default_ValidateOnQueryIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.ValidateOnQuery);
    }

    [Fact]
    public void Constructor_Default_ValidateOnMutationIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.ValidateOnMutation);
    }

    [Fact]
    public void Constructor_Default_ValidateOnActionIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.ValidateOnAction);
    }

    [Fact]
    public void Constructor_Default_ValidateOnSubscriptionIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.ValidateOnSubscription);
    }

    [Fact]
    public void Constructor_Default_ThrowOnValidationErrorIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.ThrowOnValidationError);
    }

    [Fact]
    public void Constructor_Default_StrictTypeCheckingIsFalse()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions();

        // Assert
        Assert.False(options.StrictTypeChecking);
    }

    #endregion Default Constructor Tests

    #region Strict Factory Tests

    [Fact]
    public void Strict_ValidateOnQueryIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.ValidateOnQuery);
    }

    [Fact]
    public void Strict_ValidateOnMutationIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.ValidateOnMutation);
    }

    [Fact]
    public void Strict_ValidateOnActionIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.ValidateOnAction);
    }

    [Fact]
    public void Strict_ValidateOnSubscriptionIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.ValidateOnSubscription);
    }

    [Fact]
    public void Strict_ThrowOnValidationErrorIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.ThrowOnValidationError);
    }

    [Fact]
    public void Strict_StrictTypeCheckingIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.Strict();

        // Assert
        Assert.True(options.StrictTypeChecking);
    }

    #endregion Strict Factory Tests

    #region LogOnly Factory Tests

    [Fact]
    public void LogOnly_ValidateOnQueryIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.True(options.ValidateOnQuery);
    }

    [Fact]
    public void LogOnly_ValidateOnMutationIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.True(options.ValidateOnMutation);
    }

    [Fact]
    public void LogOnly_ValidateOnActionIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.True(options.ValidateOnAction);
    }

    [Fact]
    public void LogOnly_ValidateOnSubscriptionIsTrue()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.True(options.ValidateOnSubscription);
    }

    [Fact]
    public void LogOnly_ThrowOnValidationErrorIsFalse()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.False(options.ThrowOnValidationError);
    }

    [Fact]
    public void LogOnly_StrictTypeCheckingIsFalse()
    {
        // Act
        var options = SchemaValidationOptions.LogOnly();

        // Assert
        Assert.False(options.StrictTypeChecking);
    }

    #endregion LogOnly Factory Tests

    #region Property Setter Tests

    [Fact]
    public void ValidateOnQuery_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { ValidateOnQuery = true };

        // Assert
        Assert.True(options.ValidateOnQuery);
    }

    [Fact]
    public void ValidateOnMutation_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { ValidateOnMutation = true };

        // Assert
        Assert.True(options.ValidateOnMutation);
    }

    [Fact]
    public void ValidateOnAction_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { ValidateOnAction = true };

        // Assert
        Assert.True(options.ValidateOnAction);
    }

    [Fact]
    public void ValidateOnSubscription_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { ValidateOnSubscription = true };

        // Assert
        Assert.True(options.ValidateOnSubscription);
    }

    [Fact]
    public void ThrowOnValidationError_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { ThrowOnValidationError = true };

        // Assert
        Assert.True(options.ThrowOnValidationError);
    }

    [Fact]
    public void StrictTypeChecking_CanBeSetToTrue()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions { StrictTypeChecking = true };

        // Assert
        Assert.True(options.StrictTypeChecking);
    }

    #endregion Property Setter Tests

    #region Object Initializer Tests

    [Fact]
    public void Options_CanBeCreatedWithObjectInitializer()
    {
        // Arrange & Act
        var options = new SchemaValidationOptions
        {
            ValidateOnQuery = true,
            ValidateOnMutation = true,
            ValidateOnAction = false,
            ValidateOnSubscription = false,
            ThrowOnValidationError = true,
            StrictTypeChecking = false
        };

        // Assert
        Assert.True(options.ValidateOnQuery);
        Assert.True(options.ValidateOnMutation);
        Assert.False(options.ValidateOnAction);
        Assert.False(options.ValidateOnSubscription);
        Assert.True(options.ThrowOnValidationError);
        Assert.False(options.StrictTypeChecking);
    }

    #endregion Object Initializer Tests
}
