using System;
using System.Threading.Tasks;
using Convex.Client.Features.RealTime.Subscriptions;
using Convex.Client.Infrastructure.ErrorHandling;
using Convex.Client.Infrastructure.Validation;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class SchemaValidationExceptionTests
{
    [Fact]
    public void SchemaValidationException_InheritsFromConvexException()
    {
        // Arrange & Act
        var exception = new SchemaValidationException(
            "testFunction",
            "string",
            "int",
            "Type mismatch");

        // Assert
        Assert.IsAssignableFrom<ConvexException>(exception);
    }

    [Fact]
    public void SchemaValidationException_CanBeCaughtAsConvexException()
    {
        // Arrange
        var exception = new SchemaValidationException(
            "testFunction",
            "string",
            "int",
            "Type mismatch");

        // Act & Assert
        bool caughtByConvexException = false;
        try
        {
            throw exception;
        }
        catch (ConvexException)
        {
            caughtByConvexException = true;
        }

        Assert.True(caughtByConvexException);
    }

    [Fact]
    public void SchemaValidationException_PreservesAllProperties()
    {
        // Arrange
        var functionName = "myFunction";
        var expectedType = "string";
        var actualType = "number";
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var exception = new SchemaValidationException(
            functionName,
            expectedType,
            actualType,
            errors);

        // Assert
        Assert.Equal(functionName, exception.FunctionName);
        Assert.Equal(expectedType, exception.ExpectedType);
        Assert.Equal(actualType, exception.ActualType);
        Assert.Equal(2, exception.ValidationErrors.Count);
        Assert.Contains("Error 1", exception.ValidationErrors);
        Assert.Contains("Error 2", exception.ValidationErrors);
    }

    [Fact]
    public void SchemaValidationException_MessageContainsRelevantDetails()
    {
        // Arrange & Act
        var exception = new SchemaValidationException(
            "testFunction",
            "string",
            "int",
            "Type mismatch");

        // Assert
        Assert.Contains("testFunction", exception.Message);
        Assert.Contains("string", exception.Message);
        Assert.Contains("int", exception.Message);
    }
}

public class ObservableConvexListDisposalTests
{
    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var list = new ObservableConvexList<string>();

        // Act & Assert - Multiple dispose calls should not throw
        list.Dispose();
        list.Dispose();
        list.Dispose();
    }

    [Fact]
    public void Add_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list.Add("test"));
    }

    [Fact]
    public void Count_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item");
        list.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _ = list.Count);
    }

    [Fact]
    public void BindToObservable_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Dispose();

        var testItems = new[] { "test" };
        var observable = System.Reactive.Linq.Observable.Return(testItems);

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => list.BindToObservable(observable));
    }

    [Fact]
    public async Task ConcurrentDisposeAndAccess_IsThreadSafe()
    {
        // Arrange
        var list = new ObservableConvexList<int>();
        for (int i = 0; i < 100; i++)
        {
            list.Add(i);
        }

        // Act - Concurrent dispose and access attempts
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                tasks[i] = Task.Run(() => list.Dispose());
            }
            else
            {
                tasks[i] = Task.Run(() =>
                {
                    try { list.Add(999); }
                    catch (ObjectDisposedException) { /* Expected */ }
                });
            }
        }

        // Should not deadlock or throw unexpected exceptions
        await Task.WhenAll(tasks);

        // Assert - List should be disposed
        Assert.Throws<ObjectDisposedException>(() => list.Add(1));
    }
}
