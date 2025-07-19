using Xunit;
using Moq;
using Microsoft.Azure.Functions.Worker;
using SharedStorage.Services.Media;
using Functions.Media.Services;
using Functions.Media.Functions;
using Utils;

namespace Tests.Media;

/// <summary>
/// Unit tests for the WriteMediaTable timer function
/// </summary>
public class WriteMediaTableTests
{
    private readonly Mock<IMediaItemService> _mockMediaItemService;
    private readonly Mock<IExternalMediaSyncService> _mockExternalMediaSyncService;
    private readonly Mock<IAppInsightsLogger<WriteMediaTable>> _mockLogger;
    private readonly WriteMediaTable _writeMediaTableFunction;

    public WriteMediaTableTests()
    {
        _mockMediaItemService = new Mock<IMediaItemService>();
        _mockExternalMediaSyncService = new Mock<IExternalMediaSyncService>();
        _mockLogger = new Mock<IAppInsightsLogger<WriteMediaTable>>();
        _writeMediaTableFunction = new WriteMediaTable(_mockMediaItemService.Object, _mockExternalMediaSyncService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task WriteMediaTable_SuccessfulSync_LogsCorrectly()
    {
        // Arrange
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("TikTok"))
            .ReturnsAsync(5);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Instagram"))
            .ReturnsAsync(3);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("YouTube"))
            .ReturnsAsync(2);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Facebook"))
            .ReturnsAsync(1);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("LinkedIn"))
            .ReturnsAsync(1);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Pinterest"))
            .ReturnsAsync(4);

        var mockTimerInfo = CreateMockTimerInfo();

        // Act
        await _writeMediaTableFunction.Run(mockTimerInfo);

        // Assert
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("TikTok"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Instagram"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("YouTube"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Facebook"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("LinkedIn"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Pinterest"), Times.Once);

        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("WriteMediaTable timer trigger function started")), It.IsAny<DateTime>()), Times.Once);
        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("WriteMediaTable completed successfully. Total synced: 16")), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task WriteMediaTable_SinglePlatformFails_ContinuesWithOtherPlatforms()
    {
        // Arrange
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("TikTok"))
            .ReturnsAsync(5);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Instagram"))
            .ThrowsAsync(new Exception("Instagram API error"));
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("YouTube"))
            .ReturnsAsync(3);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Facebook"))
            .ReturnsAsync(2);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("LinkedIn"))
            .ReturnsAsync(1);
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync("Pinterest"))
            .ReturnsAsync(1);

        var mockTimerInfo = CreateMockTimerInfo();

        // Act
        await _writeMediaTableFunction.Run(mockTimerInfo);

        // Assert - All platforms should be attempted
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("TikTok"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Instagram"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("YouTube"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Facebook"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("LinkedIn"), Times.Once);
        _mockExternalMediaSyncService.Verify(x => x.SyncPlatformMediaAsync("Pinterest"), Times.Once);

        // Should log the error for Instagram but continue
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Failed to sync media from Instagram")), It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        
        // Should still complete successfully with total from other platforms (5+3+2+1+1=12)
        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("WriteMediaTable completed successfully. Total synced: 12")), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task WriteMediaTable_CriticalError_LogsErrorAndRethrows()
    {
        // Arrange
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Critical system error"));

        var mockTimerInfo = CreateMockTimerInfo();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _writeMediaTableFunction.Run(mockTimerInfo));
        
        Assert.Equal("Critical system error", exception.Message);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Critical error in WriteMediaTable")), It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task WriteMediaTable_NoMediaSynced_LogsZeroTotal()
    {
        // Arrange
        _mockExternalMediaSyncService
            .Setup(x => x.SyncPlatformMediaAsync(It.IsAny<string>()))
            .ReturnsAsync(0);

        var mockTimerInfo = CreateMockTimerInfo();

        // Act
        await _writeMediaTableFunction.Run(mockTimerInfo);

        // Assert
        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("WriteMediaTable completed successfully. Total synced: 0")), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void WriteMediaTable_Constructor_ThrowsArgumentNullException_WhenServiceIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WriteMediaTable(null!, _mockExternalMediaSyncService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new WriteMediaTable(_mockMediaItemService.Object, null!, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new WriteMediaTable(_mockMediaItemService.Object, _mockExternalMediaSyncService.Object, null!));
    }

    private static TimerInfo CreateMockTimerInfo()
    {
        var mock = new Mock<TimerInfo>();
        mock.Setup(x => x.IsPastDue).Returns(false);
        return mock.Object;
    }
}