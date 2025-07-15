using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

using Utils;
using Utils.Validation;
using Functions.Authors.Services;
using Functions.Authors.Models;
using Functions.Shared;

namespace Functions.Authors.Functions;

// Endpoint: GET /authors/{slug}
// Description: Retrieves an author by their slug. If the author exists, it returns the author's details.
// If the author does not exist, it returns a 404 Not Found error.

public class GetAuthorFunction : BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>
{
  public GetAuthorFunction(
    IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>> logger,
    IAPIKeyValidator apiKeyValidator,
    IAuthorService authorService)
    : base(logger, authorService, apiKeyValidator)
  {
    _appLogger.LogInformation("GetAuthorFunction initialized");
  }

  [Function("GetAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "authors/{slug}")] HttpRequestData req,
    string slug,
    FunctionContext executionContext)
  {
    _appLogger.LogInformation("GetAuthor function triggered for slug: {Slug}", slug);

    // Validate API key using base class method
    var apiValidationResult = await ValidateApiKeyAsync(req, "GetAuthor");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Validate the slug parameter
      if (string.IsNullOrWhiteSpace(slug))
      {
        _appLogger.LogWarning("Invalid slug provided: {Slug}", slug);
        return CreateBadRequestResponse(req, "Invalid author slug");
      }

      // Get author with or without media based on query parameter
      var (_, includeMedia) = ParseGetSingleQueryParameters(req);

      object? result = null;
      if (includeMedia)
      {
        result = await _contentService.GetAuthorWithMediaAsync(slug);
      }
      else
      {
        result = await _contentService.GetAuthorBySlugAsync(slug);
      }

      if (result == null)
      {
        _appLogger.LogWarning("Author not found for slug: {Slug}", slug);
        return CreateNotFoundResponse(req, "Author not found");
      }

      _appLogger.LogInformation("Author found for slug: {Slug}", slug);
      return await CreateJsonResponseAsync(req, result);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("An unexpected error occurred while getting author: {Message}", ex, ex.Message);
      return CreateServerErrorResponse(req, "Internal server error");
    }
  }

  /// <summary>
  /// Validate author-specific model fields (required by base class)
  /// </summary>
  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, AuthorModel model)
  {
    // Authors don't use the generic upsert pattern, so this is not used
    // But we need to implement it for the base class
    return null;
  }
}