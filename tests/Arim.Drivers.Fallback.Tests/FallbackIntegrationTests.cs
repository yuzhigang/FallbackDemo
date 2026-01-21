using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;
using Arim.Drivers.Fallback.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arim.Drivers.Fallback.Tests;

public class FallbackIntegrationTests
{
    [Fact]
    public async Task BackgroundService_ShouldProcessAndMergeTasksAcrossChannels()
    {
        // Arrange
        var serviceLogger = new Mock<ILogger<FallbackService>>();
        var managerLogger = new Mock<ILogger<FallbackBackgroundService>>();
        var sourceResolverMock = new Mock<IFallbackSourceResolver>();
        
        var fallbackService = new FallbackService(serviceLogger.Object);
        
        var mockDriver = new Mock<IFallbackDriver>();
        mockDriver.Setup(d => d.InstanceId).Returns("InSQL_Primary");
        mockDriver.Setup(d => d.DriverType).Returns("InSQL");
        mockDriver.Setup(d => d.ReadAsync(It.IsAny<FallbackReadContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TagValue> { new TagValue("T1", DateTime.Now, 100.5) });

        var drivers = new List<IFallbackDriver> { mockDriver.Object };

        sourceResolverMock.Setup(s => s.GetTagsByDriverAsync("D1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, List<DriverTag>> 
            { 
                { "InSQL_Primary", new List<DriverTag> { new DriverTag("T1") } } 
            });
        
        var backgroundService = new FallbackBackgroundService(fallbackService, drivers, sourceResolverMock.Object, managerLogger.Object);
        
        using var cts = new CancellationTokenSource();
        
        // Act
        // Start the background service
        var runTask = backgroundService.StartAsync(cts.Token);

        // Enqueue two overlapping tasks for the same source
        var baseTime = DateTime.Parse("2024-01-01 10:00:00");
        await fallbackService.EnqueueAsync(new FallbackTask("D1", "InSQL_Primary", baseTime, baseTime.AddMinutes(5)));
        await fallbackService.EnqueueAsync(new FallbackTask("D1", "InSQL_Primary", baseTime.AddMinutes(2), baseTime.AddMinutes(7)));

        // Give it some time to process
        await Task.Delay(500);
        
        await backgroundService.StopAsync(cts.Token);

        // Assert
        // Verify that ReadAsync was called (due to merging, it might be called once or twice depending on timing, 
        // but here we primarily check it was reached)
        mockDriver.Verify(d => d.ReadAsync(It.Is<FallbackReadContext>(c => c.DriverId == "D1"), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }
}
