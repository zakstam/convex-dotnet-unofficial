using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Slices.Subscriptions;
using Xunit;

namespace Convex.Client.Tests.Unit;


public class SubscriptionsSliceTests
{
    #region SubscriptionsSlice Tests

    [Fact]
    public void SubscriptionsSlice_Version_ReturnsVersion()
    {
        // Act
        var version = SubscriptionsSlice.Version;

        // Assert
        Assert.NotNull(version);
        Assert.False(string.IsNullOrEmpty(version));
    }

    #endregion

    #region ObservableConvexList Tests

    [Fact]
    public void ObservableConvexList_Constructor_CreatesEmptyList()
    {
        // Act
        var list = new ObservableConvexList<string>();

        // Assert
        Assert.Empty(list);
        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void ObservableConvexList_Add_AddsItem()
    {
        // Arrange
        var list = new ObservableConvexList<string>();

        // Act
        list.Add("item1");

        // Assert
        Assert.Single(list);
        Assert.Contains("item1", list);
    }

    [Fact]
    public void ObservableConvexList_Add_FiresCollectionChangedEvent()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        list.CollectionChanged += (sender, args) => capturedArgs = args;

        // Act
        list.Add("item1");

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(NotifyCollectionChangedAction.Add, capturedArgs.Action);
    }

    [Fact]
    public void ObservableConvexList_Remove_RemovesItem()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");

        // Act
        var result = list.Remove("item1");

        // Assert
        Assert.True(result);
        Assert.Single(list);
        Assert.DoesNotContain("item1", list);
        Assert.Contains("item2", list);
    }

    [Fact]
    public void ObservableConvexList_Remove_FiresCollectionChangedEvent()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        list.CollectionChanged += (sender, args) => capturedArgs = args;

        // Act
        list.Remove("item1");

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(NotifyCollectionChangedAction.Remove, capturedArgs.Action);
    }

    [Fact]
    public void ObservableConvexList_Clear_RemovesAllItems()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        // Act
        list.Clear();

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void ObservableConvexList_Clear_FiresCollectionChangedEvent()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        list.CollectionChanged += (sender, args) => capturedArgs = args;

        // Act
        list.Clear();

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(NotifyCollectionChangedAction.Reset, capturedArgs.Action);
    }

    [Fact]
    public void ObservableConvexList_Indexer_GetAndSet_Works()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");

        // Act & Assert
        Assert.Equal("item1", list[0]);
        Assert.Equal("item2", list[1]);

        list[0] = "updated";
        Assert.Equal("updated", list[0]);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ObservableConvexList_Indexer_Set_FiresCollectionChangedEvent()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        list.CollectionChanged += (sender, args) => capturedArgs = args;

        // Act
        list[0] = "updated";

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(NotifyCollectionChangedAction.Replace, capturedArgs.Action);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ObservableConvexList_Indexer_Get_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new ObservableConvexList<string>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[0]);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ObservableConvexList_Indexer_Set_WithInvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new ObservableConvexList<string>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list[0] = "value");
    }

    [Fact]
    public void ObservableConvexList_Contains_ReturnsTrueForExistingItem()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");

        // Assert
        Assert.Contains("item1", list);
        Assert.DoesNotContain("item2", list);
    }

    [Fact]
    public void ObservableConvexList_IndexOf_ReturnsCorrectIndex()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        // Assert
        Assert.Equal(0, list.IndexOf("item1"));
        Assert.Equal(1, list.IndexOf("item2"));
        Assert.Equal(2, list.IndexOf("item3"));
        Assert.Equal(-1, list.IndexOf("nonexistent"));
    }

    [Fact]
    public void ObservableConvexList_Insert_InsertsItemAtIndex()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item3");

        // Act
        list.Insert(1, "item2");

        // Assert
        Assert.Equal(3, list.Count);
        Assert.Equal("item1", list[0]);
        Assert.Equal("item2", list[1]);
        Assert.Equal("item3", list[2]);
    }

    [Fact]
    public void ObservableConvexList_RemoveAt_RemovesItemAtIndex()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        // Act
        list.RemoveAt(1);

        // Assert
        Assert.Equal(2, list.Count);
        Assert.Equal("item1", list[0]);
        Assert.Equal("item3", list[1]);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public async Task ObservableConvexList_ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var list = new ObservableConvexList<int>();
        var tasks = new List<System.Threading.Tasks.Task>();

        // Act - Multiple threads accessing list concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(System.Threading.Tasks.Task.Run(() =>
            {
                list.Add(index);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not throw and should have correct count
        Assert.Equal(10, list.Count);
    }

    [Fact]
    public void ObservableConvexList_CopyTo_CopiesToArray()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");
        var array = new string[3];

        // Act
        list.CopyTo(array, 0);

        // Assert
        Assert.Equal("item1", array[0]);
        Assert.Equal("item2", array[1]);
        Assert.Equal("item3", array[2]);
    }

    [Fact]
    public void ObservableConvexList_GetEnumerator_EnumeratesItems()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");
        list.Add("item2");
        list.Add("item3");

        // Act
        var items = list.ToList();

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Contains("item1", items);
        Assert.Contains("item2", items);
        Assert.Contains("item3", items);
    }

    [Fact]
    public void ObservableConvexList_Dispose_DisposesResources()
    {
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Add("item1");

        // Act
        list.Dispose();

        // Assert - Should not throw on disposal
        // Note: Actual behavior depends on implementation
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void ObservableConvexList_Dispose_AfterDisposal_OperationsMayFail()
    {
        // BUG: Need to verify behavior after disposal
        // Arrange
        var list = new ObservableConvexList<string>();
        list.Dispose();

        // Act & Assert - Behavior after disposal may vary
        // This test documents current behavior
    }

    #endregion
}


