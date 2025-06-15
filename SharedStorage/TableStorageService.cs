using Azure.Data.Tables;
using Azure.Identity;
using System;

namespace az_tw_website_functions.SharedStorage;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;

    public TableStorageService(string storageAccountName)
    {
        if (string.IsNullOrEmpty(storageAccountName))
        {
            throw new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName));
        }
        if (storageAccountName.Length < 3 || storageAccountName.Length > 24)
        {
            throw new ArgumentException("Storage account name must be between 3 and 24 characters long.", nameof(storageAccountName));
        }
        var endpoint = $"https://{storageAccountName}.table.core.windows.net";
        _tableServiceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
    }

    public TableClient GetTableClient(string tableName)
    {
        return _tableServiceClient.GetTableClient(tableName);
    }
}