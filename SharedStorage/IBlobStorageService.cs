using Azure.Storage.Blobs;

namespace az_tw_website_functions.SharedStorage
{
    public interface IBlobStorageService
    {
        BlobContainerClient GetBlobContainerClient(string containerName);
    }
}