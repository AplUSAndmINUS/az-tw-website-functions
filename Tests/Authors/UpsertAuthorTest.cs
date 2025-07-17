using Functions.Authors.Functions;
using Functions.Authors.Models;
using Functions.Authors.Services;
using Tests.Helpers;
using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Moq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Tests.Authors;

public class UpsertAuthorTests
{
  private HttpResponseData CreateMockResponse(FunctionContext context, HttpStatusCode statusCode)
  {
    var response = new Mock<HttpResponseData>(context);
    response.SetupProperty(r => r.StatusCode, statusCode);
    var headers = new HttpHeadersCollection();
    response.Setup(r => r.Headers).Returns(headers);
    return response.Object;
  }

  [Fact]
  public async Task UpsertAuthorAsync_ValidRequest_ReturnsSuccessResponse()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>>>();
    var mockApiKeyValidator = new Mock<IAPIKeyValidator>();
    var mockAuthorService = new Mock<IAuthorService>();

    // Author model setup
    var authorModel = new AuthorModel 
    { 
      FirstName = "Test", 
      LastName = "Author", 
      Email = "test.author@email.com", 
      Username = "testAuthor123" 
    };
    
    var createdAuthor = new AuthorDTO 
    {
      AuthorSlug = "test-author", 
      FirstName = "Test", 
      LastName = "Author", 
      Username = "testAuthor123", 
      DisplayName = "Test Author", 
      Location = "Test Location", 
      Bio = "Test Bio", 
      Website = "https://testauthor.com", 
      TwitterHandle = "@testauthor", 
      InstagramHandle = "@testauthor", 
      LinkedInHandle = "https://linkedin.com/in/testauthor", 
      BlueskyHandle = "@testauthor.bsky.social", 
      HasValidProfileImage = true, 
      ProfileImageFileName = "test-author.jpg", 
      ProfileImageCdnUrl = "https://cdn.example.com/test-author.jpg", 
      ThumbnailCdnUrl = "https://cdn.example.com/test-author-thumbnail.jpg" 
    };

    // Mock validation and service methods
    mockApiKeyValidator
      .Setup(v => v.ValidateOrThrowAsync(It.IsAny<HttpRequestData>()))
      .Returns(Task.CompletedTask);

    mockAuthorService
      .Setup(s => s.UpsertAsync(It.IsAny<AuthorModel>()))
      .ReturnsAsync(createdAuthor);

    var function = new UpsertAuthorFunction(
      mockLogger.Object,
      mockApiKeyValidator.Object,
      mockAuthorService.Object
    );

    // Create test request
    var context = TestFactory.CreateFunctionContext();
    var request = TestFactory.CreateJsonRequestWithApiKey(
      context,
      authorModel,
      "***REMOVED***",
      "PUT",
      $"authors/{authorModel.Username}"
    );

    // Act
    var response = await function.Run(request, authorModel.Username, context);

    // Assert
    Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    
    // Verify service was called
    mockAuthorService.Verify(s => s.UpsertAsync(It.IsAny<AuthorModel>()), Times.Once);
    mockApiKeyValidator.Verify(v => v.ValidateOrThrowAsync(It.IsAny<HttpRequestData>()), Times.Once);
  }

  [Fact]
  public async Task UpsertAuthorAsync_InvalidJson_ReturnsBadRequest()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>>>();
    var mockApiKeyValidator = new Mock<IAPIKeyValidator>();
    var mockAuthorService = new Mock<IAuthorService>();

    mockApiKeyValidator
      .Setup(v => v.ValidateOrThrowAsync(It.IsAny<HttpRequestData>()))
      .Returns(Task.CompletedTask);

    var function = new UpsertAuthorFunction(
      mockLogger.Object,
      mockApiKeyValidator.Object,
      mockAuthorService.Object
    );

    // Create request with invalid JSON
    var context = TestFactory.CreateFunctionContext();
    var request = TestFactory.CreateHttpRequestData(
      context,
      "PUT",
      "authors/testuser",
      "{ invalid json }",
      new Dictionary<string, string> 
      { 
        { "Content-Type", "application/json" },
        { "x-functions-key", "***REMOVED***" }
      }
    );

    // Act
    var response = await function.Run(request, "testuser", context);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpsertAuthorAsync_NullModel_ReturnsBadRequest()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>>>();
    var mockApiKeyValidator = new Mock<IAPIKeyValidator>();
    var mockAuthorService = new Mock<IAuthorService>();

    mockApiKeyValidator
      .Setup(v => v.ValidateOrThrowAsync(It.IsAny<HttpRequestData>()))
      .Returns(Task.CompletedTask);

    var function = new CreateAuthor(
      mockLogger.Object,
      mockTableStorageService.Object,
      mockApiKeyValidator.Object,
      mockAuthorService.Object
    );

    // Create request with null/empty body
    var context = TestFactory.CreateFunctionContext();
    var request = TestFactory.CreateHttpRequestData(
      context,
      "POST",
      "authors",
      "",
      new Dictionary<string, string> 
      { 
        { "Content-Type", "application/json" },
        { "x-functions-key", "***REMOVED***" }
      }
    );

    // Act
    var response = await function.Run(request, context);

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }
}