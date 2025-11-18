using System;
using System.Linq;
using System.Text.Json;
using Convex.Client.Slices.Diagnostics;
using Xunit;

namespace Convex.Client.Tests.Unit;


public class DiagnosticsSliceTests
{
    private DiagnosticsSlice _diagnosticsSlice = null!;

    public DiagnosticsSliceTests()
    {
        _diagnosticsSlice = new DiagnosticsSlice();
    }

    #region DiagnosticsSlice Entry Point Tests

    [Fact]
    public void DiagnosticsSlice_InitialState_HasPerformanceTracker()
    {
        // Assert
        Assert.NotNull(_diagnosticsSlice.Performance);
        Assert.IsAssignableFrom<IPerformanceTracker>(_diagnosticsSlice.Performance);
    }

    [Fact]
    public void DiagnosticsSlice_InitialState_HasDisconnectTracker()
    {
        // Assert
        Assert.NotNull(_diagnosticsSlice.Disconnects);
        Assert.IsAssignableFrom<IDisconnectTracker>(_diagnosticsSlice.Disconnects);
    }

    #endregion

    #region PerformanceTracker Tests

    [Fact]
    public void PerformanceTracker_Mark_CreatesMark()
    {
        // Act
        var mark = _diagnosticsSlice.Performance.Mark("test-mark");

        // Assert
        Assert.NotNull(mark);
        Assert.Equal("test-mark", mark.Name);
        Assert.Equal("mark", mark.EntryType);
    }

    [Fact]
    public void PerformanceTracker_Mark_WithDetail_CreatesMarkWithDetail()
    {
        // Arrange
        var detail = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;

        // Act
        var mark = _diagnosticsSlice.Performance.Mark("test-mark", detail);

        // Assert
        Assert.NotNull(mark);
        Assert.NotNull(mark.Detail);
    }

    [Fact]
    public void PerformanceTracker_Mark_AddsToEntries()
    {
        // Act
        _diagnosticsSlice.Performance.Mark("mark1");
        _diagnosticsSlice.Performance.Mark("mark2");

        // Assert
        var entries = _diagnosticsSlice.Performance.Entries;
        Assert.True(entries.Count >= 2);
        Assert.Contains(entries, e => e.Name == "mark1");
        Assert.Contains(entries, e => e.Name == "mark2");
    }

    [Fact]
    public void PerformanceTracker_Measure_CreatesMeasure()
    {
        // Act
        var measure = _diagnosticsSlice.Performance.Measure("test-measure");

        // Assert
        Assert.NotNull(measure);
        Assert.Equal("test-measure", measure.Name);
        Assert.Equal("measure", measure.EntryType);
    }

    [Fact]
    public void PerformanceTracker_Measure_WithStartAndEndMark_CreatesMeasure()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("start");
        System.Threading.Thread.Sleep(10);
        _diagnosticsSlice.Performance.Mark("end");

        // Act
        var measure = _diagnosticsSlice.Performance.Measure("test-measure", "start", "end");

        // Assert
        Assert.NotNull(measure);
        Assert.True(measure.Duration.TotalMilliseconds >= 0);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void PerformanceTracker_Measure_WithNonExistentMark_UsesCurrentTime()
    {
        // Act
        var measure = _diagnosticsSlice.Performance.Measure("test-measure", "nonexistent-start", "nonexistent-end");

        // Assert
        Assert.NotNull(measure);
        // Should still create a measure even if marks don't exist
    }

    [Fact]
    public void PerformanceTracker_GetEntriesByName_ReturnsMatchingEntries()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("test-mark");
        _diagnosticsSlice.Performance.Mark("other-mark");

        // Act
        var entries = _diagnosticsSlice.Performance.GetEntriesByName("test-mark");

