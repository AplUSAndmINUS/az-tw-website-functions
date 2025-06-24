using Functions.Authors.Functions;
using Functions.Authors.Models;
using Functions.Authors.Services;
using Functions.Authors.Validators;
using Tests.Helpers;

using SharedStorage.Services;
using Utils;
using Utils.Validation;
using Moq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Tests.Authors;

public class CreateAuthorTests
{
  [Fact]
  public async Task CreateAuthorAsync_ValidRequest_ReturnsCreatedResponse()
  {
    // Arrange
    var mockLogger = new Mock<IAppInsightsLogger<CreateAuthor>>();
    var mockTableStorageService = new Mock<ITableStorageService>();
    var mockApiKeyValidator = new Mock<IAPIKeyValidator>();
    var mockAuthorService = new Mock<IAuthorService>();

    // Author model and DTO setup
    var authorModel = new AuthorModel { FirstName = "Test", LastName = "Author", Email = "Test.Author@email.com", Username = "testAuthor123" };
    var createdAuthor = new AuthorDTO {
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
      .Setup(s => s.CreateAuthorAsync(It.IsAny<AuthorModel>()))
      .ReturnsAsync(createdAuthor);

    var function = new CreateAuthor(
      mockLogger.Object,
      mockTableStorageService.Object,
      mockApiKeyValidator.Object,
      mockAuthorService.Object
    );

    // Prepare the mock HttpRequestData
    var json = JsonSerializer.Serialize(authorModel);
    var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

    var context = TestFactory.CreateFunctionContext();
    var request = TestFactory.CreateHttpRequestData(
      context,
      "POST",
      "authors",
      stream,
      "application/json"
    );

    // Act
    var response = await function.Run(request, context);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Contains(
      "/authors/test-author", 
      response.Headers.GetValues("Location").FirstOrDefault() 
      ?? string.Empty
    );
  }
}