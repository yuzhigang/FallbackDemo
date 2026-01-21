using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Abstractions;

/// <summary>
/// 补偿任务产生服务，负责业务逻辑中故障检测到生成任务的转化
/// </summary>
public interface IFallbackTaskProducer
{
    /// <summary>
    /// 触发一个补偿任务的生成（支持驱动级或多标签级）
    /// </summary>
    ValueTask CreateTaskAsync(string driverId, DateTime startTime, DateTime endTime, IReadOnlyList<string>? tagIds = null, CancellationToken ct = default);

    /// <summary>
    /// 触发单个特定标签的补偿任务
    /// </summary>
    ValueTask CreateTagTaskAsync(string driverId, string tagId, DateTime startTime, DateTime endTime, CancellationToken ct = default);
}
