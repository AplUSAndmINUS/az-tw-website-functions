using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Books.Services;
using Functions.Books.Models;
using Functions.Shared;
using System.Net;
using Utils;
using Utils.Validation;
using Utils.Extensions;

namespace Functions.Books.Functions;

/// <summary>
/// Azure Function for creating and updating books (POST/PUT operations)
/// </summary>
public class UpsertBook : BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>
{
  private readonly IBookService _bookService;

  public UpsertBook(
    IAppInsightsLogger<BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>> logger,
    IBookService bookService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, bookService, apiKeyValidator)
  {
    _bookService = bookService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BookModel model)
  {
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(model.Title))
      errors.Add("Title is required");

    if (string.IsNullOrWhiteSpace(model.Slug))
      errors.Add("Slug is required");

    if (string.IsNullOrWhiteSpace(model.AuthorSlug))
      errors.Add("Author slug is required");

    if (string.IsNullOrWhiteSpace(model.Content))
      errors.Add("Content is required");

    if (string.IsNullOrWhiteSpace(model.Category))
      errors.Add("Category is required");

    if (model.TagsList == null)
      errors.Add("Tags list is required (can be empty array)");

    // Validate media IDs if provided
    if (!string.IsNullOrEmpty(model.FeaturedImageId))
    {
      if (!IsValidGuid(model.FeaturedImageId))
        errors.Add("FeaturedImageId must be a valid GUID");
    }

    if (!string.IsNullOrEmpty(model.FeaturedVideoId))
    {
      if (!IsValidGuid(model.FeaturedVideoId))
        errors.Add("FeaturedVideoId must be a valid GUID");
    }

    if (!string.IsNullOrEmpty(model.FeaturedMediaId))
    {
      if (!IsValidGuid(model.FeaturedMediaId))
        errors.Add("FeaturedMediaId must be a valid GUID");
    }

    if (errors.Any())
    {
      var errorMessage = string.Join(", ", errors);
      _appLogger.LogWarning("Book validation failed: {Errors}", errorMessage);
      return CreateBadRequestResponse(req, errorMessage);
    }

    return null;
  }

  [Function("UpsertBook")]
  public async Task<HttpResponseData> UpsertBookAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", "put", Route = "books/{slug}")] HttpRequestData req)
  {
    _appLogger.LogInformation("UpsertBook function triggered");

    // Validate API key using base class helper method
    var apiValidationResult = await ValidateApiKeyAsync(req, "UpsertBook");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Extract and validate slug
      var slug = ExtractSlugFromRoute(req);
      var slugValidationResult = ValidateSlug(req, slug);
      if (slugValidationResult != null)
      {
        return slugValidationResult;
      }

      // Parse and validate request body
      var (model, errorResponse) = await ReadAndDeserializeBodyAsync<BookModel>(req);
      if (errorResponse != null)
      {
        return errorResponse;
      }

      _appLogger.LogInformation("Deserialized book: Title={Title}, Slug={Slug}", model!.Title ?? "(no title)", model.Slug ?? "(no slug)");

      // Ensure the model slug matches the route slug
      model.Slug = slug!;

      // Set default values if not provided
      if (string.IsNullOrEmpty(model.Id))
      {
        model.Id = Guid.NewGuid().ToString();
      }

      // Ensure all DateTime fields are properly set to UTC
      EnsureDateTimeFieldsAreUtc(model);

      // Validate model fields using base class
      var validationResult = ValidateContentModel(req, model);
      if (validationResult != null)
      {
        return validationResult;
      }

      // Upsert the book
      var result = await _bookService.UpsertBookAsync(slug!, model);
      if (result == null)
      {
        _appLogger.LogError("Failed to upsert book", new Exception("UpsertBookAsync returned null"));
        return CreateServerErrorResponse(req, "Failed to save book");
      }

      // Create response
      var response = await CreateJsonResponseAsync(req, result);
      response.StatusCode = req.Method.ToUpper() == "POST" ? HttpStatusCode.Created : HttpStatusCode.OK;

      _appLogger.LogInformation("Successfully upserted book with slug: {Slug}", slug ?? "(no slug)");
      return response;
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Error upserting book", ex);
      return CreateServerErrorResponse(req);
    }
  }

  private static bool IsValidGuid(string guidString)
  {
    return Guid.TryParse(guidString, out _);
  }

  private static void EnsureDateTimeFieldsAreUtc(BookModel book)
  {
    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Input PublishDate={book.PublishDate} (Kind={book.PublishDate.Kind})");
    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Input LastModified={book.LastModified} (Kind={book.LastModified.Kind})");

    // LastModified should always be current UTC time
    book.LastModified = DateTime.UtcNow;

    // Set PublishDate based on status
    if (book.Status == "Published")
    {
      // For published books, ensure we have a valid date
      if (book.PublishDate == default || book.PublishDate.Year < 2000)
      {
        book.PublishDate = DateTime.UtcNow;
      }
      else
      {
        book.PublishDate = book.PublishDate.EnsureUtc();
      }
    }
    else
    {
      // For drafts, ensure we have a valid future date to avoid Azure Table Storage errors
      if (book.PublishDate == default || book.PublishDate.Year < 2000)
      {
        // Set to a valid date in the future - Azure Table Storage doesn't accept DateTime.MinValue
        book.PublishDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      }
      else
      {
        book.PublishDate = book.PublishDate.EnsureUtc();
      }
    }

    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Final PublishDate={book.PublishDate} (Kind={book.PublishDate.Kind})");
    Console.WriteLine($"DEBUG: EnsureDateTimeFieldsAreUtc - Final LastModified={book.LastModified} (Kind={book.LastModified.Kind})");
  }
}