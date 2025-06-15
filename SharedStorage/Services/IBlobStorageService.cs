using Azure.Storage.Blobs;

namespace SharedStorage.Services;
public interface IBlobStorageService
{
    BlobContainerClient GetBlobContainerClient(string containerName);
}