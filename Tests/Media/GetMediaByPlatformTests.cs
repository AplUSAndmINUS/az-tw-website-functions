using Xunit;
using Moq;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Functions.Media.Functions;
using Utils;
using System.Net;

namespace Tests.Media;

/// <summary>
/// Unit tests for the GetMediaByPlatform function
/// </summary>
public class GetMediaByPlatformTests
{
    private readonly Mock<IMediaItemService> _mockMediaItemService;
    private readonly Mock<IAppInsightsLogger<GetMediaByPlatform>> _mockLogger;
    private readonly GetMediaByPlatform _getMediaByPlatformFunction;

    public GetMediaByPlatformTests()
    {
        _mockMediaItemService = new Mock<IMediaItemService>();
        _mockLogger = new Mock<IAppInsightsLogger<GetMediaByPlatform>>();
        _getMediaByPlatformFunction = new GetMediaByPlatform(_mockMediaItemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetMediaByPlatform_ValidTikTokPlatform_ReturnsTikTokMedia()
    {
        // Arrange
        var mockTikTokMedia = new List<MediaItemModel>
        {
            new MediaItemModel
            {
                Id = "1",
                Platform = "TikTok",
                MediaType = "video",
                IsExternal = true,
                ExternalId = "tiktok_1",
                ExternalUrl = "https://tiktok.com/@user/video/123",
                Description = "Test TikTok video"
            }
        };

        _mockMediaItemService
            .Setup(x => x.GetMediaByPlatformAsync("TikTok", It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockTikTokMedia);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByPlatform?platform=tiktok");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByPlatformAsync("TikTok", It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByPlatform_MissingPlatformParameter_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByPlatform");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByPlatformAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMediaByPlatform_InvalidPlatform_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByPlatform?platform=invalidplatform");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByPlatformAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("tiktok", "TikTok")]
    [InlineData("instagram", "Instagram")]
    [InlineData("youtube", "YouTube")]
    [InlineData("facebook", "Facebook")]
    [InlineData("linkedin", "LinkedIn")]
    [InlineData("pinterest", "Pinterest")]
    [InlineData("blobstorage", "BlobStorage")]
    [InlineData("TIKTOK", "TikTok")]
    [InlineData("INSTAGRAM", "Instagram")]
    public async Task GetMediaByPlatform_ValidPlatforms_NormalizesAndProcesses(string inputPlatform, string expectedPlatform)
    {
        // Arrange
        var mockMedia = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetMediaByPlatformAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMedia);

        var mockRequest = CreateMockHttpRequest($"http://localhost:7071/api/GetMediaByPlatform?platform={inputPlatform}");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByPlatformAsync(expectedPlatform, It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByPlatform_WithLimitAndOffset_PassesParametersCorrectly()
    {
        // Arrange
        var mockMedia = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetMediaByPlatformAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMedia);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByPlatform?platform=instagram&limit=15&offset=5");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByPlatformAsync("Instagram", 15, 5), Times.Once);
    }

    [Fact]
    public async Task GetMediaByPlatform_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockMediaItemService
            .Setup(x => x.GetMediaByPlatformAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test exception"));

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByPlatform?platform=youtube");

        // Act
        var response = await _getMediaByPlatformFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<string>()), Times.Once);
    }

    private static HttpRequestData CreateMockHttpRequest(string url)
    {
        var context = new Mock<FunctionContext>();
        var request = new Mock<HttpRequestData>(context.Object);
        
        request.Setup(r => r.Url).Returns(new Uri(url));
        request.Setup(r => r.CreateResponse(It.IsAny<HttpStatusCode>())).Returns(() =>
        {
            var response = new Mock<HttpResponseData>(context.Object);
            response.SetupAllProperties();
            return response.Object;
        });

        return request.Object;
    }
}