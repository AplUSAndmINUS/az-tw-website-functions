using Azure.Data.Tables;

namespace SharedStorage.Services;

public interface ITableStorageService
{
    TableClient GetTableClient(string tableName);
    Task<TableEntity?> GetEntityAsync(string tableName, string partitionKey, string rowKey);
    Task<(IEnumerable<TableEntity> Entities, string? ContinuationToken)> GetEntitiesAsync(string tableName, string? filter = null, int pageSize = 25, string? continuationToken = null);
    Task UpsertEntityAsync(string tableName, ITableEntity entity);
    Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);
}