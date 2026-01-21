namespace Arim.Drivers.Fallback.Core.Models;

/// <summary>
/// 补偿任务 DTO
/// </summary>
/// <param name="DriverId">发生故障的原始驱动实例 ID</param>
/// <param name="FallbackSourceId">指向的补偿源 ID（用于通道隔离和路由）</param>
/// <param name="StartTime">补偿开始时间</param>
/// <param name="EndTime">补偿结束时间</param>
/// <param name="Tags">需要补偿的标签列表</param>
/// <param name="Tags">需要补偿的标签映射列表（Key: 原始 ID, Value: 补偿地址）</param>
public record FallbackTask(
    string DriverId,
    string FallbackSourceId,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyDictionary<string, string>? Tags = null)
{
    public string TaskId { get; init; } = Guid.NewGuid().ToString("N");
    public bool IsWholeDriver => Tags == null || Tags.Count == 0;
}
