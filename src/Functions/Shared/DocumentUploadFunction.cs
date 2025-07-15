using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SharedStorage.Models;
using SharedStorage.Services.Media;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Constants;
using Utils.Validation;

namespace Functions.Shared;

public class DocumentUploadFunction
{
  private readonly IMediaService _mediaService;
  private readonly IAppInsightsLogger<DocumentUploadFunction> _logger;
  private readonly IAPIKeyValidator _apiKeyValidator;

  public DocumentUploadFunction(
      IMediaService mediaService,
      IAppInsightsLogger<DocumentUploadFunction> logger,
      IAPIKeyValidator apiKeyValidator)
  {
    _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
  }

  [Function("UploadDocument")]
  public async Task<HttpResponseData> UploadDocumentAsync(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "media/documents")] HttpRequestData req)
  {
    _logger.LogInformation("Document upload request received");

    // Validate API key using helper method
    var apiValidationResult = await _apiKeyValidator.ValidateApiKeyAsync(req, _logger, "UploadDocument");
    if (apiValidationResult != null)
    {
      return apiValidationResult;
    }

    try
    {
      // Check if request has a body
      if (req.Body == null)
      {
        _logger.LogWarning("No file found in request");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("No file found in request");
        return badResponse;
      }

      // Get parameters from query string
      var fileName = req.Query["fileName"] ?? "uploaded-document.pdf";
      var authorId = req.Query["authorId"];
      var contentId = req.Query["contentId"];
      var relatedContentType = req.Query["relatedContentType"];

      // Check if conversion to PDF is requested (from query params)
      var convertToPdf = req.Query["convertToPdf"]?.ToLowerInvariant() == "true";

      // Set environment variable for PDF conversion flag
      Environment.SetEnvironmentVariable("CONVERT_DOCUMENT_TO_PDF", convertToPdf.ToString().ToLowerInvariant());

      _logger.LogInformation("Processing document upload: {FileName}, Convert to PDF: {ConvertToPdf}",
          fileName, convertToPdf);

      // Copy the request body to a memory stream to ensure it's seekable
      using var memoryStream = new MemoryStream();
      await req.Body.CopyToAsync(memoryStream);
      memoryStream.Position = 0;

      // Validate that we have actual content
      if (memoryStream.Length == 0)
      {
        _logger.LogWarning("Document file is empty");
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Document file is required and cannot be empty");
        return badResponse;
      }

      // Determine content type based on file extension
      var contentType = GetDocumentMimeType(fileName);

      // Upload document through MediaService which will use the DocumentHandler
      var mediaEntity = await _mediaService.UploadMediaAsync(
          "document",
          memoryStream,
          fileName,
          authorId,
          null, // description
          null, // altText
          "document", // purpose
          contentId,
          relatedContentType);

      // Clear environment variable after use
      Environment.SetEnvironmentVariable("CONVERT_DOCUMENT_TO_PDF", null);

      // Return success response with the media entity
      var response = req.CreateResponse(HttpStatusCode.Created);
      response.Headers.Add("Content-Type", "application/json");

      var responseBody = JsonSerializer.Serialize(mediaEntity, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });

      await response.WriteStringAsync(responseBody);

      _logger.LogInformation("Successfully uploaded document with ID: {MediaId}", mediaEntity.Id);
      return response;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to upload document: {Error}", ex, ex.Message);

      // Clear environment variable in case of error
      Environment.SetEnvironmentVariable("CONVERT_DOCUMENT_TO_PDF", null);

      var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
      await errorResponse.WriteStringAsync($"Failed to upload document: {ex.Message}");
      return errorResponse;
    }
  }

  private string GetDocumentMimeType(string fileName)
  {
    // Get MIME type based on file extension
    return Path.GetExtension(fileName).ToLowerInvariant() switch
    {
      ".pdf" => "application/pdf",
      ".doc" => "application/msword",
      ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
      ".xls" => "application/vnd.ms-excel",
      ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      ".ppt" => "application/vnd.ms-powerpoint",
      ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
      ".txt" => "text/plain",
      ".csv" => "text/csv",
      ".rtf" => "application/rtf",
      _ => "application/octet-stream" // Default
    };
  }
}
