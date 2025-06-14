using Azure.Data.Tables;

namespace az_tw_website_functions.SharedStorage;

public interface ITableStorageService
{
    TableClient GetTableClient(string tableName);
}