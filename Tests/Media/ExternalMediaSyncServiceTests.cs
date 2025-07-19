using Xunit;
using Moq;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Functions.Media.Services;
using Utils;

namespace Tests.Media;

/// <summary>
/// Unit tests for the ExternalMediaSyncService
/// </summary>
public class ExternalMediaSyncServiceTests
{
    private readonly Mock<IMediaItemService> _mockMediaItemService;
    private readonly Mock<ITikTokService> _mockTikTokService;
    private readonly Mock<IInstagramService> _mockInstagramService;
    private readonly Mock<IYouTubeService> _mockYouTubeService;
    private readonly Mock<IFacebookService> _mockFacebookService;
    private readonly Mock<ILinkedInService> _mockLinkedInService;
    private readonly Mock<IPinterestService> _mockPinterestService;
    private readonly Mock<IAppInsightsLogger<ExternalMediaSyncService>> _mockLogger;
    private readonly ExternalMediaSyncService _syncService;

    public ExternalMediaSyncServiceTests()
    {
        _mockMediaItemService = new Mock<IMediaItemService>();
        _mockTikTokService = new Mock<ITikTokService>();
        _mockInstagramService = new Mock<IInstagramService>();
        _mockYouTubeService = new Mock<IYouTubeService>();
        _mockFacebookService = new Mock<IFacebookService>();
        _mockLinkedInService = new Mock<ILinkedInService>();
        _mockPinterestService = new Mock<IPinterestService>();
        _mockLogger = new Mock<IAppInsightsLogger<ExternalMediaSyncService>>();

        _syncService = new ExternalMediaSyncService(
            _mockMediaItemService.Object,
            _mockTikTokService.Object,
            _mockInstagramService.Object,
            _mockYouTubeService.Object,
            _mockFacebookService.Object,
            _mockLinkedInService.Object,
            _mockPinterestService.Object,
            _mockLogger.Object);
    }

    [Theory]
    [InlineData("TIKTOK")]
    [InlineData("INSTAGRAM")]
    [InlineData("YOUTUBE")]
    [InlineData("FACEBOOK")]
    [InlineData("LINKEDIN")]
    [InlineData("PINTEREST")]
    public async Task SyncPlatformMediaAsync_ValidPlatforms_CallsCorrectService(string platform)
    {
        // Arrange
        var mockMediaItems = new List<MediaItemModel>
        {
            new MediaItemModel
            {
                Id = "1",
                Platform = platform,
                IsExternal = true,
                ExternalId = $"{platform.ToLower()}_1",
                MediaType = "image"
            }
        };

        SetupMockPlatformService(platform, mockMediaItems);

        // Act
        var result = await _syncService.SyncPlatformMediaAsync(platform);

        // Assert
        VerifyPlatformServiceCalled(platform);
        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains($"Starting sync for platform: {platform}")), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SyncPlatformMediaAsync_UnsupportedPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncPlatformMediaAsync("UNSUPPORTED"));
        Assert.Contains("Unsupported platform: UNSUPPORTED", exception.Message);
    }

    [Fact]
    public async Task SyncPlatformMediaAsync_NullOrEmptyPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncPlatformMediaAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncPlatformMediaAsync(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => _syncService.SyncPlatformMediaAsync("   "));
    }

    [Fact]
    public async Task SyncAllPlatformsAsync_CallsAllPlatformServices()
    {
        // Arrange
        var platforms = new[] { "TikTok", "Instagram", "YouTube", "Facebook", "LinkedIn", "Pinterest" };
        foreach (var platform in platforms)
        {
            SetupMockPlatformService(platform.ToUpperInvariant(), new List<MediaItemModel>());
        }

        // Act
        var totalSynced = await _syncService.SyncAllPlatformsAsync();

        // Assert
        foreach (var platform in platforms)
        {
            VerifyPlatformServiceCalled(platform.ToUpperInvariant());
        }

        _mockLogger.Verify(x => x.LogInformation(It.Is<string>(s => s.Contains("Completed sync for all platforms")), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task SyncAllPlatformsAsync_OnePlatformFails_ContinuesWithOthers()
    {
        // Arrange
        SetupMockPlatformService("TIKTOK", new List<MediaItemModel>());
        _mockInstagramService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ThrowsAsync(new Exception("Instagram API error"));
        SetupMockPlatformService("YOUTUBE", new List<MediaItemModel>());
        SetupMockPlatformService("FACEBOOK", new List<MediaItemModel>());
        SetupMockPlatformService("LINKEDIN", new List<MediaItemModel>());
        SetupMockPlatformService("PINTEREST", new List<MediaItemModel>());

        // Act
        var totalSynced = await _syncService.SyncAllPlatformsAsync();

        // Assert
        _mockTikTokService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
        _mockInstagramService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
        _mockYouTubeService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
        _mockFacebookService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
        _mockLinkedInService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
        _mockPinterestService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);

        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Failed to sync platform Instagram")), It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    private void SetupMockPlatformService(string platform, List<MediaItemModel> mediaItems)
    {
        switch (platform.ToUpperInvariant())
        {
            case "TIKTOK":
                _mockTikTokService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
            case "INSTAGRAM":
                _mockInstagramService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
            case "YOUTUBE":
                _mockYouTubeService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
            case "FACEBOOK":
                _mockFacebookService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
            case "LINKEDIN":
                _mockLinkedInService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
            case "PINTEREST":
                _mockPinterestService.Setup(x => x.GetLatestMediaAsync(It.IsAny<int>())).ReturnsAsync(mediaItems);
                break;
        }
    }

    private void VerifyPlatformServiceCalled(string platform)
    {
        switch (platform.ToUpperInvariant())
        {
            case "TIKTOK":
                _mockTikTokService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
            case "INSTAGRAM":
                _mockInstagramService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
            case "YOUTUBE":
                _mockYouTubeService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
            case "FACEBOOK":
                _mockFacebookService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
            case "LINKEDIN":
                _mockLinkedInService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
            case "PINTEREST":
                _mockPinterestService.Verify(x => x.GetLatestMediaAsync(It.IsAny<int>()), Times.Once);
                break;
        }
    }
}