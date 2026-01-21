namespace Arim.Drivers.Fallback.Core.Abstractions;

/// <summary>
/// 补偿配置缓存接口
/// </summary>
public interface IFallbackCache
{
    /// <summary>
    /// 获取缓存值
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// 设置缓存值
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// 移除缓存
    /// </summary>
    void Remove(string key);
}
