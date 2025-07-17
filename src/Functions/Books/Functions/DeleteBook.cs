using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.Books.Services;
using Functions.Books.Models;
using Functions.Shared;
using Utils;
using Utils.Validation;

namespace Functions.Books.Functions;

/// <summary>
/// Azure Function for deleting books (DELETE operations)
/// </summary>
public class DeleteBook : BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>
{
  private readonly IBookService _bookService;

  public DeleteBook(
    IAppInsightsLogger<BaseContentFunctions<IBookService, BookModel, BookDTO, BookWithMediaDTO>> logger,
    IBookService bookService,
    IAPIKeyValidator apiKeyValidator)
    : base(logger, bookService, apiKeyValidator)
  {
    _bookService = bookService;
  }

  protected override HttpResponseData? ValidateContentModelFields(HttpRequestData req, BookModel model)
  {
    // Not used for delete operations, but required by base class
    return null;
  }

  [Function("DeleteBook")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "books/{slug}")] HttpRequestData req)
  {
    return await ProcessDeleteAsync(req, "DeleteBook", _bookService.DeleteBookAsync);
  }
}