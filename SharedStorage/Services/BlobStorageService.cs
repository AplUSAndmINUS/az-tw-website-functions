using Azure.Storage.Blobs;
using Azure.Identity;
using Azure;
using Microsoft.Extensions.Logging;
using SharedStorage.Validators;
using System.Reflection.Metadata;

namespace SharedStorage.Services;
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(string storageAccountName, ILogger<BlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
        _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _logger.LogInformation("Blob storage client created for {Endpoint}", endpoint);
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

    public async Task UploadBlobAsync(string containerName, string blobName, Stream content)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await AzureResourceValidator.ValidateAzureBlobContainerExistsAsync(_blobServiceClient, containerName);

        _logger.LogInformation("Uploading blob {BlobName} to container {ContainerName}", blobName, containerName);

        try
        {
            await containerClient.CreateIfNotExistsAsync();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true);
            _logger.LogInformation("Blob {BlobName} uploaded successfully to container {ContainerName}", blobName, containerName);
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
}