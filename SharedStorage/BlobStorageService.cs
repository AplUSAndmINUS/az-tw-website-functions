using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Extensions.Msal;

using Utils;
namespace az_tw_website_functions.SharedStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(string storageAccountName, ILogger<BlobStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            StorageAccountValidator.ValidateStorageAccountName(storageAccountName);
            _logger.LogInformation("Creating blob storage client for {StorageAccount}", storageAccountName ?? "unknown");

            var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
            _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
            _logger.LogInformation("Blob storage client created for {Endpoint}", endpoint);
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return _blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}