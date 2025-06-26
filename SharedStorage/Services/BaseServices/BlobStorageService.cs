using Azure.Storage.Blobs;
using Azure.Identity;
using Azure;
using SharedStorage.Validators;
using Utils;
using Utils.Constants;

namespace SharedStorage.Services.BaseServices;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IAppInsightsLogger<BlobStorageService> _appLogger;

    public BlobStorageService(
        string storageAccountName,
        IAppInsightsLogger<BlobStorageService> logger)
    {
        _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(storageAccountName))
        {
            _appLogger.LogError("Storage account name is null or empty.", new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName)));
            throw new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName));
        }
        _appLogger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
        _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _appLogger.LogInformation("Blob storage client created for {Endpoint}", endpoint);
    }

    private static string ResolveContainerName(string containerName)
    {
        var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        return useMock ? $"mock-{containerName}" : containerName;
    }

    public async Task<BlobClient> GetBlobClientAsync(string containerName, string blobName)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);

        // Validate container name
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        try
        {
            _appLogger.LogInformation("Retrieving blob client for container {ContainerName} (resolved: {ResolvedContainerName}) and blob {BlobName}", containerName, resolvedContainerName, blobName);
            var response = await _blobServiceClient.GetBlobContainerClient(resolvedContainerName).GetBlobClient(blobName).ExistsAsync();

            if (!response)
            {
                throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
            }

            _appLogger.LogInformation("Blob client retrieved successfully for container {ContainerName} (resolved: {ResolvedContainerName}) and blob {BlobName}", containerName, resolvedContainerName, blobName);

            return _blobServiceClient.GetBlobContainerClient(resolvedContainerName).GetBlobClient(blobName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogError("Blob '{BlobName}' not found in container '{ContainerName}' (resolved: '{ResolvedContainerName}')", ex, blobName, containerName, resolvedContainerName);
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

        // update container to use mock if needed
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(resolvedContainerName);

        _appLogger.LogInformation("Retrieving blobs from container {ContainerName} (resolved: {ResolvedContainerName}) with prefix {Prefix}, page size {PageSize}, token {Token}", containerName, resolvedContainerName, prefix ?? "null", pageSize, continuationToken ?? "null");

        try
        {
            var blobs = new List<BlobClient>();
            await foreach (var page in containerClient.GetBlobsAsync(prefix: prefix).AsPages(continuationToken, pageSize))
            {
                blobs.AddRange(page.Values.Select(b => containerClient.GetBlobClient(b.Name)));
                continuationToken = page.ContinuationToken;
                break; // We only need the first page
            }

            _appLogger.LogInformation("Successfully retrieved {Count} blobs from container {ContainerName} (resolved: {ResolvedContainerName})", blobs.Count, containerName, resolvedContainerName);
            return new BlobPageResult(blobs, continuationToken, blobs.Count, continuationToken != null);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to retrieve blobs from container {ContainerName} (resolved: {ResolvedContainerName})", ex, containerName, resolvedContainerName);
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

        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(resolvedContainerName);
        var blobReferences = new List<BlobReference>();

        _appLogger.LogInformation("Retrieving blob references from container {ContainerName} (resolved: {ResolvedContainerName}) with prefix {Prefix}, page size {PageSize}, token {Token}", containerName, resolvedContainerName, prefix ?? "null", pageSize, continuationToken ?? "null");

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

        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(resolvedContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var exists = await blobClient.ExistsAsync();

        _appLogger.LogInformation("Retrieving blob reference for container {ContainerName} (resolved: {ResolvedContainerName}) and blob {BlobName}", containerName, resolvedContainerName, blobName);
        if (exists)
        {
            _appLogger.LogInformation("Blob reference retrieved successfully for container {ContainerName} (resolved: {ResolvedContainerName}) and blob {BlobName}", containerName, resolvedContainerName, blobName);
        }
        else
        {
            _appLogger.LogWarning("Blob {BlobName} does not exist in container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
        }

        var (section, assetType) = ParseContainerName(containerName);
        var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName);
        return new BlobReference(blobName, cdnUrl);
    }

    public async Task<BlobDownloadResult> DownloadBlobAsync(string containerName, string blobName)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var blobClient = await GetBlobClientAsync(resolvedContainerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _appLogger.LogInformation("Downloading blob {BlobName} from container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
        _appLogger.LogBlobDownload(
            containerName,
            nameof(DownloadBlobAsync),
            blobName
        );

        try
        {
            var downloadResponse = await blobClient.DownloadAsync();
            _appLogger.LogInformation("Blob {BlobName} downloaded successfully from container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
            return new BlobDownloadResult(downloadResponse.Value.Content, downloadResponse.Value.ContentLength, downloadResponse.GetRawResponse().Headers.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogError("Blob {BlobName} not found in container {ContainerName} (resolved: {ResolvedContainerName})", ex, blobName, containerName, resolvedContainerName);
            throw new ArgumentException($"Blob '{blobName}' does not exist in container '{containerName}'.", nameof(blobName));
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to download blob {BlobName} from container {ContainerName} (resolved: {ResolvedContainerName})", ex, blobName, containerName, resolvedContainerName);
            throw;
        }
    }

    public async Task<MediaReference> UploadBlobAsync(string containerName, string blobName, Stream content)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(resolvedContainerName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        await containerClient.CreateIfNotExistsAsync();
        _appLogger.LogInformation("Uploading blob {BlobName} to container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
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
            if (!content.CanRead)
            {
                _appLogger.LogError("Content stream is not readable for blob {BlobName} in container {ContainerName}", new Exception("Stream not readable"), blobName, containerName);
                throw new InvalidOperationException("Content stream must be readable.");
            }
            if (content.Length == 0)    
            {
                _appLogger.LogError("Converted content is null or empty for blob {BlobName} in container {ContainerName}", new Exception("Null value"), blobName, containerName);
                throw new InvalidOperationException("Converted content is null or empty.");
            }
            // Since blob storage is not used yet, throw a NotImplementedException to satisfy non-null return type
            throw new NotImplementedException("Blob upload is not implemented yet.");
        }

        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to upload blob {BlobName} to container {ContainerName} (resolved: {ResolvedContainerName})", ex, blobName, containerName, resolvedContainerName);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var blobClient = await GetBlobClientAsync(resolvedContainerName, blobName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _appLogger.LogInformation("Deleting blob {BlobName} from container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);

        try
        {
            await blobClient.DeleteIfExistsAsync();
            _appLogger.LogInformation("Blob {BlobName} deleted successfully from container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogWarning("Blob {BlobName} not found in container {ContainerName} (resolved: {ResolvedContainerName}), nothing to delete", blobName, containerName, resolvedContainerName);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to delete blob {BlobName} from container {ContainerName} (resolved: {ResolvedContainerName})", ex, blobName, containerName, resolvedContainerName);
            throw;
        }
    }

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Retrieving BlobContainerClient for container {ContainerName}, resolved to {ResolvedContainerName}", containerName, resolvedContainerName);
        return _blobServiceClient.GetBlobContainerClient(resolvedContainerName);
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