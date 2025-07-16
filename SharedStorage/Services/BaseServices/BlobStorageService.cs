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

        // Check for user-assigned managed identity client ID
        var clientId = System.Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        if (!string.IsNullOrEmpty(clientId))
        {
            _appLogger.LogInformation("Using user-assigned managed identity with client ID: {ClientId}", clientId);
            var options = new DefaultAzureCredentialOptions { ManagedIdentityClientId = clientId };
            _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential(options));
        }
        else
        {
            _appLogger.LogInformation("Using default credentials (system-assigned managed identity or local credentials)");
            _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        }

        _appLogger.LogInformation("Blob storage client created for {Endpoint}", endpoint);
    }

    private static string ResolveContainerName(string containerName)
    {
        var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        string resolvedName;

        if (useMock)
        {
            // Only add the mock prefix if it's not already there
            if (!containerName.StartsWith("mock-", StringComparison.OrdinalIgnoreCase))
            {
                resolvedName = $"mock-{containerName}";
            }
            else
            {
                resolvedName = containerName;
            }
        }
        else
        {
            // If we're not using mock storage, remove the mock- prefix if it exists
            if (containerName.StartsWith("mock-", StringComparison.OrdinalIgnoreCase))
            {
                resolvedName = containerName.Substring(5); // Remove "mock-" prefix

                // Handle case where containerName might just be "mock-" (unlikely but possible)
                if (string.IsNullOrEmpty(resolvedName))
                {
                    resolvedName = "container"; // Default to "container" if the entire name was "mock-"
                }
            }
            else
            {
                resolvedName = containerName;
            }
        }

        Console.WriteLine($"DEBUG: ResolveContainerName - Original={containerName}, USE_MOCK_STORAGE={useMock}, Resolved={resolvedName}");
        return resolvedName;
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

            // Check if mock storage is enabled for CDN URL generation
            var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
            var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName, null, useMockStorage);

            // Get blob client to access metadata
            var blobClient = containerClient.GetBlobClient(blobName);
            var blobProperties = await blobClient.GetPropertiesAsync();

            // Look for metadata that might contain ContentId and RelatedContentType
            string? contentId = null;
            string? relatedContentType = null;

            if (blobProperties.Value.Metadata.TryGetValue("ContentId", out var contentIdValue))
            {
                contentId = contentIdValue;
            }

            if (blobProperties.Value.Metadata.TryGetValue("RelatedContentType", out var relatedContentTypeValue))
            {
                relatedContentType = relatedContentTypeValue;
            }

            blobReferences.Add(new BlobReference(blobName, cdnUrl, contentId, relatedContentType));
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

        // Check if mock storage is enabled for CDN URL generation
        var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName, null, useMockStorage);

        _appLogger.LogInformation("Generated CDN URL with USE_MOCK_STORAGE={UseMock}: {CdnUrl}",
            useMockStorage ? "true" : "false", cdnUrl);

        // Look for metadata that might contain ContentId and RelatedContentType
        var blobProperties = await blobClient.GetPropertiesAsync();
        string? contentId = null;
        string? relatedContentType = null;

        if (blobProperties.Value.Metadata.TryGetValue("ContentId", out var contentIdValue))
        {
            contentId = contentIdValue;
        }

        if (blobProperties.Value.Metadata.TryGetValue("RelatedContentType", out var relatedContentTypeValue))
        {
            relatedContentType = relatedContentTypeValue;
        }

        return new BlobReference(blobName, cdnUrl, contentId, relatedContentType);
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

    public async Task<MediaReference> UploadBlobAsync(string containerName, string blobName, Stream content, string? contentId = null, string? relatedContentType = null)
    {
        var resolvedContainerName = ResolveContainerName(containerName);
        _appLogger.LogInformation("Resolving container name to {ResolvedContainerName}", resolvedContainerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(resolvedContainerName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);
        await containerClient.CreateIfNotExistsAsync();
        _appLogger.LogInformation("Uploading blob {BlobName} to container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);
        if (!string.IsNullOrEmpty(contentId))
        {
            _appLogger.LogInformation("Uploading blob with content relationship: ContentId={ContentId}, RelatedContentType={RelatedContentType}", contentId, relatedContentType ?? "unknown");
        }
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

            // Reset stream position to ensure we read from the beginning
            if (content.CanSeek)
            {
                content.Position = 0;
            }

            // Upload the blob
            var uploadResponse = await blobClient.UploadAsync(content, overwrite: true);

            // Add metadata if ContentId is provided
            if (!string.IsNullOrEmpty(contentId))
            {
                var metadata = new Dictionary<string, string>();
                metadata["ContentId"] = contentId;

                if (!string.IsNullOrEmpty(relatedContentType))
                {
                    metadata["RelatedContentType"] = relatedContentType;
                }

                // Set metadata on the blob
                await blobClient.SetMetadataAsync(metadata);
                _appLogger.LogInformation("Added content relationship metadata to blob {BlobName}: ContentId={ContentId}, RelatedContentType={RelatedContentType}",
                    blobName, contentId, relatedContentType ?? "unknown");
            }

            _appLogger.LogInformation("Successfully uploaded blob {BlobName} to container {ContainerName} (resolved: {ResolvedContainerName})", blobName, containerName, resolvedContainerName);

            // Check if mock storage is enabled for CDN URL generation
            var useMockStorage = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";

            // Get the CDN URL for the uploaded blob
            var (section, assetType) = ParseContainerName(containerName);
            var cdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, blobName, null, useMockStorage);

            _appLogger.LogInformation("Generated CDN URL with USE_MOCK_STORAGE={UseMock}: {CdnUrl}",
                useMockStorage ? "true" : "false", cdnUrl);

            // Create thumbnail blob name (simplified approach - assumes thumbnail will be uploaded separately)
            var thumbnailBlobName = blobName.Contains("/thumb_") ? blobName : $"thumb_{blobName}";
            var thumbnailCdnUrl = CdnUrlBuilder.ResolveCdnUrl(section, assetType, thumbnailBlobName, null, useMockStorage);

            return new MediaReference(blobName, thumbnailBlobName, cdnUrl, thumbnailCdnUrl, contentId, relatedContentType);
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

        // Get mock storage setting
        bool useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        _appLogger.LogInformation("ParseContainerName processing container: {ContainerName}, USE_MOCK_STORAGE={UseMock}",
            containerName, useMock ? "true" : "false");

        // Try to match container name directly from ContentNameResolver first to ensure exact matching
        foreach (ContentSections contentSection in Enum.GetValues(typeof(ContentSections)))
        {
            // Try non-hyphenated first (exact section match with current mock setting)
            string sectionName = ContentNameResolver.GetBlobContainerName(contentSection, null, useMock);
            if (string.Equals(containerName, sectionName, StringComparison.OrdinalIgnoreCase))
            {
                _appLogger.LogInformation("Matched section {Section} with no asset type", contentSection);
                return (contentSection, null);
            }

            // Also try with opposite mock setting for robustness
            string sectionNameAltMock = ContentNameResolver.GetBlobContainerName(contentSection, null, !useMock);
            if (string.Equals(containerName, sectionNameAltMock, StringComparison.OrdinalIgnoreCase))
            {
                _appLogger.LogInformation("Matched section {Section} with no asset type (alternate mock setting)", contentSection);
                return (contentSection, null);
            }

            // Try with asset types (both with and without mock storage)
            foreach (AssetType type in Enum.GetValues(typeof(AssetType)))
            {
                // Skip Comments as it's not valid for blob containers (only for tables)
                if (type == AssetType.Comments)
                {
                    continue;
                }

                try
                {
                    // Check with current mock setting
                    string expectedName = ContentNameResolver.GetBlobContainerName(contentSection, type, useMock);
                    if (string.Equals(containerName, expectedName, StringComparison.OrdinalIgnoreCase))
                    {
                        _appLogger.LogInformation("Matched section {Section} with asset type {AssetType}",
                            contentSection, type);
                        return (contentSection, type);
                    }

                    // Also try with opposite mock setting for robustness
                    string expectedNameAltMock = ContentNameResolver.GetBlobContainerName(contentSection, type, !useMock);
                    if (string.Equals(containerName, expectedNameAltMock, StringComparison.OrdinalIgnoreCase))
                    {
                        _appLogger.LogInformation("Matched section {Section} with asset type {AssetType} (alternate mock setting)",
                            contentSection, type);
                        return (contentSection, type);
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _appLogger.LogWarning("Skipping invalid asset type {AssetType} for section {Section}: {Error}",
                        type, contentSection, ex.Message);
                    // Continue with the next asset type
                }
            }
        }

        // If no direct match found, fallback to legacy parsing
        _appLogger.LogInformation("No direct match found, attempting legacy parsing for {ContainerName}", containerName);

        // Handle mock prefix if present
        int startIndex = 0;
        if (parts[0].Equals("mock", StringComparison.OrdinalIgnoreCase) && parts.Length > 1)
        {
            // Skip the "mock-" prefix
            startIndex = 1;
            _appLogger.LogInformation("Detected mock prefix, adjusting parsing");
        }

        // Handle potential hyphenated names by checking after the mock prefix if present
        if (parts.Length > startIndex)
        {
            // Try to parse section from the part after any mock prefix
            if (Enum.TryParse<ContentSections>(parts[startIndex], true, out var section))
            {
                _appLogger.LogInformation("Parsed section {Section} from container part: {Part}",
                    section, parts[startIndex]);

                // Check if there's a second content part that could be an AssetType
                if (parts.Length > startIndex + 1)
                {
                    string assetPart = parts[startIndex + 1].ToLowerInvariant();

                    AssetType? assetType = assetPart switch
                    {
                        "images" => AssetType.Images,
                        "video" => AssetType.Video,
                        "media" => AssetType.Media,
                        "data" => AssetType.Data,
                        _ => null
                    };

                    if (assetType.HasValue)
                    {
                        _appLogger.LogInformation("Legacy parse matched section {Section} with asset type {AssetType}",
                            section, assetType);
                        return (section, assetType);
                    }
                }

                // No asset type found, just return the section
                _appLogger.LogInformation("Legacy parse matched section {Section} with no asset type", section);
                return (section, null);
            }
        }

        // If we still can't determine, throw an exception
        _appLogger.LogWarning("Unable to parse container name {ContainerName}", containerName);
        throw new ArgumentException($"Unable to determine content section for container: {containerName}", nameof(containerName));
    }
}