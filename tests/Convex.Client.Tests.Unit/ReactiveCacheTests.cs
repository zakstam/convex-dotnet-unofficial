using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Convex.Client.Features.DataAccess.Caching;
using Convex.Client.Infrastructure.Caching;
using Xunit;

namespace Convex.Client.Tests.Unit;

/// <summary>
/// Unit tests for the ReactiveCacheImplementation class.
/// Verifies that SetAndNotify triggers observable subscribers.
/// </summary>
public class ReactiveCacheTests : IDisposable
{
    private readonly ReactiveCacheImplementation _cache = new();

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetObservable Tests

    [Fact]
    public void GetObservable_ReturnsNonNullObservable()
    {
        // Act
        var observable = _cache.GetObservable<string>("test:key");

        // Assert
        Assert.NotNull(observable);
    }

    [Fact]
    public void GetObservable_SameKey_ReturnsBothObservables()
    {
        // Act
        var observable1 = _cache.GetObservable<string>("test:key");
        var observable2 = _cache.GetObservable<string>("test:key");

        // Assert - Both should be backed by the same entry
        Assert.NotNull(observable1);
        Assert.NotNull(observable2);
    }

    #endregion GetObservable Tests

    #region SetAndNotify Tests

    [Fact]
    public async Task SetAndNotify_NotifiesSubscribers()
    {
        // Arrange
        string? receivedValue = null;
        var completionSource = new TaskCompletionSource<string?>();

        var observable = _cache.GetObservable<string>("test:key");
        using var subscription = observable.Subscribe(value =>
        {
            receivedValue = value;
            completionSource.TrySetResult(value);
        });

        // Act
        _cache.SetAndNotify("test:key", "hello", CacheEntrySource.OptimisticUpdate);

        // Wait for notification (with timeout)
        var result = await Task.WhenAny(completionSource.Task, Task.Delay(1000));

        // Assert
        Assert.Same(completionSource.Task, result);
        Assert.Equal("hello", receivedValue);
    }

    [Fact]
    public async Task SetAndNotify_NotifiesMultipleSubscribers()
    {
        // Arrange
        string? subscriber1Value = null;
        string? subscriber2Value = null;
        var tcs1 = new TaskCompletionSource<string?>();
        var tcs2 = new TaskCompletionSource<string?>();

        var observable = _cache.GetObservable<string>("test:key");
        using var subscription1 = observable.Subscribe(value =>
        {
            subscriber1Value = value;
            tcs1.TrySetResult(value);
        });
        using var subscription2 = observable.Subscribe(value =>
        {
            subscriber2Value = value;
            tcs2.TrySetResult(value);
        });

        // Act
        _cache.SetAndNotify("test:key", "shared value", CacheEntrySource.Query);

        // Wait for both notifications
        await Task.WhenAll(
            Task.WhenAny(tcs1.Task, Task.Delay(1000)),
            Task.WhenAny(tcs2.Task, Task.Delay(1000))
        );

        // Assert
        Assert.Equal("shared value", subscriber1Value);
        Assert.Equal("shared value", subscriber2Value);
    }

