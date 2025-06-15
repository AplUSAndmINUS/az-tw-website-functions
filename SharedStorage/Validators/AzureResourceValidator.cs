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
          var enumerator = client.QueryAsync<TableEntity>().GetAsyncEnumerator();
          if (!await enumerator.MoveNextAsync())
          {
              // Table exists but no rows—still valid!
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