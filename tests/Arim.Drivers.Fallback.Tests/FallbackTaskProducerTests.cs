using Arim.Drivers.Fallback.Core.Abstractions;
using Arim.Drivers.Fallback.Core.Models;
using Arim.Drivers.Fallback.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arim.Drivers.Fallback.Tests;

public class FallbackTaskProducerTests
{
    [Fact]
    public async Task CreateTaskAsync_LargeTimeRange_ShouldSplitIntoMultipleTasks()
    {
        // Arrange
        var fallbackServiceMock = new Mock<IFallbackService>();
        var sourceResolverMock = new Mock<IFallbackSourceResolver>();
        var loggerMock = new Mock<ILogger<FallbackTaskProducer>>();
        
        sourceResolverMock.Setup(x => x.ResolveSourceIdAsync("D1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("S1");
        
        var service = new FallbackTaskProducer(
            fallbackServiceMock.Object, 
            sourceResolverMock.Object, 
            loggerMock.Object);

        var startTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var endTime = startTime.AddHours(2.5); // 2.5 hours > 1 hour limit

        // Act
        await service.CreateTaskAsync("D1", startTime, endTime);

        // Assert
        // Should split into: 10:00-11:00, 11:00-12:00, 12:00-12:30 (3 tasks)
        fallbackServiceMock.Verify(x => x.EnqueueAsync(It.IsAny<FallbackTask>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateTaskAsync_WithMappedAddress_ShouldIncludeAddressInTask()
    {
        // Arrange
        var fallbackServiceMock = new Mock<IFallbackService>();
        var sourceResolverMock = new Mock<IFallbackSourceResolver>();
        var loggerMock = new Mock<ILogger<FallbackTaskProducer>>();
        
        var tags = new List<string> { "Tag1" };
        var mapping = new Dictionary<string, List<DriverTag>>
        {
            { "SourceA", new List<DriverTag> { new DriverTag("Tag1", "PLC_ADDR_001") } }
        };

        sourceResolverMock.Setup(x => x.GroupTagsBySourceAsync("D1", tags, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);
        
        var service = new FallbackTaskProducer(
            fallbackServiceMock.Object, 
            sourceResolverMock.Object, 
            loggerMock.Object);

        // Act
        await service.CreateTaskAsync("D1", DateTime.Now, DateTime.Now.AddMinutes(10), tags);

        // Assert
        fallbackServiceMock.Verify(x => x.EnqueueAsync(
            It.Is<FallbackTask>(t => t.Tags != null && t.Tags["Tag1"] == "PLC_ADDR_001"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
