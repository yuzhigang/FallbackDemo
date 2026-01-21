namespace Arim.Drivers.Fallback.Core.Models;

/// <summary>
/// 驱动标签定义，包含补偿映射信息
/// </summary>
/// <param name="TagId">原始驱动中的标签 ID</param>
/// <param name="Address">在补偿驱动中对应的地址/ID（如果不一致则使用此字段）</param>
public record DriverTag(string TagId, string? Address = null)
{
    /// <summary>
    /// 获取在补偿源中实际使用的 ID
    /// </summary>
    public string FallbackAddress => Address ?? TagId;
}
