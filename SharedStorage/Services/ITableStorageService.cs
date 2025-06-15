using Azure.Data.Tables;

namespace SharedStorage.Services;

public record TablePageResult(
    IEnumerable<TableEntity> Entities,
    string? ContinuationToken,
    int TotalCount,
    bool HasMore
);

public interface ITableStorageService
{
    TableClient GetTableClient(string tableName);
    Task<TableEntity?> GetEntityAsync(string tableName, string partitionKey, string rowKey);
    Task<TablePageResult> GetEntitiesAsync(string tableName, string? filter = null, int pageSize = 25, string? continuationToken = null);
    Task UpsertEntityAsync(string tableName, ITableEntity entity);
    Task DeleteEntityAsync(string tableName, string partitionKey, string rowKey);
}