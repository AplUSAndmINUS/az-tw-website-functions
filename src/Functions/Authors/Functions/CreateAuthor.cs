using az_tw_website_functions.src.Functions.Authors.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

using SharedStorage.Services;
using Utils;
using Utils.Constants;

namespace az_tw_website_functions.src.Functions.Authors.Functions;

public class CreateAuthor
{
  private readonly ITableStorageService _tableStorageService;

  public CreateAuthor(ITableStorageService tableStorageService)
  {
    _tableStorageService = tableStorageService;
  }

  // Implementation for creating an author
  [Function("CreateAuthor")]
  public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authors/add")] HttpRequestData req, FunctionContext executionContext)

  {
    // 0. Initialize variables
    AuthorModel? input;
    var logger = executionContext.GetLogger("CreateAuthor");
    // var isMockStorage = executionContext.FunctionAppDirectory.Contains("mock", StringComparison.OrdinalIgnoreCase);
    var tableName = ContentNameResolver.GetTableName(ContentSections.Authors, null, true);
    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

    try
    {
      // 1. Deserialize incoming JSON request body to AuthorModel
      input = JsonSerializer.Deserialize<AuthorModel>(requestBody, new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
      }) ?? throw new ArgumentException("Deserialization returned null.");

      // 2. Clean + validate Username and prepare keys
      var partitionKey = input.Username.ToLowerInvariant().Replace(" ", "-");
      if (string.IsNullOrWhiteSpace(partitionKey)) throw new ArgumentException("Username cannot be empty or whitespace.");
      if (partitionKey.Length > 100) partitionKey = partitionKey[..100]; // Ensure it fits within Azure Table Storage limits
      var rowKey = "profile"; // Optional, could be "profile" or "metadata"
      input.AuthorSlug = partitionKey; // Ensure the model has the correct slug
      logger.LogInformation($"PartitionKey: {partitionKey}");
      logger.LogInformation($"Request body: {requestBody}");

      // 3. Convert to AuthorEntity
      var entity = AuthorEntity.FromModel(input, partitionKey, rowKey);

      // 4. Persist to Table Storage
      await _tableStorageService.UpsertEntityAsync(tableName, entity);

      // 5. Map the clean, persisted entity into the DTO (image metadata blank for now)
      var dto = AuthorModelMapper.Map(entity, null);

      // 6. Return enriched result
      var response = req.CreateResponse(HttpStatusCode.Created);
      await response.WriteAsJsonAsync(dto);
      logger.LogInformation($"Author {dto.FullName} ({dto.AuthorSlug}) created successfully.");
      return response;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Invalid author data provided.");
      var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
      await badResponse.WriteStringAsync("Invalid author payload.");
      return badResponse;
    }
  }
}