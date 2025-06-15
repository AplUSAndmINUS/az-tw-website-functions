using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Utils;

namespace az_tw_website_functions.SharedStorage;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<TableStorageService> _logger;

    public TableStorageService(string storageAccountName, ILogger<TableStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate storage account name
        StorageAccountValidator.ValidateStorageAccountName(storageAccountName);
        _logger.LogInformation("Creating table client for {Table}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.table.core.windows.net";
        _tableServiceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _logger.LogInformation("Table client created for {Endpoint}", endpoint);
    }

    public TableClient GetTableClient(string tableName)
    {
        return _tableServiceClient.GetTableClient(tableName);
    }
}