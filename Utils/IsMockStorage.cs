namespace Utils;

public static class IsMockStorage
{
  public static bool IsMockTableName(string tableName)
  {
    // Check if the table name starts with "mock"
    return tableName.StartsWith("mock", StringComparison.OrdinalIgnoreCase);
  }
  public static bool IsMockBlobName(string blobName)
  {
      // Check if the blob name starts with "mock-"
      return blobName.StartsWith("mock-", StringComparison.OrdinalIgnoreCase);
  }
}