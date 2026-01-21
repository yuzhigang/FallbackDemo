namespace Arim.Drivers.Fallback.Core.Models;

/// <summary>
/// 补偿读取上下文
/// </summary>
/// <param name="Tags">标签映射（Key: 原始 ID, Value: 补偿源中的地址/ID）</param>
public record FallbackReadContext(
    string DriverId,
    DateTime StartTime,
    DateTime EndTime,
    IReadOnlyDictionary<string, string>? Tags = null,
    int MaxCount = 1000);
