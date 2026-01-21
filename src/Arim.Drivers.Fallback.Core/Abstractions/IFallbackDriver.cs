using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Abstractions;

/// <summary>
/// 补偿驱动实现接口
/// </summary>
public interface IFallbackDriver
{
    /// <summary>
    /// 补偿源的唯一标识（对应 FallbackTask.FallbackSourceId）
    /// </summary>
    string InstanceId { get; }

    /// <summary>
    /// 驱动类型（如 InSQL, CSV）
    /// </summary>
    string DriverType { get; }

    /// <summary>
    /// 执行数据补偿读取
    /// </summary>
    Task<IEnumerable<TagValue>> ReadAsync(FallbackReadContext context, CancellationToken ct = default);
}
