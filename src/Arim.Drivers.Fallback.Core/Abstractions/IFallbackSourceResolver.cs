using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Abstractions;

public interface IFallbackSourceResolver
{
    /// <summary>
    /// 获取驱动对应的默认补偿源 ID
    /// </summary>
    Task<string?> ResolveSourceIdAsync(string driverId, CancellationToken ct = default);

    /// <summary>
    /// 将一组标签按补偿源 ID 进行分组。
    /// Key 为补偿源 ID，Value 为该源下的标签定义列表。
    /// </summary>
    Task<IDictionary<string, List<DriverTag>>> GroupTagsBySourceAsync(string driverId, IEnumerable<string> tagIds, CancellationToken ct = default);

    /// <summary>
    /// 获取驱动下的所有采集点，并按补偿源进行分组。
    /// </summary>
    Task<IDictionary<string, List<DriverTag>>> GetTagsByDriverAsync(string driverId, CancellationToken ct = default);
}
