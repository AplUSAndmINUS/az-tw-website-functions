using Azure.Data.Tables;
using Azure.Identity;
using Azure;
using Microsoft.Extensions.Logging;
using SharedStorage.Validators;

namespace SharedStorage.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly ILogger<TableStorageService> _logger;
    
    public TableStorageService(string storageAccountName, ILogger<TableStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate storage account name
        _logger.LogInformation("Creating table client for {Table}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.table.core.windows.net";
        _tableServiceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _logger.LogInformation("Table client created for {Endpoint}", endpoint);
    }

    public async Task<TableEntity?> GetEntityAsync(string tableName, string partitionKey, string rowKey)
    {
        var client = _tableServiceClient.GetTableClient(tableName);

        // Validate table name
        TableNameValidator.ValidateTableName(tableName);

        try
        {
            _logger.LogInformation("Retrieving entity from table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
            var response = await client.GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey);

            _logger.LogInformation("Entity retrieved successfully from table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Entity not found in table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
            return null;
        }
    }

    public async Task<IEnumerable<TableEntity>> GetEntitiesAsync(string tableName, string? filter = null)
    {
        var client = _tableServiceClient.GetTableClient(tableName);
        _logger.LogInformation("Retrieving entities from table {TableName} with filter {Filter}", tableName, filter);

        // Validate table name
        TableNameValidator.ValidateTableName(tableName);

        try
        {
            var entities = new List<TableEntity>();
            await foreach (var entity in client.QueryAsync<TableEntity>(filter))
            {
                entities.Add(entity);
            }

            _logger.LogInformation("Successfully retrieved entities from table {TableName}", tableName);
            return entities;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to check existence of table {TableName}", tableName);
            throw;
        }
    }

    public TableClient GetTableClient(string tableName)
    {
        return _tableServiceClient.GetTableClient(tableName);
    }
}