    [Fact]
    public async Task SetAndNotify_EmitsMultipleValues()
    {
        // Arrange
        var receivedValues = new List<string?>();
        var countLatch = new CountdownEvent(3);

        var observable = _cache.GetObservable<string>("test:key");
        using var subscription = observable.Subscribe(value =>
        {
            receivedValues.Add(value);
            countLatch.Signal();
        });

        // Act
        _cache.SetAndNotify("test:key", "first", CacheEntrySource.Query);
        _cache.SetAndNotify("test:key", "second", CacheEntrySource.Subscription);
        _cache.SetAndNotify("test:key", "third", CacheEntrySource.OptimisticUpdate);

        // Wait for all values
        var completed = countLatch.Wait(TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(completed, "Did not receive all 3 values");
        Assert.Contains("first", receivedValues);
        Assert.Contains("second", receivedValues);
        Assert.Contains("third", receivedValues);

        await Task.CompletedTask;
    }

    #endregion SetAndNotify Tests

    #region GetCurrentValue Tests

    [Fact]
    public void GetCurrentValue_BeforeSet_ReturnsDefault()
    {
        // Act
        var value = _cache.GetCurrentValue<string>("test:nonexistent");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetCurrentValue_AfterSetAndNotify_ReturnsValue()
    {
        // Arrange
        _cache.SetAndNotify("test:key", "cached value", CacheEntrySource.Query);

        // Act
        var value = _cache.GetCurrentValue<string>("test:key");

        // Assert
        Assert.Equal("cached value", value);
    }

    [Fact]
    public void GetCurrentValue_AfterMultipleSetAndNotify_ReturnsLatestValue()
    {
        // Arrange
        _cache.SetAndNotify("test:key", "value1", CacheEntrySource.Query);
        _cache.SetAndNotify("test:key", "value2", CacheEntrySource.Subscription);
        _cache.SetAndNotify("test:key", "value3", CacheEntrySource.OptimisticUpdate);

        // Act
        var value = _cache.GetCurrentValue<string>("test:key");

        // Assert
        Assert.Equal("value3", value);
    }

    #endregion GetCurrentValue Tests

    #region CacheEntrySource Tracking Tests

    [Fact]
    public void TryGetSource_AfterSetAndNotify_ReturnsCorrectSource()
    {
        // Arrange
        _cache.SetAndNotify("test:key", "value", CacheEntrySource.OptimisticUpdate);

        // Act
        var found = _cache.TryGetSource("test:key", out var source);

        // Assert
        Assert.True(found);
        Assert.Equal(CacheEntrySource.OptimisticUpdate, source);
    }

    [Fact]
    public void TryGetSource_AfterOverwrite_ReturnsNewSource()
    {
        // Arrange
        _cache.SetAndNotify("test:key", "value1", CacheEntrySource.Query);
        _cache.SetAndNotify("test:key", "value2", CacheEntrySource.OptimisticUpdate);

        // Act
        var found = _cache.TryGetSource("test:key", out var source);

        // Assert
        Assert.True(found);
        Assert.Equal(CacheEntrySource.OptimisticUpdate, source);
    }

    #endregion CacheEntrySource Tracking Tests

    #region IConvexCache Interface Tests

    [Fact]
    public void Set_CanBeRetrievedWithTryGet()
    {
        // Arrange
        _cache.Set("test:key", "stored value");

        // Act
        var found = _cache.TryGet<string>("test:key", out var value);

        // Assert
        Assert.True(found);
        Assert.Equal("stored value", value);
    }

    [Fact]
    public void Remove_RemovesEntry()
    {
        // Arrange
        _cache.Set("test:key", "value");
        Assert.True(_cache.TryGet<string>("test:key", out _));

        // Act
        var removed = _cache.Remove("test:key");

        // Assert
        Assert.True(removed);
        Assert.False(_cache.TryGet<string>("test:key", out _));
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        _cache.Set("key1", "value1");
        _cache.Set("key2", "value2");
        _cache.Set("key3", "value3");
        Assert.Equal(3, _cache.Count);

        // Act
        _cache.Clear();

        // Assert
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public void RemovePattern_RemovesMatchingEntries()
    {
        // Arrange
        _cache.Set("test:key1", "value1");
        _cache.Set("test:key2", "value2");
        _cache.Set("other:key", "value3");

        // Act
        var removed = _cache.RemovePattern("test:*");

        // Assert
        Assert.Equal(2, removed);
        Assert.False(_cache.TryGet<string>("test:key1", out _));
        Assert.False(_cache.TryGet<string>("test:key2", out _));
        Assert.True(_cache.TryGet<string>("other:key", out _));
    }

    #endregion IConvexCache Interface Tests

    #region Optimistic Update Simulation Tests

    [Fact]
    public async Task OptimisticUpdate_SimulateSetQueryNotifyingObservers()
    {
        // This test simulates the exact scenario we fixed:
        // 1. Subscribe to Observe() observable
        // 2. Call SetQuery() which should notify the subscriber

        // Arrange
        string? receivedValue = null;
        var tcs = new TaskCompletionSource<string?>();

        // Simulate what Observe() does internally
        var observable = _cache.GetObservable<string>("messages:query");
        using var subscription = observable.Subscribe(value =>
        {
            receivedValue = value;
            tcs.TrySetResult(value);
        });

        // Simulate what SetQuery() does internally
        _cache.SetAndNotify("messages:query", "optimistic message", CacheEntrySource.OptimisticUpdate);

        // Wait for notification
        var result = await Task.WhenAny(tcs.Task, Task.Delay(1000));

        // Assert
        Assert.Same(tcs.Task, result);
        Assert.Equal("optimistic message", receivedValue);
    }

    [Fact]
    public async Task Rollback_NotifiesObserversWithOriginalValue()
    {
        // This test simulates rollback behavior:
        // 1. Set original value (subscription)
        // 2. Apply optimistic update
        // 3. Rollback to original value

        // Arrange
        var receivedValues = new List<string?>();
        var countLatch = new CountdownEvent(3);

        var observable = _cache.GetObservable<string>("messages:query");
        using var subscription = observable.Subscribe(value =>
        {
            receivedValues.Add(value);
            countLatch.Signal();
        });

        // Act
        // 1. Original server value arrives
        _cache.SetAndNotify("messages:query", "server value", CacheEntrySource.Subscription);

        // 2. Optimistic update applied
        _cache.SetAndNotify("messages:query", "optimistic value", CacheEntrySource.OptimisticUpdate);

        // 3. Rollback (mutation failed)
        _cache.SetAndNotify("messages:query", "server value", CacheEntrySource.OptimisticUpdate);

        // Wait for all values
        var completed = countLatch.Wait(TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(completed, "Did not receive all 3 values");
        Assert.Equal(3, receivedValues.Count);
        Assert.Equal("server value", receivedValues[0]);
        Assert.Equal("optimistic value", receivedValues[1]);
        Assert.Equal("server value", receivedValues[2]); // Rollback restores original

        await Task.CompletedTask;
    }

    #endregion Optimistic Update Simulation Tests

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentSetAndNotify_DoesNotThrow()
    {
        // Arrange
        var tasks = new List<Task>();
        var observable = _cache.GetObservable<int>("test:concurrent");

        var receivedCount = 0;
        using var subscription = observable.Subscribe(_ => Interlocked.Increment(ref receivedCount));

        // Act - Fire many concurrent updates
        for (var i = 0; i < 100; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => _cache.SetAndNotify("test:concurrent", value, CacheEntrySource.Query)));
        }

        await Task.WhenAll(tasks);

        // Give a little time for all notifications to process
        await Task.Delay(100);

        // Assert - Should have received all notifications without throwing
        Assert.True(receivedCount > 0);
        Assert.True(_cache.TryGet<int>("test:concurrent", out _));
    }

    #endregion Thread Safety Tests
}
