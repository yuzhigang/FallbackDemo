using Arim.Drivers.Fallback.Core.Models;
using Arim.Drivers.Fallback.Core.Services;
using Xunit;

namespace Arim.Drivers.Fallback.Tests;

public class TaskMergerTests
{
    [Fact]
    public void Merge_OverlappingTasks_ShouldConsolidate()
    {
        // Arrange
        var baseTime = DateTime.Parse("2024-01-01 10:00:00");
        var tasks = new List<FallbackTask>
        {
            new FallbackTask("D1", "S1", baseTime, baseTime.AddMinutes(5), new Dictionary<string, string> { ["T1"] = "T1" }),
            new FallbackTask("D1", "S1", baseTime.AddMinutes(3), baseTime.AddMinutes(8), new Dictionary<string, string> { ["T2"] = "T2" })
        };

        // Act
        var merged = TaskMerger.Merge(tasks);

        // Assert
        Assert.Single(merged);
        Assert.Equal(baseTime, merged[0].StartTime);
        Assert.Equal(baseTime.AddMinutes(8), merged[0].EndTime);
        Assert.Contains("T1", merged[0].Tags!.Keys);
        Assert.Contains("T2", merged[0].Tags!.Keys);
    }

    [Fact]
    public void Merge_DifferentDrivers_ShouldNotConsolidate()
    {
        // Arrange
        var baseTime = DateTime.Parse("2024-01-01 10:00:00");
        var tasks = new List<FallbackTask>
        {
            new FallbackTask("D1", "S1", baseTime, baseTime.AddMinutes(5)),
            new FallbackTask("D2", "S1", baseTime, baseTime.AddMinutes(5))
        };

        // Act
        var merged = TaskMerger.Merge(tasks);

        // Assert
        Assert.Equal(2, merged.Count);
    }

    [Fact]
    public void Merge_WholeDriverTask_ShouldOverrideTagTasks()
    {
        // Arrange
        var baseTime = DateTime.Parse("2024-01-01 10:00:00");
        var tasks = new List<FallbackTask>
        {
            new FallbackTask("D1", "S1", baseTime, baseTime.AddMinutes(5), new Dictionary<string, string> { ["T1"] = "T1" }),
            new FallbackTask("D1", "S1", baseTime.AddMinutes(2), baseTime.AddMinutes(7), null) // Whole driver
        };

        // Act
        var merged = TaskMerger.Merge(tasks);

        // Assert
        Assert.Single(merged);
        Assert.True(merged[0].IsWholeDriver);
        Assert.Null(merged[0].Tags);
        Assert.Equal(baseTime.AddMinutes(7), merged[0].EndTime);
    }
}
