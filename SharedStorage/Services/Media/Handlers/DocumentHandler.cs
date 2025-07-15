using SharedStorage.Models;
using SharedStorage.Services.BaseServices;
using Utils;
using Utils.Constants;

namespace SharedStorage.Services.Media.Handlers;

public class DocumentHandler : MediaHandler, IMediaTypeHandler
{
  private readonly IBlobStorageService _blobStorageService;
  private readonly IAppInsightsLogger<DocumentHandler> _logger;
  private readonly IDocumentConversionService _documentConversionService;

  public override string SupportedType => "document";

  public DocumentHandler(
      IBlobStorageService blobStorageService,
      IDocumentConversionService documentConversionService,
      IAppInsightsLogger<DocumentHandler> logger) : base("document")
  {
    _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
    _documentConversionService = documentConversionService ?? throw new ArgumentNullException(nameof(documentConversionService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public override async Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorId = null, string? contentId = null, string? relatedContentType = null)
  {
    try
    {
      _logger.LogInformation("Starting document upload for file: {FileName}", fileName);

      if (stream == null || !stream.CanRead)
        throw new ArgumentException("Stream must be readable", nameof(stream));

      // Store original stream in memory so we can use it multiple times
      using var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);
      memoryStream.Position = 0;

      _logger.LogInformation("Copied document stream to memory: {Length} bytes", memoryStream.Length);

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
      _logger.LogInformation("USE_MOCK_STORAGE environment variable is set to: {UseMockStorage}", useMockStorage ? "true" : "false");

      // Create the container name based on content section, explicitly passing the mock storage flag
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Document, useMockStorage);

      // Generate a unique ID for this document
      var mediaId = Guid.NewGuid().ToString();

      // Check if we need to convert to PDF
      bool convertToPdf = IsConvertToPdfRequested();
      string finalFileName = fileName;
      string finalContentType = contentType;
      Stream uploadStream = memoryStream;

      // Convert to PDF if requested and file is not already a PDF
      if (convertToPdf && !IsPdf(fileName))
      {
        _logger.LogInformation("Converting document to PDF format: {FileName}", fileName);

        // Convert document to PDF
        var conversionResult = await _documentConversionService.ConvertToPdfAsync(memoryStream, fileName);

        // Update file name and content type
        finalFileName = Path.ChangeExtension(fileName, ".pdf");
        finalContentType = "application/pdf";
        uploadStream = conversionResult.Content;
        uploadStream.Position = 0;

        _logger.LogInformation("Document conversion completed: {OriginalFile} -> {PdfFile}", fileName, finalFileName);
      }
      else
      {
        _logger.LogInformation("No conversion needed. Using original format: {FileName}, {ContentType}", fileName, contentType);
      }

      // Generate blob name
      var documentBlobName = $"documents/{mediaId}/{finalFileName}";

      // Upload document to blob storage
      _logger.LogInformation("Uploading document to blob storage: {BlobName}", documentBlobName);
      var mediaReference = await _blobStorageService.UploadBlobAsync(containerName, documentBlobName, uploadStream, contentId, relatedContentType);

      // Create document entity using CDN URLs from mediaReference
      var documentEntity = new MediaEntity
      {
        Id = mediaId,
        PartitionKey = authorId ?? "system",
        RowKey = mediaId,
        AuthorId = authorId ?? "system",
        Filename = finalFileName,
        MediaType = "document",
        Url = mediaReference.CdnUrl,
        ContentType = finalContentType,
        UploadedAt = DateTime.UtcNow,
        ContentId = contentId ?? string.Empty,
        RelatedContentType = relatedContentType ?? string.Empty
      };

      _logger.LogInformation(
          "Successfully uploaded document {MediaId} with file {FileName}. Original format: {OriginalFormat}, " +
          "Final format: {FinalFormat}, Conversion: {Conversion}",
          mediaId,
          finalFileName,
          contentType,
          finalContentType,
          convertToPdf ? "PDF conversion applied" : "No conversion");

      return documentEntity;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to upload document {FileName}: {Error}", ex, fileName, ex.Message);
      throw;
    }
  }

  public override Task<MediaEntity> GetAsync(string id)
  {
    // This method is handled by the MediaService itself via table storage
    // Handlers are primarily for upload/delete operations with blob storage
    throw new NotSupportedException("Get operations are handled by MediaService directly");
  }

  public override Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
  {
    // This method is handled by the MediaService itself via table storage
    // Handlers are primarily for upload/delete operations with blob storage
    throw new NotSupportedException("Get operations are handled by MediaService directly");
  }

  public override async Task<bool> DeleteAsync(string id)
  {
    try
    {
      _logger.LogInformation("Deleting document with ID: {MediaId}", id);

      // Check if mock storage is enabled
      var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";

      // Delete document blob from storage
      var containerName = ContentNameResolver.GetBlobContainerName(ContentSections.Blog, AssetType.Document, useMockStorage);

      // Delete all blobs associated with this document
      await DeleteDocumentBlobsAsync(containerName, id);

      _logger.LogInformation("Successfully deleted document blobs for media ID: {MediaId}", id);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete document {MediaId}: {Error}", ex, id, ex.Message);
      return false;
    }
  }

  private async Task DeleteDocumentBlobsAsync(string containerName, string mediaId)
  {
    try
    {
      // Get all blobs with the media ID prefix
      var prefix = $"documents/{mediaId}/";
      var blobsResult = await _blobStorageService.GetBlobsAsync(containerName, prefix);

      // Delete each blob
      foreach (var blobClient in blobsResult.Blobs)
      {
        await _blobStorageService.DeleteBlobAsync(containerName, blobClient.Name);
        _logger.LogInformation("Deleted document blob: {BlobName}", blobClient.Name);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError("Failed to delete document blobs for media ID {MediaId}: {Error}", ex, mediaId, ex.Message);
      throw;
    }
  }

  private bool IsConvertToPdfRequested()
  {
    // Check if the convert-to-pdf option is set from request context
    // This could be stored in a thread-local variable, from query parameters, or from environment
    // For now, we'll use an environment variable as a simple example
    return System.Environment.GetEnvironmentVariable("CONVERT_DOCUMENT_TO_PDF")?.ToLowerInvariant() == "true";
  }

  private bool IsPdf(string fileName)
  {
    // Check if the file is already a PDF
    return Path.GetExtension(fileName).ToLowerInvariant() == ".pdf";
  }

  private static string GetDocumentMimeType(string fileName)
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
