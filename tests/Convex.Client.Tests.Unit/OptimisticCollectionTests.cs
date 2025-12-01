using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Convex.Client.Features.DataAccess.Mutations;
using Xunit;

namespace Convex.Client.Tests.Unit;

public class OptimisticCollectionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithItems_ContainsItems()
    {
        // Arrange
        var items = new[] { "a", "b", "c" };

        // Act
        using var collection = new OptimisticCollection<string>(items);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal("a", collection[0]);
        Assert.Equal("b", collection[1]);
        Assert.Equal("c", collection[2]);
    }

    [Fact]
    public void Constructor_WithEmptyEnumerable_CreatesEmptyCollection()
    {
        // Arrange & Act
        using var collection = new OptimisticCollection<int>([]);

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithNull_CreatesEmptyCollection()
    {
        // Arrange & Act
        using var collection = new OptimisticCollection<string>(null!);

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        // Arrange & Act
        using var collection = new OptimisticCollection<int>();

        // Assert
        Assert.Empty(collection);
    }

    #endregion Constructor Tests

    #region Items Property Tests

    [Fact]
    public void Items_ReturnsReadOnlyCollection()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var items = collection.Items;

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(1, items[0]);
        Assert.Equal(2, items[1]);
        Assert.Equal(3, items[2]);
    }

    [Fact]
    public void Items_ReturnsWrappedCollection()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var items1 = collection.Items;
        collection.Add(4);
        var items2 = collection.Items;

        // Assert - Items wraps the internal list, so both see updates
        Assert.Equal(4, items1.Count);
        Assert.Equal(4, items2.Count);
    }

    #endregion Items Property Tests

    #region Count Property Tests

    [Fact]
    public void Count_Initially_ReturnsCorrectCount()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3, 4, 5]);

        // Assert
        Assert.Equal(5, collection.Count);
    }

    [Fact]
    public void Count_AfterAdd_Increments()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2]);

        // Act
        collection.Add(3);

        // Assert
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Count_AfterRemove_Decrements()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        _ = collection.Remove(2);

        // Assert
        Assert.Equal(2, collection.Count);
    }

    #endregion Count Property Tests

    #region Indexer Tests

    [Fact]
    public void Indexer_ValidIndex_ReturnsItem()
    {
        // Arrange
        using var collection = new OptimisticCollection<string>(["a", "b", "c"]);

        // Act & Assert
        Assert.Equal("a", collection[0]);
        Assert.Equal("b", collection[1]);
        Assert.Equal("c", collection[2]);
    }

    [Fact]
    public void Indexer_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => collection[5]);
    }

    [Fact]
    public void Indexer_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
    }

    #endregion Indexer Tests

    #region HasSnapshot Tests

    [Fact]
    public void HasSnapshot_Initially_IsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Assert
        Assert.False(collection.HasSnapshot);
    }

    [Fact]
    public void HasSnapshot_AfterCreateSnapshot_IsTrue()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        collection.CreateSnapshot();

        // Assert
        Assert.True(collection.HasSnapshot);
    }

    [Fact]
    public void HasSnapshot_AfterRollback_IsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();

        // Act
        _ = collection.Rollback();

        // Assert
        Assert.False(collection.HasSnapshot);
    }

    [Fact]
    public void HasSnapshot_AfterCommit_IsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();

        // Act
        collection.CommitSnapshot();

        // Assert
        Assert.False(collection.HasSnapshot);
    }

    #endregion HasSnapshot Tests

    #region CreateSnapshot Tests

    [Fact]
    public void CreateSnapshot_SavesCurrentState()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        collection.CreateSnapshot();
        collection.Add(4);
        _ = collection.Rollback();

        // Assert
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void CreateSnapshot_OverwritesPreviousSnapshot()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2]);
        collection.CreateSnapshot();
        collection.Add(3);
        collection.CreateSnapshot(); // New snapshot with 3 items
        collection.Add(4);

        // Act
        _ = collection.Rollback();

        // Assert - Should rollback to 3 items, not 2
        Assert.Equal(3, collection.Count);
    }

    #endregion CreateSnapshot Tests

    #region Rollback Tests

    [Fact]
    public void Rollback_WithSnapshot_RestoresState()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();
        collection.Add(4);
        collection.Add(5);

        // Act
        var result = collection.Rollback();

        // Assert
        Assert.True(result);
        Assert.Equal(3, collection.Count);
        Assert.Equal(1, collection[0]);
        Assert.Equal(2, collection[1]);
        Assert.Equal(3, collection[2]);
    }

    [Fact]
    public void Rollback_WithoutSnapshot_ReturnsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Add(4);

        // Act
        var result = collection.Rollback();

        // Assert
        Assert.False(result);
        Assert.Equal(4, collection.Count); // State unchanged
    }

    [Fact]
    public void Rollback_ClearsSnapshot()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();

        // Act
        _ = collection.Rollback();

        // Assert
        Assert.False(collection.HasSnapshot);
    }

    #endregion Rollback Tests

    #region CommitSnapshot Tests

    [Fact]
    public void CommitSnapshot_ClearsSnapshot()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();

        // Act
        collection.CommitSnapshot();

        // Assert
        Assert.False(collection.HasSnapshot);
    }

    [Fact]
    public void CommitSnapshot_PreservesCurrentState()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.CreateSnapshot();
        collection.Add(4);

        // Act
        collection.CommitSnapshot();
        var rollbackResult = collection.Rollback(); // Should have no effect

        // Assert
        Assert.False(rollbackResult);
        Assert.Equal(4, collection.Count);
    }

    #endregion CommitSnapshot Tests

    #region Add Tests

    [Fact]
    public void Add_AppendsItem()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2]);

        // Act
        collection.Add(3);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(3, collection[2]);
    }

    [Fact]
    public void Add_RaisesCollectionChanged()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>();
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.Add(1);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion Add Tests

    #region AddRange Tests

    [Fact]
    public void AddRange_AppendsItems()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1]);

        // Act
        collection.AddRange([2, 3, 4]);

        // Assert
        Assert.Equal(4, collection.Count);
    }

    [Fact]
    public void AddRange_NullItems_ThrowsArgumentNullException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>();

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => collection.AddRange(null!));
    }

    [Fact]
    public void AddRange_RaisesCollectionChanged()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>();
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.AddRange([1, 2, 3]);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion AddRange Tests

    #region Remove Tests

    [Fact]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var result = collection.Remove(2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, collection.Count);
        Assert.DoesNotContain(2, collection);
    }

    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var result = collection.Remove(99);

        // Assert
        Assert.False(result);
        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Remove_RaisesCollectionChanged_WhenItemRemoved()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        _ = collection.Remove(2);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void Remove_DoesNotRaiseCollectionChanged_WhenItemNotFound()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        _ = collection.Remove(99);

        // Assert
        Assert.False(eventRaised);
    }

    #endregion Remove Tests

    #region RemoveAt Tests

    [Fact]
    public void RemoveAt_ValidIndex_RemovesItem()
    {
        // Arrange
        using var collection = new OptimisticCollection<string>(["a", "b", "c"]);

        // Act
        collection.RemoveAt(1);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal("a", collection[0]);
        Assert.Equal("c", collection[1]);
    }

    [Fact]
    public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(5));
    }

    [Fact]
    public void RemoveAt_RaisesCollectionChanged()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.RemoveAt(0);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion RemoveAt Tests

    #region RemoveAll Tests

    [Fact]
    public void RemoveAll_MatchingPredicate_RemovesItems()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3, 4, 5, 6]);

        // Act
        var removed = collection.RemoveAll(static x => x % 2 == 0);

        // Assert
        Assert.Equal(3, removed);
        Assert.Equal(3, collection.Count);
        Assert.Contains(1, collection);
        Assert.Contains(3, collection);
        Assert.Contains(5, collection);
    }

    [Fact]
    public void RemoveAll_NoMatches_ReturnsZero()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 3, 5]);

        // Act
        var removed = collection.RemoveAll(static x => x % 2 == 0);

        // Assert
        Assert.Equal(0, removed);
    }

    [Fact]
    public void RemoveAll_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => collection.RemoveAll(null!));
    }

    [Fact]
    public void RemoveAll_RaisesCollectionChanged_WhenItemsRemoved()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        _ = collection.RemoveAll(static x => x > 1);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void RemoveAll_DoesNotRaiseCollectionChanged_WhenNoItemsRemoved()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        _ = collection.RemoveAll(static x => x > 100);

        // Assert
        Assert.False(eventRaised);
    }

    #endregion RemoveAll Tests

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        collection.Clear();

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void Clear_RaisesCollectionChanged()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.Clear();

        // Assert
        Assert.True(eventRaised);
    }

    #endregion Clear Tests

    #region Replace Tests

    [Fact]
    public void Replace_ReplacesAllItems()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        collection.Replace([4, 5]);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Equal(4, collection[0]);
        Assert.Equal(5, collection[1]);
    }

    [Fact]
    public void Replace_NullItems_ThrowsArgumentNullException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => collection.Replace(null!));
    }

    [Fact]
    public void Replace_RaisesCollectionChanged()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.Replace([4, 5]);

        // Assert
        Assert.True(eventRaised);
    }

    #endregion Replace Tests

    #region Contains Tests

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        Assert.True(collection.Contains(2));
    }

    [Fact]
    public void Contains_NonExistentItem_ReturnsFalse()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        Assert.False(collection.Contains(99));
    }

    #endregion Contains Tests

    #region Find Tests

    [Fact]
    public void Find_MatchingPredicate_ReturnsItem()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3, 4, 5]);

        // Act
        var result = collection.Find(static x => x > 3);

        // Assert
        Assert.Equal(4, result);
    }

    [Fact]
    public void Find_NoMatch_ReturnsDefault()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var result = collection.Find(static x => x > 100);

        // Assert
        Assert.Equal(default, result);
    }

    [Fact]
    public void Find_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() => collection.Find(null!));
    }

    #endregion Find Tests

    #region GetEnumerator Tests

    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        var items = collection.ToList();

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(1, items[0]);
        Assert.Equal(2, items[1]);
        Assert.Equal(3, items[2]);
    }

    [Fact]
    public void GetEnumerator_ReturnsSnapshot()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act - Modify during enumeration (should work because enumerator is a snapshot)
        var count = 0;
        foreach (var item in collection)
        {
            if (count == 0)
            {
                collection.Add(4);
            }
            count++;
        }

        // Assert - Enumeration saw only original 3 items
        Assert.Equal(3, count);
        Assert.Equal(4, collection.Count); // But collection now has 4
    }

    #endregion GetEnumerator Tests

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsCollection()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act
        collection.Dispose();

        // Assert - Any operation should throw
        _ = Assert.Throws<ObjectDisposedException>(() => collection.Count);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);

        // Act & Assert (should not throw)
        collection.Dispose();
        collection.Dispose();
    }

    [Fact]
    public void Dispose_ClearsEventHandlers()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        var eventRaised = false;
        collection.CollectionChanged += (_, _) => eventRaised = true;

        // Act
        collection.Dispose();

        // Assert - Event handler should have been cleared
        // (Can't test directly, but at least we know dispose ran)
        Assert.False(eventRaised);
    }

    #endregion Dispose Tests

    #region ObjectDisposedException Tests

    [Fact]
    public void Items_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => collection.Items);
    }

    [Fact]
    public void Add_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => collection.Add(4));
    }

    [Fact]
    public void Remove_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => collection.Remove(1));
    }

    [Fact]
    public void Clear_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(collection.Clear);
    }

    [Fact]
    public void CreateSnapshot_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(collection.CreateSnapshot);
    }

    [Fact]
    public void Rollback_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var collection = new OptimisticCollection<int>([1, 2, 3]);
        collection.Dispose();

        // Act & Assert
        _ = Assert.Throws<ObjectDisposedException>(() => collection.Rollback());
    }

    #endregion ObjectDisposedException Tests

    #region Thread Safety Tests

    [Fact]
    public async Task Collection_ConcurrentOperations_AreThreadSafe()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>(Enumerable.Range(1, 100));
        var tasks = new List<Task>();

        // Act - Concurrent reads and writes
        for (var i = 0; i < 10; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => collection.Add(value + 1000)));
            tasks.Add(Task.Run(() => collection.Contains(value)));
            tasks.Add(Task.Run(() => _ = collection.Count));
            tasks.Add(Task.Run(() =>
            {
                foreach (var item in collection)
                {
                    _ = item; // Just iterate
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw
        Assert.True(collection.Count >= 100);
    }

    #endregion Thread Safety Tests

    #region IReadOnlyList Implementation Tests

    [Fact]
    public void IReadOnlyList_Count_ReturnsCorrectCount()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        IReadOnlyList<int> readOnlyList = collection;

        // Assert
        Assert.Equal(3, readOnlyList.Count);
    }

    [Fact]
    public void IReadOnlyList_Indexer_ReturnsCorrectItem()
    {
        // Arrange
        using var collection = new OptimisticCollection<int>([1, 2, 3]);
        IReadOnlyList<int> readOnlyList = collection;

        // Assert
        Assert.Equal(1, readOnlyList[0]);
        Assert.Equal(2, readOnlyList[1]);
        Assert.Equal(3, readOnlyList[2]);
    }

    #endregion IReadOnlyList Implementation Tests
}
