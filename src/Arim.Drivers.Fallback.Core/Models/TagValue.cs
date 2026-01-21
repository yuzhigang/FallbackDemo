namespace Arim.Drivers.Fallback.Core.Models;

/// <summary>
/// 历史标签值
/// </summary>
public record TagValue(
    string TagName,
    DateTime Timestamp,
    object Value,
    string? Quality = "Good");
