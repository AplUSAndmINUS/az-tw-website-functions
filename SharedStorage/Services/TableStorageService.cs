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

    public async Task<TablePageResult>GetEntitiesAsync(
        string tableName,
        string? filter = null,
        int pageSize = 25,
        string? continuationToken = null)
    {
        TableNameValidator.ValidateTableName(tableName);
        var client = _tableServiceClient.GetTableClient(tableName);

        _logger.LogInformation("Retrieving entities from table {TableName} with filter {Filter} and page size {PageSize} token {Token}", tableName, filter, pageSize, continuationToken);

        try
        {
            await foreach (var page in client.QueryAsync<TableEntity>(filter).AsPages(continuationToken, pageSize))
            {
                _logger.LogInformation("Successfully retrieved {Count} entities from table {TableName}", page.Values.Count, tableName);
                return new TablePageResult(
                    Entities: page.Values,
                    ContinuationToken: page.ContinuationToken,
                    TotalCount: page.Values.Count,
                    HasMore: page.ContinuationToken != null
                );
            }

            return new TablePageResult(
                Entities: Enumerable.Empty<TableEntity>(),
                ContinuationToken: null,
                TotalCount: 0,
                HasMore: false
            );
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to retrieve entities from table {TableName}", tableName);
            throw;
        }
    }

    public async Task UpsertEntityAsync(string tableName, ITableEntity entity)
    {
        var client = _tableServiceClient.GetTableClient(tableName);

        // Validate table name
        TableNameValidator.ValidateTableName(tableName);

        try
        {
            _logger.LogInformation("Upserting entity into table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, entity.PartitionKey, entity.RowKey);
            await client.UpsertEntityAsync(entity);
            _logger.LogInformation("Entity upserted successfully into table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, entity.PartitionKey, entity.RowKey);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upsert entity into table {TableName}", tableName);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
    {
        var client = _tableServiceClient.GetTableClient(tableName);

        // Validate table name
        TableNameValidator.ValidateTableName(tableName);

        try
        {
            _logger.LogInformation("Deleting entity from table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
            await client.DeleteEntityAsync(partitionKey, rowKey);
            _logger.LogInformation("Entity deleted successfully from table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Entity not found in table {TableName} with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, partitionKey, rowKey);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete entity from table {TableName}", tableName);
            throw;
        }
    }

    public TableClient GetTableClient(string tableName)
    {
        return _tableServiceClient.GetTableClient(tableName);
    }
}