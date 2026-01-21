using Arim.Drivers.Fallback.Core.Models;
using Arim.Drivers.Fallback.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arim.Drivers.Fallback.Tests;

public class FallbackServiceTests
{
    [Fact]
    public async Task EnqueueAsync_DifferentSources_ShouldCreateMultipleChannels()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FallbackService>>();
        var service = new FallbackService(loggerMock.Object);
        var baseTime = DateTime.Now;

        // Act
        await service.EnqueueAsync(new FallbackTask("D1", "Source_A", baseTime, baseTime.AddMinutes(1)));
        await service.EnqueueAsync(new FallbackTask("D2", "Source_B", baseTime, baseTime.AddMinutes(1)));

        // Assert
        var channelReaders = new List<object>();
        while (service.NewChannelReader.TryRead(out var reader))
        {
            channelReaders.Add(reader);
        }

        Assert.Equal(2, channelReaders.Count);
    }
}
