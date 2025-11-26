using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convex.Client.Features.DataAccess.Caching;
using Xunit;

namespace Convex.Client.Tests.Unit;


public class CachingSliceTests
{
    private CachingSlice _cachingSlice = null!;

    public CachingSliceTests()
    {
        _cachingSlice = new CachingSlice();
    }

    #region CachingSlice Entry Point Tests

    [Fact]
    public void CachingSlice_TryGet_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = _cachingSlice.TryGet<string>("nonexistent", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_TryGet_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.TryGet<string>(null!, out _));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_TryGet_WithEmptyKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.TryGet<string>("", out _));
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.TryGet<string>("   ", out _));
    }

    [Fact]
    public void CachingSlice_Set_WithValue_StoresValue()
    {
        // Arrange
        var key = "test:key";
        var value = "test value";

        // Act
        _cachingSlice.Set(key, value);

        // Assert
        Assert.True(_cachingSlice.TryGet<string>(key, out var retrievedValue));
        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_Set_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.Set<string>(null!, "value"));
    }

    [Fact]
    public void CachingSlice_Set_OverwritesExistingValue()
    {
        // Arrange
        var key = "test:key";
        _cachingSlice.Set(key, "original");

        // Act
        _cachingSlice.Set(key, "updated");

        // Assert
        Assert.True(_cachingSlice.TryGet<string>(key, out var value));
        Assert.Equal("updated", value);
    }

    [Fact]
    public void CachingSlice_TryUpdate_WithExistingValue_UpdatesValue()
    {
        // Arrange
        var key = "test:key";
        _cachingSlice.Set(key, 10);

        // Act
        var result = _cachingSlice.TryUpdate<int>(key, x => x * 2);

        // Assert
        Assert.True(result);
        Assert.True(_cachingSlice.TryGet<int>(key, out var updatedValue));
        Assert.Equal(20, updatedValue);
    }

    [Fact]
    public void CachingSlice_TryUpdate_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = _cachingSlice.TryUpdate<int>("nonexistent", x => x * 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_TryUpdate_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.TryUpdate<int>(null!, x => x));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_TryUpdate_WithNullUpdateFn_ThrowsArgumentNullException()
    {
        // Arrange
        _cachingSlice.Set("test:key", 10);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.TryUpdate<int>("test:key", null!));
    }

    [Fact]
    public void CachingSlice_Remove_WithExistingKey_RemovesValue()
    {
        // Arrange
        var key = "test:key";
        _cachingSlice.Set(key, "value");

        // Act
        var result = _cachingSlice.Remove(key);

        // Assert
        Assert.True(result);
        Assert.False(_cachingSlice.TryGet<string>(key, out _));
    }

    [Fact]
    public void CachingSlice_Remove_WithNonExistentKey_ReturnsFalse()
    {
        // Act
        var result = _cachingSlice.Remove("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_Remove_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.Remove(null!));
    }

    [Fact]
    public void CachingSlice_RemovePattern_WithMatchingKeys_RemovesMatchingKeys()
    {
        // Arrange
        _cachingSlice.Set("todos:list", new List<string>());
        _cachingSlice.Set("todos:count", 5);
        _cachingSlice.Set("users:list", new List<string>());

        // Act
        var removedCount = _cachingSlice.RemovePattern("todos:*");

        // Assert
        Assert.Equal(2, removedCount);
        Assert.False(_cachingSlice.TryGet<object>("todos:list", out _));
        Assert.False(_cachingSlice.TryGet<object>("todos:count", out _));
        Assert.True(_cachingSlice.TryGet<object>("users:list", out _));
    }

    [Fact]
    public void CachingSlice_RemovePattern_WithNoMatches_ReturnsZero()
    {
        // Arrange
        _cachingSlice.Set("todos:list", new List<string>());

        // Act
        var removedCount = _cachingSlice.RemovePattern("users:*");

        // Assert
        Assert.Equal(0, removedCount);
        Assert.True(_cachingSlice.TryGet<object>("todos:list", out _));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_RemovePattern_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.RemovePattern(null!));
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_RemovePattern_WithEmptyPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _cachingSlice.RemovePattern(""));
    }

    [Fact]
    public void CachingSlice_Clear_RemovesAllKeys()
    {
        // Arrange
        _cachingSlice.Set("key1", "value1");
        _cachingSlice.Set("key2", "value2");
        _cachingSlice.Set("key3", "value3");

        // Act
        _cachingSlice.Clear();

        // Assert
        Assert.Equal(0, _cachingSlice.Count);
        Assert.False(_cachingSlice.TryGet<string>("key1", out _));
        Assert.False(_cachingSlice.TryGet<string>("key2", out _));
        Assert.False(_cachingSlice.TryGet<string>("key3", out _));
    }

    [Fact]
    public void CachingSlice_Count_ReturnsCorrectCount()
    {
        // Arrange
        _cachingSlice.Set("key1", "value1");
        _cachingSlice.Set("key2", "value2");

        // Assert
        Assert.Equal(2, _cachingSlice.Count);
    }

    [Fact]
    public void CachingSlice_Keys_ReturnsAllKeys()
    {
        // Arrange
        _cachingSlice.Set("key1", "value1");
        _cachingSlice.Set("key2", "value2");

        // Act
        var keys = _cachingSlice.Keys.ToList();

        // Assert
        Assert.Equal(2, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
    }

    [Fact]
    [Trait("Category", "Bug")]
    public void CachingSlice_TryGet_WithWrongType_ReturnsFalse()
    {
        // BUG: Type mismatch handling - should return false when type doesn't match
        // Arrange
        var key = "test:key";
        _cachingSlice.Set(key, "string value");

        // Act
        var result = _cachingSlice.TryGet<int>(key, out var intValue);

        // Assert
        Assert.False(result);
        Assert.Equal(default(int), intValue);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task CachingSlice_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Multiple threads accessing cache concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                _cachingSlice.Set($"key{index}", $"value{index}");
                _cachingSlice.TryGet<string>($"key{index}", out var value);
                Assert.Equal($"value{index}", value);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, _cachingSlice.Count);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void CachingSlice_RemovePattern_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        _cachingSlice.Set("test:key:1", "value1");
        _cachingSlice.Set("test:key:2", "value2");
        _cachingSlice.Set("test:other", "value3");

        // Act
        var removedCount = _cachingSlice.RemovePattern("test:key:*");

        // Assert
        Assert.Equal(2, removedCount);
    }

    #endregion
}


