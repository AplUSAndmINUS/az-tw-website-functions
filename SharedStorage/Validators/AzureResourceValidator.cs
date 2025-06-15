using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Azure;

namespace SharedStorage.Validators;

public static class AzureResourceValidator
{
  public static async Task ValidateAzureTableExistsAsync(TableServiceClient tableServiceClient, string tableName)
  {
      TableNameValidator.ValidateTableName(tableName);

      var client = tableServiceClient.GetTableClient(tableName);

      try
      {
          // Safe, lightweight check — will throw 404 if table doesn't exist
          await foreach (var _ in client.QueryAsync<TableEntity>())
          {
              break; // Only need to check if at least one entity exists
          }
      }
      catch (RequestFailedException ex) when (ex.Status == 404)
      {
          throw new ArgumentException($"Table '{tableName}' does not exist.", nameof(tableName));
      }
  }

  public static async Task ValidateAzureBlobContainerExistsAsync(BlobServiceClient blobServiceClient, string containerName)
  {
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    BlobContainerNameValidator.ValidateBlobContainerName(containerName);

    try
    {
      await containerClient.GetPropertiesAsync();
    }
    catch (RequestFailedException ex) when (ex.Status == 404)
    {
      throw new ArgumentException($"Blob container '{containerName}' does not exist.", nameof(containerName));
    }
  }
}