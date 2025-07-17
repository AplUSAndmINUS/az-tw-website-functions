using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Books.Services;
using Functions.Books.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;

namespace Functions.Books.Functions;

/// <summary>
/// Azure Function for retrieving books (GET operations)
/// </summary>
public class GetBooksFunction : BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>
{
  private readonly IBookService _bookService;

  public GetBooksFunction(
    IAppInsightsLogger<BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>> logger,
    IBookService bookService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, bookService, apiKeyValidator)
  {
    _bookService = bookService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BookModel model)
  {
    if (string.IsNullOrWhiteSpace(model.Slug))
    {
      _appLogger.LogWarning("Book model slug is required");
      return CreateBadRequestResponse(req, "Slug is required");
    }

    if (string.IsNullOrWhiteSpace(model.Title))
    {
      _appLogger.LogWarning("Book model title is required");
      return CreateBadRequestResponse(req, "Title is required");
    }

    if (string.IsNullOrWhiteSpace(model.Content))
    {
      _appLogger.LogWarning("Book model content is required");
      return CreateBadRequestResponse(req, "Content is required");
    }

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
    {
      _appLogger.LogWarning("Book model author slug is required");
      return CreateBadRequestResponse(req, "Author slug is required");
    }

    return null;
  }

  [Function("GetBooks")]
  public async Task<HttpResponseData> GetBooks([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "books")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBooks function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetBooks");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Parse query parameters using base class method
      var (authorSlug, category, isPublished, limit, includeMedia) = ParseGetQueryParameters(req);

      // Get books with or without media
      object result;
      if (includeMedia)
      {
        result = await _bookService.GetBooksWithMediaAsync(authorSlug, category, isPublished, limit);
      }
      else
      {
        result = await _bookService.GetBooksAsync(authorSlug, category, isPublished, limit);
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved books");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving books", ex);
      return CreateServerErrorResponse(req);
    }
  }

  [Function("GetBook")]
  public async Task<HttpResponseData> GetBook([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "books/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("GetBook function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetBook");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract and validate slug using base class methods
      var slug = ExtractSlugFromRoute(req);
      var slugValidationResult = ValidateSlug(req, slug);
      if (slugValidationResult != null)
      {
        return slugValidationResult;
      }

      // Parse query parameters using base class method
      var (isPublished, includeMedia) = ParseGetSingleQueryParameters(req);

      // Get the book with or without media
      object? result = null;
      if (includeMedia)
      {
        result = await _bookService.GetBookWithMediaAsync(slug!, isPublished);
      }
      else
      {
        result = await _bookService.GetBookAsync(slug!, isPublished);
      }

      if (result == null)
      {
        _appLogger.LogInformation("Book with slug {Slug} not found", slug ?? "unknown");
        return CreateNotFoundResponse(req, "Book not found");
      }

      // Create response using base class method
      var response = await CreateJsonResponseAsync(req, result);

      _appLogger.LogInformation("Successfully retrieved book with slug: {Slug}", slug ?? "unknown");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error retrieving book", ex);
      return CreateServerErrorResponse(req);
    }
  }
}