        // Assert
        Assert.True(entries.Count > 0);
        Assert.True(entries.All(e => e.Name == "test-mark"));
    }

    [Fact]
    public void PerformanceTracker_GetEntriesByType_ReturnsMatchingEntries()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("mark1");
        _diagnosticsSlice.Performance.Measure("measure1");

        // Act
        var marks = _diagnosticsSlice.Performance.GetEntriesByType("mark");
        var measures = _diagnosticsSlice.Performance.GetEntriesByType("measure");

        // Assert
        Assert.True(marks.Count > 0);
        Assert.True(marks.All(e => e.EntryType == "mark"));
        Assert.True(measures.Count > 0);
        Assert.True(measures.All(e => e.EntryType == "measure"));
    }

    [Fact]
    public void PerformanceTracker_ClearMarks_RemovesAllMarks()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("mark1");
        _diagnosticsSlice.Performance.Mark("mark2");
        _diagnosticsSlice.Performance.Measure("measure1");

        // Act
        _diagnosticsSlice.Performance.ClearMarks();

        // Assert
        var marks = _diagnosticsSlice.Performance.GetEntriesByType("mark");
        Assert.Empty(marks);
        // Measures should still exist
        var measures = _diagnosticsSlice.Performance.GetEntriesByType("measure");
        Assert.True(measures.Count > 0);
    }

    [Fact]
    public void PerformanceTracker_ClearMeasures_RemovesAllMeasures()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("mark1");
        _diagnosticsSlice.Performance.Measure("measure1");
        _diagnosticsSlice.Performance.Measure("measure2");

        // Act
        _diagnosticsSlice.Performance.ClearMeasures();

        // Assert
        var measures = _diagnosticsSlice.Performance.GetEntriesByType("measure");
        Assert.Empty(measures);
        // Marks should still exist
        var marks = _diagnosticsSlice.Performance.GetEntriesByType("mark");
        Assert.True(marks.Count > 0);
    }

    [Fact]
    public void PerformanceTracker_Clear_RemovesAllEntries()
    {
        // Arrange
        _diagnosticsSlice.Performance.Mark("mark1");
        _diagnosticsSlice.Performance.Measure("measure1");

        // Act
        _diagnosticsSlice.Performance.Clear();

        // Assert
        Assert.Empty(_diagnosticsSlice.Performance.Entries);
    }

    #endregion

    #region DisconnectTracker Tests

    [Fact]
    public void DisconnectTracker_InitialState_IsNotDisconnected()
    {
        // Assert
        Assert.False(_diagnosticsSlice.Disconnects.IsDisconnected);
        Assert.Null(_diagnosticsSlice.Disconnects.CurrentDisconnectDuration);
        Assert.False(_diagnosticsSlice.Disconnects.IsLongDisconnect);
    }

    [Fact]
    public void DisconnectTracker_RecordDisconnect_SetsDisconnectedState()
    {
        // Act
        _diagnosticsSlice.Disconnects.RecordDisconnect();

        // Assert
        Assert.True(_diagnosticsSlice.Disconnects.IsDisconnected);
        Assert.NotNull(_diagnosticsSlice.Disconnects.CurrentDisconnectDuration);
    }

    [Fact]
    public void DisconnectTracker_RecordReconnect_ClearsDisconnectedState()
    {
        // Arrange
        _diagnosticsSlice.Disconnects.RecordDisconnect();

        // Act
        _diagnosticsSlice.Disconnects.RecordReconnect();

        // Assert
        Assert.False(_diagnosticsSlice.Disconnects.IsDisconnected);
        Assert.Null(_diagnosticsSlice.Disconnects.CurrentDisconnectDuration);
    }

    [Fact]
    public void DisconnectTracker_RecordDisconnect_AddsToHistory()
    {
        // Act
        _diagnosticsSlice.Disconnects.RecordDisconnect();
        System.Threading.Thread.Sleep(10);
        _diagnosticsSlice.Disconnects.RecordReconnect();

        // Assert
        var history = _diagnosticsSlice.Disconnects.DisconnectHistory;
        Assert.True(history.Count > 0);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void DisconnectTracker_RecordDisconnect_WithManyDisconnects_LimitsHistory()
    {
        // Arrange - Record more than max history size (default is 50)
        for (int i = 0; i < 60; i++)
        {
            _diagnosticsSlice.Disconnects.RecordDisconnect();
            _diagnosticsSlice.Disconnects.RecordReconnect();
        }

        // Assert - Should only keep the most recent 50
        var history = _diagnosticsSlice.Disconnects.DisconnectHistory;
        Assert.True(history.Count <= 50);
    }

    [Fact]
    public void DisconnectTracker_IsLongDisconnect_WithShortDisconnect_ReturnsFalse()
    {
        // Arrange
        _diagnosticsSlice.Disconnects.RecordDisconnect();

        // Assert - Default threshold is 30 seconds, so short disconnect should return false
        Assert.False(_diagnosticsSlice.Disconnects.IsLongDisconnect);
    }

    [Fact]
    public void DisconnectTracker_GetDisconnectHistory_ReturnsHistory()
    {
        // Arrange
        _diagnosticsSlice.Disconnects.RecordDisconnect();
        _diagnosticsSlice.Disconnects.RecordReconnect();
        _diagnosticsSlice.Disconnects.RecordDisconnect();
        _diagnosticsSlice.Disconnects.RecordReconnect();

        // Act
        var history = _diagnosticsSlice.Disconnects.DisconnectHistory;

        // Assert
        Assert.True(history.Count >= 2);
    }

    [Fact]
    [Trait("Category", "EdgeCase")]
    public void DisconnectTracker_RecordReconnect_WithoutDisconnect_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        _diagnosticsSlice.Disconnects.RecordReconnect();
    }

    #endregion
}


