using System.Collections.Concurrent;
using System.Threading.Channels;
using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;
using Microsoft.Extensions.Logging;

namespace Arim.Drivers.Fallback.Core.Services;

public class FallbackService : IFallbackService
{
    private readonly ConcurrentDictionary<string, Channel<FallbackTask>> _routingTable = new();
    
    // 用于通知后台服务有新的通道被创建
    private readonly Channel<ChannelReader<FallbackTask>> _newChannelNotifier = 
        Channel.CreateUnbounded<ChannelReader<FallbackTask>>();

    private readonly ILogger<FallbackService> _logger;

    public FallbackService(ILogger<FallbackService> logger)
    {
        _logger = logger;
    }

    public ChannelReader<ChannelReader<FallbackTask>> NewChannelReader => _newChannelNotifier.Reader;

    public async ValueTask EnqueueAsync(FallbackTask task, CancellationToken ct = default)
    {
        // 1. 根据 FallbackSourceId 获取或创建隔离通道
        var channel = _routingTable.GetOrAdd(task.FallbackSourceId, sourceId =>
        {
            _logger.LogInformation("Creating new isolated channel for fallback source: {SourceId}", sourceId);
            
            var newChannel = Channel.CreateBounded<FallbackTask>(new BoundedChannelOptions(2000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true, // 每个源由一个独立协程处理
                SingleWriter = false
            });

            // 通知后台服务启动新的 Worker
            _newChannelNotifier.Writer.TryWrite(newChannel.Reader);
            
            return newChannel;
        });

        // 2. 将任务写入对应通道
        await channel.Writer.WriteAsync(task, ct);
    }
}
