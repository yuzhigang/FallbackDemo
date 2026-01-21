using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;
using Microsoft.Extensions.Logging;

namespace Arim.Drivers.Fallback.Core.Services;

/// <summary>
/// 补偿任务生成服务的默认实现
/// </summary>
public class FallbackTaskProducer(
    IFallbackService fallbackService,
    IFallbackSourceResolver sourceResolver,
    ILogger<FallbackTaskProducer> logger) 
    : IFallbackTaskProducer
{
    public async ValueTask CreateTaskAsync(
        string driverId, 
        DateTime startTime, 
        DateTime endTime, 
        IReadOnlyList<string>? tagIds = null, 
        CancellationToken ct = default)
    {
        if (startTime >= endTime) return;

        // 如果 tagIds 为空，说明是驱动级故障
        if (tagIds == null || tagIds.Count == 0)
        {
            await ProduceBatchedTasksAsync(driverId, null, startTime, endTime, null, ct);
            return;
        }

        // 核心改进：批量解析并分组
        // 这一步解决了：1. 补偿源映射 2. 不同标签分流 3. 标签 ID 到 Address 的转换
        var sourceGroups = await sourceResolver.GroupTagsBySourceAsync(driverId, tagIds, ct);

        foreach (var group in sourceGroups)
        {
            var sourceId = group.Key;
            var tagsInGroup = group.Value.ToDictionary(t => t.TagId, t => t.FallbackAddress);

            await ProduceBatchedTasksAsync(driverId, sourceId, startTime, endTime, tagsInGroup, ct);
        }
    }

    public async ValueTask CreateTagTaskAsync(
        string driverId, 
        string tagId, 
        DateTime startTime, 
        DateTime endTime, 
        CancellationToken ct = default)
    {
        await CreateTaskAsync(driverId, startTime, endTime, new[] { tagId }, ct);
    }

    private async ValueTask ProduceBatchedTasksAsync(
        string driverId,
        string? sourceId,
        DateTime startTime,
        DateTime endTime,
        IReadOnlyDictionary<string, string>? tags,
        CancellationToken ct)
    {
        sourceId ??= await sourceResolver.ResolveSourceIdAsync(driverId, ct);
        if (string.IsNullOrEmpty(sourceId))
        {
            logger.LogWarning("No fallback source configured for driver: {DriverId}. Task dropped.", driverId);
            return;
        }

        var maxTimeRange = TimeSpan.FromHours(1);
        var currentStart = startTime;

        while (currentStart < endTime)
        {
            var currentEnd = currentStart + maxTimeRange;
            if (currentEnd > endTime) currentEnd = endTime;

            var task = new FallbackTask(
                DriverId: driverId,
                FallbackSourceId: sourceId,
                StartTime: currentStart,
                EndTime: currentEnd,
                Tags: tags
            );

            await fallbackService.EnqueueAsync(task, ct);
            currentStart = currentEnd;
        }
    }
}
