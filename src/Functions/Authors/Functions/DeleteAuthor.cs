using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

using Utils;
using Utils.Validation;
using Functions.Authors.Services;
using Functions.Authors.Models;
using Functions.Shared;

namespace Functions.Authors.Functions;

/// <summary>
/// Azure Function to delete an author by slug
/// </summary>
public class DeleteAuthorFunction : BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>
{
  public DeleteAuthorFunction(
      IAppInsightsLogger<BaseContentFunctions<IAuthorService, AuthorModel, AuthorDTO, AuthorWithMediaDTO>> logger,
      IAuthorService authorService,
      IAPIKeyValidator apiKeyValidator)
      : base(logger, authorService, apiKeyValidator)
  {
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, AuthorModel model)
  {
    // For delete operations, we don't validate model fields since we're only using slug
    return null;
  }

  /// <summary>
  /// Deletes an author by slug
  /// </summary>
  /// <param name="req">The HTTP request</param>
  /// <param name="slug">The author slug to delete</param>
  /// <returns>HTTP response indicating success or failure</returns>
  [Function("DeleteAuthor")]
  public async Task<HttpResponseData> Run(
      [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "authors/{slug}")] HttpRequestData req,
      string slug)
  {
    return await ProcessDeleteAsync(req, "DeleteAuthor", async (authorSlug) =>
    {
      return await _contentService.DeleteAuthorAsync(authorSlug);
    });
  }
}
