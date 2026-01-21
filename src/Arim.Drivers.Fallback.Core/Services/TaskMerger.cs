using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Services;

public static class TaskMerger
{
    /// <summary>
    /// 合并任务逻辑
    /// 基本思路：相同 DriverId 的任务，如果时间窗口重叠或紧邻，则进行合并
    /// </summary>
    public static List<FallbackTask> Merge(IEnumerable<FallbackTask> tasks)
    {
        var groupedTasks = tasks.GroupBy(t => t.DriverId);
        var mergedResults = new List<FallbackTask>();

        foreach (var group in groupedTasks)
        {
            var orderedTasks = group.OrderBy(t => t.StartTime).ToList();
            if (orderedTasks.Count == 0) continue;

            var current = orderedTasks[0];

            for (int i = 1; i < orderedTasks.Count; i++)
            {
                var next = orderedTasks[i];

                // 如果时间重叠或间距很小（例如小于1秒），则尝试合并
                if (next.StartTime <= current.EndTime.AddSeconds(1))
                {
                    // 更新结束时间
                    var newEnd = next.EndTime > current.EndTime ? next.EndTime : current.EndTime;
                    
                    // 合并标签列表
                    Dictionary<string, string>? mergedTags = null;
                    if (!current.IsWholeDriver && !next.IsWholeDriver)
                    {
                        mergedTags = new Dictionary<string, string>(current.Tags!);
                        foreach (var tag in next.Tags!)
                        {
                            mergedTags[tag.Key] = tag.Value;
                        }
                    }
                    // 如果其中一个是整个驱动补偿，则结果也是整个驱动补偿
                    
                    current = current with { EndTime = newEnd, Tags = mergedTags };
                }
                else
                {
                    mergedResults.Add(current);
                    current = next;
                }
            }
            mergedResults.Add(current);
        }

        return mergedResults;
    }
}
