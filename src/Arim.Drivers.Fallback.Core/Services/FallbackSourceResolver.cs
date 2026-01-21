using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;

namespace Arim.Drivers.Fallback.Core.Services;

/// <summary>
/// 补偿源解析器的默认实现，支持仓储加载和缓存机制
/// </summary>
public class FallbackSourceResolver(
    IFallbackRepository repository,
    IFallbackCache cache) : IFallbackSourceResolver
{
    private const string SourceIdCachePrefix = "fb_source_id_";
    private const string TagMappingsCachePrefix = "fb_tag_mappings_";
    private const string AllTagsCachePrefix = "fb_all_tags_";

    public async Task<string?> ResolveSourceIdAsync(string driverId, CancellationToken ct = default)
    {
        var cacheKey = $"{SourceIdCachePrefix}{driverId}";
        var cached = cache.Get<string>(cacheKey);
        if (cached != null) return cached;

        var sourceId = await repository.GetDefaultSourceIdAsync(driverId, ct);
        if (sourceId != null)
        {
            cache.Set(cacheKey, sourceId, TimeSpan.FromMinutes(10));
        }

        return sourceId;
    }

    public async Task<IDictionary<string, List<DriverTag>>> GroupTagsBySourceAsync(string driverId, IEnumerable<string> tagIds, CancellationToken ct = default)
    {
        // 简单起见，这里不对标签列表进行复杂的缓存键生成，直接查仓储
        // 如果需要更高性能，可以考虑逐个标签缓存或在仓储层处理
        var mappings = await repository.GetTagMappingsAsync(driverId, tagIds, ct);
        
        return mappings
            .GroupBy(m => m.SourceId)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(m => m.Tag).ToList()
            );
    }

    public async Task<IDictionary<string, List<DriverTag>>> GetTagsByDriverAsync(string driverId, CancellationToken ct = default)
    {
        var cacheKey = $"{AllTagsCachePrefix}{driverId}";
        var cached = cache.Get<IDictionary<string, List<DriverTag>>>(cacheKey);
        if (cached != null) return cached;

        var mappings = await repository.GetAllTagMappingsAsync(driverId, ct);
        var result = mappings
            .GroupBy(m => m.SourceId)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(m => m.Tag).ToList()
            );

        if (result.Count > 0)
        {
            cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        }

        return result;
    }
}
