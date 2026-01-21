using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Abstractions;

/// <summary>
/// 补偿配置仓储接口，用于从持久化层获取驱动和标签的补偿映射关系
/// </summary>
public interface IFallbackRepository
{
    /// <summary>
    /// 获取驱动对应的默认补偿源 ID
    /// </summary>
    Task<string?> GetDefaultSourceIdAsync(string driverId, CancellationToken ct = default);

    /// <summary>
    /// 获取指定驱动下标签的补偿配置映射
    /// </summary>
    /// <returns>返回标签定义与其对应的补偿源 ID 的集合</returns>
    Task<IEnumerable<(DriverTag Tag, string SourceId)>> GetTagMappingsAsync(string driverId, IEnumerable<string> tagIds, CancellationToken ct = default);

    /// <summary>
    /// 获取驱动下所有标签的补偿配置映射
    /// </summary>
    Task<IEnumerable<(DriverTag Tag, string SourceId)>> GetAllTagMappingsAsync(string driverId, CancellationToken ct = default);
}
