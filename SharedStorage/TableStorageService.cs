using Azure.Data.Tables;
using Azure.Identity;
using System;

namespace az_tw_website_functions.SharedStorage;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;

    public TableStorageService(string storageAccountName)
    {
        var endpoint = $"https://{storageAccountName}.table.core.windows.net";
        _tableServiceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
    }

    public TableClient GetTableClient(string tableName)
    {
        return _tableServiceClient.GetTableClient(tableName);
    }
}