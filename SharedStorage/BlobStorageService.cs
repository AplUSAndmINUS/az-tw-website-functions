using Azure.Storage.Blobs;
using Azure.Identity;

namespace az_tw_website_functions.SharedStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(string storageAccountName)
        {
            if (string.IsNullOrEmpty(storageAccountName))
            {
                throw new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName));
            }
            if (storageAccountName.Length < 3 || storageAccountName.Length > 24)
            {
                throw new ArgumentException("Storage account name must be between 3 and 24 characters long.", nameof(storageAccountName));
            }
            var endpoint = $"https://{storageAccountName}.blob.core.windows.net";
            _blobServiceClient = new BlobServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        }

        public BlobContainerClient GetBlobContainerClient(string containerName)
        {
            return _blobServiceClient.GetBlobContainerClient(containerName);
        }
    }
}