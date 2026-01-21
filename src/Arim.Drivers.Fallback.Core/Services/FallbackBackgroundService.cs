using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arim.Drivers.Fallback.Core.Services;

/// <summary>
/// 动态补偿执行管理器
/// </summary>
public class FallbackBackgroundService : BackgroundService
{
    private readonly FallbackService _fallbackService;
    private readonly IEnumerable<IFallbackDriver> _fallbackDrivers;
    private readonly IFallbackSourceResolver _sourceResolver;
    private readonly ILogger<FallbackBackgroundService> _logger;

    public FallbackBackgroundService(
        IFallbackService fallbackService,
        IEnumerable<IFallbackDriver> fallbackDrivers,
        IFallbackSourceResolver sourceResolver,
        ILogger<FallbackBackgroundService> logger)
    {
        _fallbackService = (FallbackService)fallbackService;
        _fallbackDrivers = fallbackDrivers;
        _sourceResolver = sourceResolver;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fallback Manager Service is starting.");

        // 监听 FallbackService 发出的“新通道创建”通知
        await foreach (var reader in _fallbackService.NewChannelReader.ReadAllAsync(stoppingToken))
        {
            // 为每个新通道启动一个独立的 Worker 任务
            _ = Task.Run(() => StartWorkerAsync(reader, stoppingToken), stoppingToken);
        }
    }

    private async Task StartWorkerAsync(ChannelReader<FallbackTask> reader, CancellationToken ct)
    {
        string? currentSourceId = null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var tasks = new List<FallbackTask>();

                // 等待第一个任务以确定 SourceId 并开始批处理
                if (await reader.WaitToReadAsync(ct))
                {
                    while (tasks.Count < 100 && reader.TryRead(out var task))
                    {
                        currentSourceId ??= task.FallbackSourceId;
                        tasks.Add(task);
                    }

                    if (tasks.Count == 0) continue;

                    // 1. 合并任务（仅限本通道/本源的任务）
                    var mergedTasks = TaskMerger.Merge(tasks);
                    
                    _logger.LogDebug("[Source:{SourceId}] Merged {OriginalCount} tasks into {MergedCount}", 
                        currentSourceId, tasks.Count, mergedTasks.Count);

                    // 2. 执行任务
                    foreach (var mTask in mergedTasks)
                    {
                        await ProcessTaskWithDriverAsync(mTask, ct);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in Worker for Source: {SourceId}", currentSourceId);
        }
    }

    private async Task ProcessTaskWithDriverAsync(FallbackTask task, CancellationToken ct)
    {
        // 精确匹配对应的补偿驱动实例
        var driver = _fallbackDrivers.FirstOrDefault(d => d.InstanceId == task.FallbackSourceId);

        if (driver == null)
        {
            _logger.LogWarning("No registered FallbackDriver found for SourceId: {SourceId}", task.FallbackSourceId);
            return;
        }

        try
        {
            // 1. 解析标签：如果是全驱动补偿，则在此刻解析出该驱动下的所有采集点
            var tagsToRead = await ResolveTagsAsync(task, ct);

            if (tagsToRead == null || tagsToRead.Count == 0)
            {
                _logger.LogWarning("[{SourceId}] No tags resolved for Driver {DriverId}. Task skipped.", 
                    task.FallbackSourceId, task.DriverId);
                return;
            }

            // 2. 分块执行：如果采集点过多（例如 > 1000 个），分解成多次读取请求
            // 这样可以避免单次请求过大导致的超时或补偿驱动负载过高
            const int MaxTagsPerRequest = 1000;
            var tagChunks = tagsToRead.Chunk(MaxTagsPerRequest).ToList();
            
            int totalCompensated = 0;
            foreach (var chunk in tagChunks)
            {
                var chunkTags = chunk.ToDictionary(t => t.Key, t => t.Value);
                var context = new FallbackReadContext(task.DriverId, task.StartTime, task.EndTime, chunkTags);
                var results = await driver.ReadAsync(context, ct);
                totalCompensated += results.Count();
            }
            
            _logger.LogInformation("[{SourceId}] Successfully compensated {Count} points for Driver {DriverId} (Total Tags: {TagCount}, Chunks: {ChunkCount})", 
                task.FallbackSourceId, totalCompensated, task.DriverId, tagsToRead.Count, tagChunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{SourceId}] Compensation failed for Driver {DriverId}", 
                task.FallbackSourceId, task.DriverId);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> ResolveTagsAsync(FallbackTask task, CancellationToken ct)
    {
        // 如果任务本身带了标签，直接使用
        if (!task.IsWholeDriver)
        {
            return task.Tags!;
        }

        // 如果是全驱动任务，则从 resolver 获取该驱动在当前补偿源下的所有标签
        var driverTags = await _sourceResolver.GetTagsByDriverAsync(task.DriverId, ct);
        if (driverTags != null && driverTags.TryGetValue(task.FallbackSourceId, out var tags))
        {
            return tags.ToDictionary(t => t.TagId, t => t.FallbackAddress);
        }

        return new Dictionary<string, string>();
    }
}
