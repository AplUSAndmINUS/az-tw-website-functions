using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

using Functions.Authors.Models;
using Functions.Authors.Validators;
using Functions.Authors.Helpers;
using Functions.Authors.Services;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.Authors.Functions;

// Endpoint: PUT /authors/{slug}
// Description: Upserts an author by slug. If the author exists, it updates the properties.
// If the author does not exist, it creates a new author with the provided data.

public class UpsertAuthorFunction : BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>
{
  public UpsertAuthorFunction(
    IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>> logger,
    IAPIKeyValidator apiKeyValidator,
    IAuthorService authorService)
    : base(logger, authorService, apiKeyValidator)
  {
    _appLogger.LogInformation("UpsertAuthorFunction initialized.");
  }

  [Function("UpsertAuthorAsync")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "put", Route = "authors/{slug}")] HttpRequestData req,
    string slug,
    FunctionContext executionContext)
  {
    _appLogger.LogInformation("UpsertAuthorAsync function triggered for slug: {Slug}", slug);

    // Validate API key using base class method
    var apiValidationResult = await ValidateApiKeyAsync(req, "UpsertAuthorAsync");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Read and deserialize the request body
      var (model, errorResponse) = await ReadAndDeserializeBodyAsync<AuthorModel>(req);
      if (errorResponse != null)
      {
        return errorResponse;
      }

      // Set the slug from route if available
      if (!string.IsNullOrWhiteSpace(slug))
      {
        model!.AuthorSlug = slug;
      }

      // Validate the model using the base class method
      var validationResult = ValidateContentModel(req, model!);
      if (validationResult != null)
      {
        return validationResult;
      }

      // Generate slug if needed
      if (string.IsNullOrEmpty(model!.AuthorSlug))
      {
        model.AuthorSlug = slug ??
        SlugGenerator.FromName(model.FirstName, model.LastName)
        ?? SlugGenerator.FromString(model.Username)
        ?? SlugGenerator.FromString(model.DisplayName)
        ?? SlugGenerator.FromAnonymous();
        _appLogger.LogInformation($"Generated slug for author: {model.AuthorSlug}");
      }
      else if (!string.Equals(model.AuthorSlug, slug, StringComparison.OrdinalIgnoreCase))
      {
        _appLogger.LogWarning($"Slug mismatch: provided slug '{slug}' does not match model slug '{model.AuthorSlug}'.");
        return CreateBadRequestResponse(req, "Slug mismatch.");
      }

      // Create/Update the author using the service
      var result = await _contentService.UpsertAuthorAsync(model);

      _appLogger.LogInformation("Author upserted successfully with slug: {AuthorSlug}", result.AuthorSlug);
      return await CreateJsonResponseAsync(req, result);
    }
    catch (Exception ex)
    {
      _appLogger.LogError("Failed to upsert author: {ErrorMessage}", ex, ex.Message);
      return CreateServerErrorResponse(req, "Internal server error");
    }
  }

  /// <summary>
  /// Validate author-specific model fields
  /// </summary>
  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, AuthorModel model)
  {
    // Validate the model using the AuthorModelDataValidator
    _appLogger.LogInformation("Validating author model data.");
    if (!AuthorModelDataValidator.TryValidate(model, out var errors))
    {
      _appLogger.LogError("Author model validation failed.", new Exception(string.Join(" | ", errors)));
      return CreateValidationErrorResponse(req, errors);
    }

    return null;
  }

  /// <summary>
  /// Create validation error response with proper formatting
  /// </summary>
  private HttpResponseData CreateValidationErrorResponse(HttpRequestData req, IEnumerable<string> errors)
  {
    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
    errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

    var errorObject = new { errors = errors.ToArray() };
    errorResponse.WriteString(JsonHelper.Serialize(errorObject));
    return errorResponse;
  }
}