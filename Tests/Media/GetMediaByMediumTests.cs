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
/// Unit tests for the GetMediaByMedium function
/// </summary>
public class GetMediaByMediumTests
{
    private readonly Mock<IMediaItemService> _mockMediaItemService;
    private readonly Mock<IAppInsightsLogger<GetMediaByMedium>> _mockLogger;
    private readonly GetMediaByMedium _getMediaByMediumFunction;

    public GetMediaByMediumTests()
    {
        _mockMediaItemService = new Mock<IMediaItemService>();
        _mockLogger = new Mock<IAppInsightsLogger<GetMediaByMedium>>();
        _getMediaByMediumFunction = new GetMediaByMedium(_mockMediaItemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetMediaByMedium_ValidImageMedium_ReturnsImageMedia()
    {
        // Arrange
        var mockImageMedia = new List<MediaItemModel>
        {
            new MediaItemModel
            {
                Id = "1",
                Platform = "Instagram",
                MediaType = "image",
                IsExternal = true,
                ExternalId = "instagram_img_1",
                Description = "Test Instagram image"
            }
        };

        _mockMediaItemService
            .Setup(x => x.GetMediaByMediumAsync("image", It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockImageMedia);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByMedium?medium=image");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync("image", It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByMedium_ValidVideoMedium_ReturnsVideoMedia()
    {
        // Arrange
        var mockVideoMedia = new List<MediaItemModel>
        {
            new MediaItemModel
            {
                Id = "2",
                Platform = "YouTube",
                MediaType = "video",
                IsExternal = true,
                ExternalId = "youtube_vid_1",
                Description = "Test YouTube video"
            }
        };

        _mockMediaItemService
            .Setup(x => x.GetMediaByMediumAsync("video", It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockVideoMedia);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByMedium?medium=video");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync("video", It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByMedium_MissingMediumParameter_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByMedium");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMediaByMedium_InvalidMediumType_ReturnsBadRequest()
    {
        // Arrange
        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByMedium?medium=invalid");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("image")]
    [InlineData("video")]
    [InlineData("audio")]
    [InlineData("IMAGE")]
    [InlineData("VIDEO")]
    [InlineData("AUDIO")]
    public async Task GetMediaByMedium_ValidMediumTypes_ProcessesSuccessfully(string mediumType)
    {
        // Arrange
        var mockMedia = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetMediaByMediumAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMedia);

        var mockRequest = CreateMockHttpRequest($"http://localhost:7071/api/GetMediaByMedium?medium={mediumType}");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync(mediumType.ToLowerInvariant(), It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetMediaByMedium_WithLimitAndOffset_PassesParametersCorrectly()
    {
        // Arrange
        var mockMedia = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetMediaByMediumAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMedia);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetMediaByMedium?medium=image&limit=20&offset=10");

        // Act
        var response = await _getMediaByMediumFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetMediaByMediumAsync("image", 20, 10), Times.Once);
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