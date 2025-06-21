using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Utils;

namespace Functions.Authors.Functions;

public class CreateAuthor
{
  // This function is a placeholder for creating an author.
  // The actual implementation will depend on your specific requirements.
  // You can use this function to handle HTTP requests to create a new author.
  private readonly ILogger<CreateAuthor> _appLogger;

  // Constructor to inject the logger
  public CreateAuthor(ILogger<CreateAuthor> logger)
  {
    _appLogger = logger;
  }

  [Function("CreateAuthor")]
  public Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequestData req,
    FunctionContext executionContext)

  {
    // Log the function execution context
    // You can use this logger to log information, warnings, errors, etc.
    _appLogger.LogInformation("Creating a new author.");

    // Request processing logic goes here
    // For example, you might read the request body, validate input,
    // and create a new author entity in your data store.

    // For now, we will return a simple response indicating success. (No logic has happened yet)
    var response = req.CreateResponse(HttpStatusCode.Created);
    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
    response.WriteString("{\"message\":\"Author created successfully.\"}");
    return Task.FromResult(response);
  }
}

// Example:
// [Function("CreateAuthor")]
// public async Task<HttpResponseData> Run(
//   [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors")] HttpRequestData req,
//   FunctionContext executionContext)
// {
//   var logger = executionContext.GetLogger("CreateAuthor");
//   logger.LogInformation("Creating a new author.");
//
//   // Your logic to create an author goes here
//
//   return req.CreateResponse(HttpStatusCode.Created);
// }