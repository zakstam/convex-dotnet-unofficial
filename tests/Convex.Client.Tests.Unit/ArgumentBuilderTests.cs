using System;
using Convex.Client.Infrastructure.ArgumentBuilders;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class ArgumentBuilderTests
{
    #region Test Types

    private class SimpleArgs
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private class ComplexArgs
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public double? Score { get; set; }
    }

    private class NestedArgs
    {
        public string? RoomId { get; set; }
        public SimpleArgs? Nested { get; set; }
    }

    #endregion Test Types

    #region ArgumentBuilder<TArgs> Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesNewInstance()
    {
        // Act
        var builder = new ArgumentBuilder<SimpleArgs>();
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_Default_CreatesInstanceWithDefaultValues()
    {
        // Act
        var builder = new ArgumentBuilder<SimpleArgs>();
        var result = builder.Build();

        // Assert
        Assert.Null(result.Name);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Constructor_WithExistingArgs_UsesProvidedArgs()
    {
        // Arrange
        var existingArgs = new SimpleArgs { Name = "Test", Value = 42 };

        // Act
        var builder = new ArgumentBuilder<SimpleArgs>(existingArgs);
        var result = builder.Build();

        // Assert
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Constructor_WithNullArgs_CreatesNewInstance()
    {
        // Act
        var builder = new ArgumentBuilder<SimpleArgs>(null!);
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    #endregion ArgumentBuilder<TArgs> Constructor Tests

    #region Set Method Tests

    [Fact]
    public void Set_SingleProperty_SetsValue()
    {
        // Act
        var result = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "TestName")
            .Build();

        // Assert
        Assert.Equal("TestName", result.Name);
    }

    [Fact]
    public void Set_MultipleProperties_SetsAllValues()
    {
        // Act
        var result = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "TestName")
            .Set(a => a.Value = 100)
            .Build();

        // Assert
        Assert.Equal("TestName", result.Name);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public void Set_ReturnsSameBuilder_ForMethodChaining()
    {
        // Arrange
        var builder = new ArgumentBuilder<SimpleArgs>();

        // Act
        var returnedBuilder = builder.Set(a => a.Name = "Test");

        // Assert
        Assert.Same(builder, returnedBuilder);
    }

    [Fact]
    public void Set_Chained_SetsAllValues()
    {
        // Act
        var result = new ArgumentBuilder<ComplexArgs>()
            .Set(a => a.Id = "123")
            .Set(a => a.Title = "Test Title")
            .Set(a => a.Count = 5)
            .Set(a => a.IsActive = true)
            .Set(a => a.Score = 99.5)
            .Build();

        // Assert
        Assert.Equal("123", result.Id);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal(5, result.Count);
        Assert.True(result.IsActive);
        Assert.Equal(99.5, result.Score);
    }

    [Fact]
    public void Set_WithNullSetter_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ArgumentBuilder<SimpleArgs>();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => builder.Set(null!));
    }

    [Fact]
    public void Set_OverwritesPreviousValue()
    {
        // Act
        var result = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "First")
            .Set(a => a.Name = "Second")
            .Build();

        // Assert
        Assert.Equal("Second", result.Name);
    }

    [Fact]
    public void Set_WithComplexAction_Works()
    {
        // Act
        var result = new ArgumentBuilder<ComplexArgs>()
            .Set(a =>
            {
                a.Id = "complex-id";
                a.Title = "Complex Title";
                a.Count = 10;
            })
            .Build();

        // Assert
        Assert.Equal("complex-id", result.Id);
        Assert.Equal("Complex Title", result.Title);
        Assert.Equal(10, result.Count);
    }

    #endregion Set Method Tests

    #region Build Method Tests

    [Fact]
    public void Build_ReturnsSameInstance()
    {
        // Arrange
        var builder = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "Test");

        // Act
        var result1 = builder.Build();
        var result2 = builder.Build();

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void Build_WithoutSetting_ReturnsDefaultInstance()
    {
        // Arrange
        var builder = new ArgumentBuilder<SimpleArgs>();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Equal(0, result.Value);
    }

    #endregion Build Method Tests

    #region Implicit Operator Tests

    [Fact]
    public void ImplicitOperator_ConvertsToArgs()
    {
        // Arrange
        var builder = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "Implicit")
            .Set(a => a.Value = 42);

        // Act
        SimpleArgs result = builder;

        // Assert
        Assert.Equal("Implicit", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ImplicitOperator_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ArgumentBuilder<SimpleArgs>? builder = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => (SimpleArgs)builder!);
    }

    #endregion Implicit Operator Tests

    #region Static Factory Tests

    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        // Act
        var builder = ArgumentBuilder.Create<SimpleArgs>();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Create_BuildReturnsDefaultInstance()
    {
        // Act
        var result = ArgumentBuilder.Create<SimpleArgs>().Build();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void Create_AllowsChaining()
    {
        // Act
        var result = ArgumentBuilder.Create<SimpleArgs>()
            .Set(a => a.Name = "Created")
            .Set(a => a.Value = 123)
            .Build();

        // Assert
        Assert.Equal("Created", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void From_WithExistingArgs_CreatesBuilderWithValues()
    {
        // Arrange
        var existing = new SimpleArgs { Name = "Existing", Value = 999 };

        // Act
        var builder = ArgumentBuilder.From(existing);
        var result = builder.Build();

        // Assert
        Assert.Equal("Existing", result.Name);
        Assert.Equal(999, result.Value);
    }

    [Fact]
    public void From_AllowsModification()
    {
        // Arrange
        var existing = new SimpleArgs { Name = "Original", Value = 100 };

        // Act
        var result = ArgumentBuilder.From(existing)
            .Set(a => a.Name = "Modified")
            .Build();

        // Assert
        Assert.Equal("Modified", result.Name);
        Assert.Equal(100, result.Value); // Unchanged
    }

    [Fact]
    public void From_WithNull_CreatesNewInstance()
    {
        // Act
        var builder = ArgumentBuilder.From<SimpleArgs>(null!);
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
    }

    #endregion Static Factory Tests

    #region Edge Cases

    [Fact]
    public void NestedObject_CanBeSet()
    {
        // Act
        var result = new ArgumentBuilder<NestedArgs>()
            .Set(a => a.RoomId = "room-123")
            .Set(a => a.Nested = new SimpleArgs { Name = "Nested", Value = 50 })
            .Build();

        // Assert
        Assert.Equal("room-123", result.RoomId);
        Assert.NotNull(result.Nested);
        Assert.Equal("Nested", result.Nested.Name);
        Assert.Equal(50, result.Nested.Value);
    }

    [Fact]
    public void EmptyBuilder_ProducesValidResult()
    {
        // Act
        var result = new ArgumentBuilder<SimpleArgs>().Build();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Builder_CanSetNullValue()
    {
        // Act
        var result = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "NotNull")
            .Set(a => a.Name = null)
            .Build();

        // Assert
        Assert.Null(result.Name);
    }

    [Fact]
    public void MultipleBuilders_AreIndependent()
    {
        // Arrange
        var builder1 = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "Builder1");
        var builder2 = new ArgumentBuilder<SimpleArgs>()
            .Set(a => a.Name = "Builder2");

        // Act
        var result1 = builder1.Build();
        var result2 = builder2.Build();

        // Assert
        Assert.Equal("Builder1", result1.Name);
        Assert.Equal("Builder2", result2.Name);
    }

    #endregion Edge Cases
}
