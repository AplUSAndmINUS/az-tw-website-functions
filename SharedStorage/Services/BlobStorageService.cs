using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using Azure;
using Microsoft.Extensions.Logging;
using SharedStorage.Validators;
using Utils;
using Utils.Constants;

namespace SharedStorage.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IAppInsightsLogger<BlobStorageService> _appLogger;
    private readonly IImageService _imageConversionService;
    private readonly IThumbnailService _thumbnailService;

    public BlobStorageService(
        string storageAccountName,
        IAppInsightsLogger<BlobStorageService> logger,
        IImageService imageConversionService,
        IThumbnailService thumbnailService)
    {
        _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));

        _appLogger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
        _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _appLogger.LogInformation("Blob storage client created for {Endpoint}", endpoint);

        _imageConversionService = imageConversionService ?? throw new ArgumentNullException(nameof(imageConversionService));
        _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
    }

    public async Task<BlobClient> GetBlobClientAsync(string containerName, string blobName)
    {
        // Validate container name
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        try
        {
            _appLogger.LogInformation("Retrieving blob client for container {ContainerName} and blob {BlobName}", containerName, blobName);
            var response = await _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName).ExistsAsync();

            if (!response)
            {
                throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
            }

            _appLogger.LogInformation("Blob client retrieved successfully for container {ContainerName} and blob {BlobName}", containerName, blobName);

            return _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogError("Blob '{BlobName}' not found in container '{ContainerName}'", ex, blobName, containerName);
            throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
        }
    }

    // Used for internal operations, not exposed in the interface
    public async Task<BlobPageResult> GetBlobsAsync(
        string containerName,
        string? prefix = null,
        int pageSize = 25,
        string? continuationToken = null)
    {
        // Validate container name
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        _appLogger.LogInformation("Retrieving blobs from container {ContainerName} with prefix {Prefix}, page size {PageSize}, token {Token}", containerName, prefix ?? "null", pageSize, continuationToken ?? "null");

        try
        {
            var blobs = new List<BlobClient>();
            await foreach (var page in containerClient.GetBlobsAsync(prefix: prefix).AsPages(continuationToken, pageSize))
            {
                blobs.AddRange(page.Values.Select(b => containerClient.GetBlobClient(b.Name)));
                continuationToken = page.ContinuationToken;
                break; // We only need the first page
            }

            _appLogger.LogInformation("Successfully retrieved {Count} blobs from container {ContainerName}", blobs.Count, containerName);
            return new BlobPageResult(blobs, continuationToken, blobs.Count, continuationToken != null);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to retrieve blobs from container {ContainerName}", ex, containerName);
            throw;
        }
    }

    // Used for Public CDN blob storage ONLY in Production
    public async Task<IList<BlobReference>> GetBlobReferencesAsync(
        string containerName,
        string? prefix = null,
        int pageSize = 25,
        string? continuationToken = null)
    {
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobReferences = new List<BlobReference>();

        _appLogger.LogInformation("Retrieving blob references from container {ContainerName} with prefix {Prefix}, page size {PageSize}, token {Token}", containerName, prefix ?? "null", pageSize, continuationToken ?? "null");

        await foreach (var blob in containerClient.GetBlobsAsync(prefix: prefix))
        {
            var blobName = blob.Name;
            var (section, assetType) = ParseContainerName(containerName);
            var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName);
            blobReferences.Add(new BlobReference(blobName, cdnUrl));
        }

        return blobReferences;
    }

    public async Task<BlobReference> GetBlobReferenceAsync(string containerName, string blobName)
    {
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var exists = await blobClient.ExistsAsync();

        _appLogger.LogInformation("Retrieving blob reference for container {ContainerName} and blob {BlobName}", containerName, blobName);
        if (exists)
        {
            _appLogger.LogInformation("Blob reference retrieved successfully for container {ContainerName} and blob {BlobName}", containerName, blobName);
        }
        else
        {
            _appLogger.LogWarning("Blob {BlobName} does not exist in container {ContainerName}", blobName, containerName);
        }

        var (section, assetType) = ParseContainerName(containerName);
        var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName);
        return new BlobReference(blobName, cdnUrl);
    }

    public async Task<BlobDownloadResult> DownloadBlobAsync(string containerName, string blobName)
    {
        var blobClient = await GetBlobClientAsync(containerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _appLogger.LogInformation("Downloading blob {BlobName} from container {ContainerName}", blobName, containerName);
        _appLogger.LogBlobDownload(
            containerName,
            nameof(DownloadBlobAsync),
            blobName
        );

        try
        {
            var downloadResponse = await blobClient.DownloadAsync();
            _appLogger.LogInformation("Blob {BlobName} downloaded successfully from container {ContainerName}", blobName, containerName);
            return new BlobDownloadResult(downloadResponse.Value.Content, downloadResponse.Value.ContentLength, downloadResponse.GetRawResponse().Headers.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogError("Blob {BlobName} not found in container {ContainerName}", ex, blobName, containerName);
            throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to download blob {BlobName} from container {ContainerName}", ex, blobName, containerName);
            throw;
        }
    }

    public async Task<MediaReference> UploadBlobAsync(string containerName, string blobName, Stream content)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        await containerClient.CreateIfNotExistsAsync();
        _appLogger.LogInformation("Uploading blob {BlobName} to container {ContainerName}", blobName, containerName);
        _appLogger.LogBlobUpload(
            containerName,
            nameof(UploadBlobAsync),
            blobName,
            content.Length
        );

        try
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            if (content == null)
            {
                _appLogger.LogError("Content stream is null for blob {BlobName} in container {ContainerName}", new Exception("Null value"), blobName, containerName);
                throw new ArgumentNullException(nameof(content), "Content stream cannot be null.");
            }

            // Convert and reformat the image to WebP format
            var convertedParams = await _imageConversionService.ConvertToWebPAsync(content);
            if (convertedParams == null || convertedParams.Content == null)
            {
                _appLogger.LogError("Converted content is null or empty for blob {BlobName} in container {ContainerName}", new Exception("Null value"), blobName, containerName);
                throw new InvalidOperationException("Converted content is null or empty.");
            }
            convertedParams.Content.Position = 0; // Reset stream position to the beginning before upload

            // Create a thumnail from the converted content
            var thumbnail = await _thumbnailService.GenerateWebPThumbnailAsync(convertedParams.Content);
            if (thumbnail == null || thumbnail.Content == null)
            {
                _appLogger.LogError("Thumbnail content is null or empty for blob {BlobName} in container {ContainerName}", new Exception("Null value"), blobName, containerName);
                throw new InvalidOperationException("Thumbnail content is null or empty.");
            }
            thumbnail.Content.Position = 0; // Reset stream position to the beginning before upload

            // Upload the WebP image to the blob storage
            _appLogger.LogInformation("Uploading main blob {BlobName} to container {ContainerName}", blobName, containerName);
            await blobClient.UploadAsync(convertedParams.Content, overwrite: true);

            // Upload the thumbnail image to the blob storage
            var thumbnailBlobName = $"thumbnails/{Path.GetFileNameWithoutExtension(blobName)}.webp";
            _appLogger.LogInformation("Uploading thumbnail blob {ThumbnailBlobName} to container {ContainerName}", thumbnailBlobName, containerName);
            _appLogger.LogBlobUpload(
                containerName,
                nameof(UploadBlobAsync),
                thumbnailBlobName,
                thumbnail.Content.Length
            );
            var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailBlobName);
            await thumbnailBlobClient.UploadAsync(
                thumbnail.Content,
                new BlobHttpHeaders { ContentType = "image/webp" });
            _appLogger.LogInformation("Thumbnail blob {ThumbnailBlobName} uploaded successfully to container {ContainerName}", thumbnailBlobName, containerName);

            _appLogger.LogInformation("Blob {BlobName} uploaded successfully to container {ContainerName}", blobName, containerName);
            var (section, assetType) = ParseContainerName(containerName);

            // For upload operations, use direct Azure Blob URLs
            var mainBlobUrl = blobClient.Uri.ToString();
            var thumbnailBlobUrl = thumbnailBlobClient.Uri.ToString();

            // Return Media Reference with direct URLs
            return new MediaReference(
                blobName,
                thumbnailBlobName,
                mainBlobUrl,
                thumbnailBlobUrl
            );
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to upload blob {BlobName} to container {ContainerName}", ex, blobName, containerName);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var blobClient = await GetBlobClientAsync(containerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _appLogger.LogInformation("Deleting blob {BlobName} from container {ContainerName}", blobName, containerName);

        try
        {
            await blobClient.DeleteIfExistsAsync();
            _appLogger.LogInformation("Blob {BlobName} deleted successfully from container {ContainerName}", blobName, containerName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogWarning("Blob {BlobName} not found in container {ContainerName}, nothing to delete", blobName, containerName);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to delete blob {BlobName} from container {ContainerName}", ex, blobName, containerName);
            throw;
        }
    }

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        _appLogger.LogInformation("Retrieving BlobContainerClient for container {ContainerName}", containerName);
        return _blobServiceClient.GetBlobContainerClient(containerName);
    }

    private (ContentSections section, AssetType? assetType) ParseContainerName(string containerName)
    {
        // Special handling for hyphenated container names
        string[] parts = containerName.Split('-');

        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            _appLogger.LogInformation("Container name {ContainerName} is invalid", containerName);
            throw new ArgumentException($"Invalid container name: {containerName}", nameof(containerName));
        }
        // Try to match container name directly from ContentNameResolver first to ensure exact matching
        foreach (ContentSections contentSection in Enum.GetValues(typeof(ContentSections)))
        {
            // Try non-hyphenated first (exact section match)
            string sectionName = contentSection.ToString().ToLowerInvariant();
            if (string.Equals(containerName, sectionName, StringComparison.OrdinalIgnoreCase))
            {
                return (contentSection, null);
            }

            // Try with asset types
            foreach (AssetType type in Enum.GetValues(typeof(AssetType)))
            {
                string expectedName = ContentNameResolver.GetBlobContainerName(contentSection, type);
                if (string.Equals(containerName, expectedName, StringComparison.OrdinalIgnoreCase))
                {
                    return (contentSection, type);
                }
            }
        }

        // Handle potential hyphenated names by checking the first part
        if (parts.Length > 1)
        {
            foreach (ContentSections contentSection in Enum.GetValues(typeof(ContentSections)))
            {
                if (string.Equals(parts[0], contentSection.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    // Determine asset type from the second part
                    string assetPart = parts[1].ToLowerInvariant();

                    switch (assetPart)
                    {
                        case "images":
                            return (contentSection, AssetType.Images);
                        case "video":
                            return (contentSection, AssetType.Video);
                        case "media":
                            return (contentSection, AssetType.Media);
                        case "data":
                            return (contentSection, AssetType.Data);
                        default:
                            return (contentSection, null);
                    }
                }
            }
        }

        // If we still can't determine, throw an exception
        _appLogger.LogInformation("Unable to parse container name {ContainerName}", containerName);
        throw new ArgumentException($"Unable to determine content section for container: {containerName}", nameof(containerName));
    }
}