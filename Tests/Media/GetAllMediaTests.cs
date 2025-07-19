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
/// Unit tests for the GetAllMedia function
/// </summary>
public class GetAllMediaTests
{
    private readonly Mock<IMediaItemService> _mockMediaItemService;
    private readonly Mock<IAppInsightsLogger<GetAllMedia>> _mockLogger;
    private readonly GetAllMedia _getAllMediaFunction;

    public GetAllMediaTests()
    {
        _mockMediaItemService = new Mock<IMediaItemService>();
        _mockLogger = new Mock<IAppInsightsLogger<GetAllMedia>>();
        _getAllMediaFunction = new GetAllMedia(_mockMediaItemService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllMedia_ReturnsSuccessWithValidData()
    {
        // Arrange
        var mockMediaItems = new List<MediaItemModel>
        {
            new MediaItemModel
            {
                Id = "1",
                Platform = "Instagram",
                MediaType = "image",
                IsExternal = true,
                ExternalId = "instagram_1",
                ExternalUrl = "https://instagram.com/p/test1",
                Description = "Test Instagram image"
            },
            new MediaItemModel
            {
                Id = "2",
                Platform = "YouTube", 
                MediaType = "video",
                IsExternal = true,
                ExternalId = "youtube_1",
                ExternalUrl = "https://youtube.com/watch?v=test1",
                Description = "Test YouTube video"
            }
        };

        _mockMediaItemService
            .Setup(x => x.GetAllMediaAsync(It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMediaItems);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetAllMedia");

        // Act
        var response = await _getAllMediaFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetAllMediaAsync(It.IsAny<int?>(), It.IsAny<int>()), Times.Once);
        _mockLogger.Verify(x => x.LogInformation(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAllMedia_WithLimitParameter_CallsServiceWithLimit()
    {
        // Arrange
        var mockMediaItems = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetAllMediaAsync(It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMediaItems);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetAllMedia?limit=10");

        // Act
        var response = await _getAllMediaFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetAllMediaAsync(10, 0), Times.Once);
    }

    [Fact]
    public async Task GetAllMedia_WithOffsetParameter_CallsServiceWithOffset()
    {
        // Arrange
        var mockMediaItems = new List<MediaItemModel>();
        _mockMediaItemService
            .Setup(x => x.GetAllMediaAsync(It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(mockMediaItems);

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetAllMedia?offset=5");

        // Act
        var response = await _getAllMediaFunction.Run(mockRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mockMediaItemService.Verify(x => x.GetAllMediaAsync(null, 5), Times.Once);
    }

    [Fact]
    public async Task GetAllMedia_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockMediaItemService
            .Setup(x => x.GetAllMediaAsync(It.IsAny<int?>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Test exception"));

        var mockRequest = CreateMockHttpRequest("http://localhost:7071/api/GetAllMedia");

        // Act
        var response = await _getAllMediaFunction.Run(mockRequest);

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