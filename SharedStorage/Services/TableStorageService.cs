using Azure.Data.Tables;
using Azure.Identity;
using Azure;
using Microsoft.Extensions.Logging;
using SharedStorage.Validators;
using Utils;

namespace SharedStorage.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IAppInsightsLogger<TableStorageService> _appLogger;

    public TableStorageService(string storageAccountName, IAppInsightsLogger<TableStorageService> logger)
    {
        _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));

        _appLogger.LogInformation("Creating table client for {StorageAccount}", storageAccountName ?? "unknown");

        var endpoint = $"https://{storageAccountName}.table.core.windows.net";
        _tableServiceClient = new TableServiceClient(new Uri(endpoint), new DefaultAzureCredential());
        _appLogger.LogInformation("Table client created for {Endpoint}", endpoint);
    }

    private static string ResolveTableName(string tableName)
    {
        var useMock = System.Environment.GetEnvironmentVariable("USE_MOCK_STORAGE")?.ToLowerInvariant() == "true";
        return useMock ? $"mock{tableName}" : tableName;
    }

    public async Task<TableEntity?> GetEntityAsync(string tableName, string partitionKey, string rowKey)
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Resolving table name to {ResolvedTableName}", resolvedTableName);

        TableNameValidator.ValidateTableName(resolvedTableName);
        var client = _tableServiceClient.GetTableClient(resolvedTableName);

        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey), "PartitionKey cannot be null or empty.");
        }

        _appLogger.LogTableQuery(
            tableName,
            nameof(GetEntityAsync),
            filter: null,
            pageSize: 1,
            continuationToken: null
        );

        try
        {
            _appLogger.LogInformation("Retrieving entity from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
            var response = await client.GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey);

            _appLogger.LogInformation("Entity retrieved successfully from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogWarning("Entity not found in table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
            return null;
        }
    }

    public async Task<T?> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Resolving table name to {ResolvedTableName}", resolvedTableName);

        TableNameValidator.ValidateTableName(resolvedTableName);
        var client = _tableServiceClient.GetTableClient(resolvedTableName);

        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey), "PartitionKey cannot be null or empty.");
        }

        _appLogger.LogTableQuery(
            tableName,
            nameof(GetEntityAsync),
            filter: null,
            pageSize: 1,
            continuationToken: null
        );

        try
        {
            _appLogger.LogInformation("Retrieving entity of type {EntityType} from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", typeof(T).Name, tableName, resolvedTableName, partitionKey, rowKey);
            var response = await client.GetEntityIfExistsAsync<T>(partitionKey, rowKey);

            if (response.HasValue)
            {
                _appLogger.LogInformation("Entity of type {EntityType} retrieved successfully from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", typeof(T).Name, tableName, resolvedTableName, partitionKey, rowKey);
                return response.Value;
            }
            else
            {
                _appLogger.LogWarning("Entity of type {EntityType} not found in table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", typeof(T).Name, tableName, resolvedTableName, partitionKey, rowKey);
                return null;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogWarning("Entity of type {EntityType} not found in table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", typeof(T).Name, tableName, resolvedTableName, partitionKey, rowKey);
            return null;
        }
    }

    public async Task<TablePageResult> GetEntitiesAsync(
        string tableName,
        string? filter = null,
        int pageSize = 25,
        string? continuationToken = null)
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Resolving table name to {ResolvedTableName}", resolvedTableName);

        TableNameValidator.ValidateTableName(resolvedTableName);
        var client = _tableServiceClient.GetTableClient(resolvedTableName);

        _appLogger.LogInformation("Retrieving entities from table {TableName} (resolved: {ResolvedTableName}) with filter {Filter} and page size {PageSize} token {Token}", tableName, resolvedTableName, filter ?? "null", pageSize, continuationToken ?? "null");

        _appLogger.LogTableQuery(
            tableName,
            nameof(GetEntitiesAsync),
            filter,
            pageSize,
            continuationToken
        );

        try
        {
            await foreach (var page in client.QueryAsync<TableEntity>(filter).AsPages(continuationToken, pageSize))
            {
                _appLogger.LogInformation("Successfully retrieved {Count} entities from table {TableName} (resolved: {ResolvedTableName})", page.Values.Count, tableName, resolvedTableName);
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
            _appLogger.LogError("Failed to retrieve entities from table {TableName} (resolved: {ResolvedTableName})", ex, tableName, resolvedTableName);
            throw;
        }
    }

    public async Task UpsertEntityAsync(string tableName, ITableEntity entity)
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Resolving table name to {ResolvedTableName}", resolvedTableName);

        var client = _tableServiceClient.GetTableClient(resolvedTableName);

        // Validate table name
        TableNameValidator.ValidateTableName(resolvedTableName);

        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity), "Entity cannot be null.");
        }

        _appLogger.LogTableEntryUpsert(
            tableName,
            nameof(UpsertEntityAsync),
            entity.PartitionKey,
            entity.RowKey
        );

        try
        {
            _appLogger.LogInformation("Upserting entity into table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, entity.PartitionKey, entity.RowKey);
            await client.UpsertEntityAsync(entity);
            _appLogger.LogInformation("Entity upserted successfully into table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, entity.PartitionKey, entity.RowKey);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to upsert entity into table {TableName} (resolved: {ResolvedTableName})", ex, tableName, resolvedTableName);
            throw;
        }
    }

    public async Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey)
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Resolving table name to {ResolvedTableName}", resolvedTableName);

        var client = _tableServiceClient.GetTableClient(resolvedTableName);

        // Validate table name
        TableNameValidator.ValidateTableName(resolvedTableName);

        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            throw new ArgumentNullException(nameof(partitionKey), "PartitionKey cannot be null or empty.");
        }

        _appLogger.LogTableEntryDelete(
            tableName,
            nameof(DeleteEntityAsync),
            partitionKey,
            rowKey
        );

        try
        {
            _appLogger.LogInformation("Deleting entity from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
            await client.DeleteEntityAsync(partitionKey, rowKey);
            _appLogger.LogInformation("Entity deleted successfully from table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _appLogger.LogWarning("Entity not found in table {TableName} (resolved: {ResolvedTableName}) with PartitionKey {PartitionKey} and RowKey {RowKey}", tableName, resolvedTableName, partitionKey, rowKey);
        }
        catch (RequestFailedException ex)
        {
            _appLogger.LogError("Failed to delete entity from table {TableName} (resolved: {ResolvedTableName})", ex, tableName, resolvedTableName);
            throw;
        }
    }

    public TableClient GetTableClient(string tableName)
    {
        var resolvedTableName = ResolveTableName(tableName);
        _appLogger.LogInformation("Getting TableClient for table {TableName} (resolved: {ResolvedTableName})", tableName, resolvedTableName);
        return _tableServiceClient.GetTableClient(resolvedTableName);
    }
}