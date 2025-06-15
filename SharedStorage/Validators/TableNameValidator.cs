namespace SharedStorage.Validators;

public static class TableNameValidator
{
  public static void ValidateTableName(string tableName)
  {
    
    if (string.IsNullOrEmpty(tableName))
    {
      throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
    }

    if (tableName.Length < 3 || tableName.Length > 63)
    {
      throw new ArgumentException("Table name must be between 3 and 63 characters long.", nameof(tableName));
    }

    if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9]{2,62}$"))
    {
      throw new ArgumentException("Table name must start with a letter and only contain alphanumeric characters.", nameof(tableName));
    }
  }
}