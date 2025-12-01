using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convex.Client.Infrastructure.Caching;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class QueryDependencyRegistryTests
{
    #region DefineQueryDependency Tests

    [Fact]
    public void DefineQueryDependency_WithValidArgs_RegistersDependency()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        Assert.Contains("todos:list", queries);
    }

    [Fact]
    public void DefineQueryDependency_WithMultipleQueries_RegistersAll()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list", "todos:count", "todos:recent");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        Assert.Equal(3, queries.Count);
        Assert.Contains("todos:list", queries);
        Assert.Contains("todos:count", queries);
        Assert.Contains("todos:recent", queries);
    }

    [Fact]
    public void DefineQueryDependency_CalledMultipleTimes_AccumulatesQueries()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list");
        registry.DefineQueryDependency("todos:create", "todos:count");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        Assert.Equal(2, queries.Count);
        Assert.Contains("todos:list", queries);
        Assert.Contains("todos:count", queries);
    }

    [Fact]
    public void DefineQueryDependency_DuplicateQueries_OnlyAddsOnce()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list");
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        _ = Assert.Single(queries);
    }

    [Fact]
    public void DefineQueryDependency_NullMutationName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => registry.DefineQueryDependency(null!, "todos:list"));
    }

    [Fact]
    public void DefineQueryDependency_EmptyMutationName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => registry.DefineQueryDependency("", "todos:list"));
    }

    [Fact]
    public void DefineQueryDependency_WhitespaceMutationName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => registry.DefineQueryDependency("   ", "todos:list"));
    }

    [Fact]
    public void DefineQueryDependency_NullInvalidates_ThrowsArgumentException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => registry.DefineQueryDependency("todos:create", null!));
    }

    [Fact]
    public void DefineQueryDependency_EmptyInvalidates_ThrowsArgumentException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() => registry.DefineQueryDependency("todos:create"));
    }

    [Fact]
    public void DefineQueryDependency_SkipsEmptyQueryNames()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list", "", "  ", "todos:count");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        Assert.Equal(2, queries.Count);
        Assert.Contains("todos:list", queries);
        Assert.Contains("todos:count", queries);
    }

    [Fact]
    public void DefineQueryDependency_IsCaseInsensitive()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "TODOS:LIST");
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Assert
        var queries = registry.GetQueriesToInvalidate("todos:create");
        _ = Assert.Single(queries);
    }

    #endregion DefineQueryDependency Tests

    #region GetQueriesToInvalidate Tests

    [Fact]
    public void GetQueriesToInvalidate_WithDefinedDependencies_ReturnsQueries()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Act
        var queries = registry.GetQueriesToInvalidate("todos:create");

        // Assert
        _ = Assert.Single(queries);
        Assert.Contains("todos:list", queries);
    }

    [Fact]
    public void GetQueriesToInvalidate_WithNoDependencies_ReturnsEmpty()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        var queries = registry.GetQueriesToInvalidate("unknown:mutation");

        // Assert
        Assert.Empty(queries);
    }

    [Fact]
    public void GetQueriesToInvalidate_NullMutationName_ReturnsEmpty()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Act
        var queries = registry.GetQueriesToInvalidate(null!);

        // Assert
        Assert.Empty(queries);
    }

    [Fact]
    public void GetQueriesToInvalidate_EmptyMutationName_ReturnsEmpty()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Act
        var queries = registry.GetQueriesToInvalidate("");

        // Assert
        Assert.Empty(queries);
    }

    [Fact]
    public void GetQueriesToInvalidate_ReturnsNewCollection()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Act
        var queries1 = registry.GetQueriesToInvalidate("todos:create");
        var queries2 = registry.GetQueriesToInvalidate("todos:create");

        // Assert
        Assert.NotSame(queries1, queries2);
    }

    #endregion GetQueriesToInvalidate Tests

    #region RemoveDependencies Tests

    [Fact]
    public void RemoveDependencies_WithExistingDependency_ReturnsTrue()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");

        // Act
        var result = registry.RemoveDependencies("todos:create");

        // Assert
        Assert.True(result);
        Assert.Empty(registry.GetQueriesToInvalidate("todos:create"));
    }

    [Fact]
    public void RemoveDependencies_WithNonExistentDependency_ReturnsFalse()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        var result = registry.RemoveDependencies("unknown:mutation");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveDependencies_NullMutationName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => registry.RemoveDependencies(null!));
    }

    [Fact]
    public void RemoveDependencies_EmptyMutationName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => registry.RemoveDependencies(""));
    }

    #endregion RemoveDependencies Tests

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllDependencies()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        registry.DefineQueryDependency("todos:create", "todos:list");
        registry.DefineQueryDependency("todos:update", "todos:get");

        // Act
        registry.Clear();

        // Assert
        Assert.Equal(0, registry.Count);
        Assert.Empty(registry.GetQueriesToInvalidate("todos:create"));
        Assert.Empty(registry.GetQueriesToInvalidate("todos:update"));
    }

    [Fact]
    public void Clear_OnEmptyRegistry_DoesNotThrow()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act & Assert (should not throw)
        registry.Clear();
        Assert.Equal(0, registry.Count);
    }

    #endregion Clear Tests

    #region Count Tests

    [Fact]
    public void Count_Initially_IsZero()
    {
        // Arrange & Act
        var registry = new QueryDependencyRegistry();

        // Assert
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void Count_AfterAddingDependencies_ReflectsMutationCount()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list");
        registry.DefineQueryDependency("todos:update", "todos:get");

        // Assert
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void Count_SameMutationMultipleQueries_CountsAsOne()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();

        // Act
        registry.DefineQueryDependency("todos:create", "todos:list", "todos:count", "todos:recent");

        // Assert
        Assert.Equal(1, registry.Count);
    }

    #endregion Count Tests

    #region ExpandPattern Tests

    [Fact]
    public void ExpandPattern_WithNoWildcard_ReturnsPattern()
    {
        // Arrange
        var cachedQueries = new[] { "todos:list", "todos:count", "users:list" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("todos:list", cachedQueries).ToList();

        // Assert
        _ = Assert.Single(result);
        Assert.Equal("todos:list", result[0]);
    }

    [Fact]
    public void ExpandPattern_WithAsteriskWildcard_MatchesMultiple()
    {
        // Arrange
        var cachedQueries = new[] { "todos:list", "todos:count", "todos:get", "users:list" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("todos:*", cachedQueries).ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("todos:list", result);
        Assert.Contains("todos:count", result);
        Assert.Contains("todos:get", result);
    }

    [Fact]
    public void ExpandPattern_WithQuestionMarkWildcard_MatchesSingleChar()
    {
        // Arrange
        var cachedQueries = new[] { "todos:a", "todos:b", "todos:ab", "users:a" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("todos:?", cachedQueries).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("todos:a", result);
        Assert.Contains("todos:b", result);
    }

    [Fact]
    public void ExpandPattern_NullPattern_ReturnsEmpty()
    {
        // Arrange
        var cachedQueries = new[] { "todos:list" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern(null!, cachedQueries).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExpandPattern_EmptyPattern_ReturnsEmpty()
    {
        // Arrange
        var cachedQueries = new[] { "todos:list" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("", cachedQueries).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExpandPattern_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var cachedQueries = new[] { "todos:list", "todos:count" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("users:*", cachedQueries).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExpandPattern_CaseInsensitive()
    {
        // Arrange
        var cachedQueries = new[] { "TODOS:LIST", "todos:count" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("todos:*", cachedQueries).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ExpandPattern_ComplexPattern_Works()
    {
        // Arrange
        var cachedQueries = new[] { "app:todos:list", "app:todos:count", "app:users:list", "other:todos:list" };

        // Act
        var result = QueryDependencyRegistry.ExpandPattern("app:*:list", cachedQueries).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("app:todos:list", result);
        Assert.Contains("app:users:list", result);
    }

    #endregion ExpandPattern Tests

    #region Thread Safety Tests

    [Fact]
    public async Task Registry_ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        var registry = new QueryDependencyRegistry();
        var tasks = new List<Task>();

        // Act - Concurrent writes and reads
        for (int i = 0; i < 100; i++)
        {
            var mutationName = $"mutation:{i}";
            var queryName = $"query:{i}";

            tasks.Add(Task.Run(() => registry.DefineQueryDependency(mutationName, queryName)));
            tasks.Add(Task.Run(() => registry.GetQueriesToInvalidate(mutationName)));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and registry should be consistent
        Assert.True(registry.Count <= 100);
    }

    #endregion Thread Safety Tests
}
