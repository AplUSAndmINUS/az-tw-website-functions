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
    private readonly ILogger<BlobStorageService> _logger;
    private readonly IImageService _imageConversionService;
    private readonly IThumbnailService _thumbnailService;

    public BlobStorageService(
        string storageAccountName,
        ILogger<BlobStorageService> logger,
        IImageService imageConversionService,
        IThumbnailService thumbnailService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
        _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _logger.LogInformation("Blob storage client created for {Endpoint}", endpoint);

        _imageConversionService = imageConversionService ?? throw new ArgumentNullException(nameof(imageConversionService));
        _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
    }

    public async Task<BlobClient> GetBlobClientAsync(string containerName, string blobName)
    {
        // Validate container name
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        try
        {
            _logger.LogInformation("Retrieving blob client for container {ContainerName} and blob {BlobName}", containerName, blobName);
            var response = await _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName).ExistsAsync();

            if (!response)
            {
                throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
            }

            _logger.LogInformation("Blob client retrieved successfully for container {ContainerName} and blob {BlobName}", containerName, blobName);

            return _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Blob '{BlobName}' not found in container '{ContainerName}'", blobName, containerName);
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

        _logger.LogInformation("Retrieving blobs from container {ContainerName} with prefix {Prefix}, page size {PageSize}, token {Token}", containerName, prefix, pageSize, continuationToken);

        try
        {
            var blobs = new List<BlobClient>();
            await foreach (var page in containerClient.GetBlobsAsync(prefix: prefix).AsPages(continuationToken, pageSize))
            {
                blobs.AddRange(page.Values.Select(b => containerClient.GetBlobClient(b.Name)));
                continuationToken = page.ContinuationToken;
                break; // We only need the first page
            }

            _logger.LogInformation("Successfully retrieved {Count} blobs from container {ContainerName}", blobs.Count, containerName);
            return new BlobPageResult(blobs, continuationToken, blobs.Count, continuationToken != null);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to retrieve blobs from container {ContainerName}", containerName);
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
        if (!exists)
        {
            throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
        }

        var (section, assetType) = ParseContainerName(containerName);
        var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName);
        return new BlobReference(blobName, cdnUrl);
    }

    public async Task<BlobDownloadResult> DownloadBlobAsync(string containerName, string blobName)
    {
        var blobClient = await GetBlobClientAsync(containerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _logger.LogInformation("Downloading blob {BlobName} from container {ContainerName}", blobName, containerName);

        try
        {
            var downloadResponse = await blobClient.DownloadAsync();
            _logger.LogInformation("Blob {BlobName} downloaded successfully from container {ContainerName}", blobName, containerName);
            return new BlobDownloadResult(downloadResponse.Value.Content, downloadResponse.Value.ContentLength, downloadResponse.GetRawResponse().Headers.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Blob {BlobName} not found in container {ContainerName}", blobName, containerName);
            throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task<MediaReference> UploadBlobAsync(string containerName, string blobName, Stream content)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        await containerClient.CreateIfNotExistsAsync();
        _logger.LogInformation("Uploading blob {BlobName} to container {ContainerName}", blobName, containerName);

        try
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content), "Content stream cannot be null.");
            }

            // Convert and reformat the image to WebP format
            var convertedParams = await _imageConversionService.ConvertToWebPAsync(content);
            if (convertedParams == null || convertedParams.Content == null)
            {
                throw new InvalidOperationException("Converted content is null or empty.");
            }
            convertedParams.Content.Position = 0; // Reset stream position to the beginning before upload

            // Create a thumnail from the converted content
            var thumbnail = await _thumbnailService.GenerateWebPThumbnailAsync(convertedParams.Content);
            if (thumbnail == null || thumbnail.Content == null)
            {
                throw new InvalidOperationException("Thumbnail content is null or empty.");
            }
            thumbnail.Content.Position = 0; // Reset stream position to the beginning before upload

            // Upload the WebP image to the blob storage
            _logger.LogInformation("Uploading main blob {BlobName} to container {ContainerName}", blobName, containerName);
            await blobClient.UploadAsync(convertedParams.Content, overwrite: true);

            // Upload the thumbnail image to the blob storage
            var thumbnailBlobName = $"thumbnails/{Path.GetFileNameWithoutExtension(blobName)}.webp";
            _logger.LogInformation("Uploading thumbnail blob {ThumbnailBlobName} to container {ContainerName}", thumbnailBlobName, containerName);
            var thumbnailBlobClient = containerClient.GetBlobClient(thumbnailBlobName);
            await thumbnailBlobClient.UploadAsync(
                thumbnail.Content,
                new BlobHttpHeaders { ContentType = "image/webp" });
            _logger.LogInformation("Thumbnail blob {ThumbnailBlobName} uploaded successfully to container {ContainerName}", thumbnailBlobName, containerName);

            _logger.LogInformation("Blob {BlobName} uploaded successfully to container {ContainerName}", blobName, containerName);
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
            _logger.LogError(ex, "Failed to upload blob {BlobName} to container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var blobClient = await GetBlobClientAsync(containerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _logger.LogInformation("Deleting blob {BlobName} from container {ContainerName}", blobName, containerName);

        try
        {
            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Blob {BlobName} deleted successfully from container {ContainerName}", blobName, containerName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Blob {BlobName} not found in container {ContainerName}, nothing to delete", blobName, containerName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobName} from container {ContainerName}", blobName, containerName);
            throw;
        }
    }

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        return _blobServiceClient.GetBlobContainerClient(containerName);
    }

    private (ContentSections section, AssetType? assetType) ParseContainerName(string containerName)
    {
        // Special handling for hyphenated container names
        string[] parts = containerName.Split('-');

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
        throw new ArgumentException($"Unable to determine content section for container: {containerName}", nameof(containerName));
    }
}