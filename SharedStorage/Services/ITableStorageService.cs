using Azure.Data.Tables;

namespace SharedStorage.Services;

public interface ITableStorageService
{
    TableClient GetTableClient(string tableName);
    Task<TableEntity?> GetEntityAsync(string tableName, string partitionKey, string rowKey);
    Task<IEnumerable<TableEntity>> GetEntitiesAsync(string tableName, string? filter = null);
    Task UpsertEntityAsync(string tableName, ITableEntity entity);
    Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);
}