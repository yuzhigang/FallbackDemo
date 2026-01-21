using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Abstractions;

/// <summary>
/// 补偿服务接口（生产者端）
/// </summary>
public interface IFallbackService
{
    /// <summary>
    /// 提交一个补偿任务
    /// </summary>
    ValueTask EnqueueAsync(FallbackTask task, CancellationToken ct = default);
}
