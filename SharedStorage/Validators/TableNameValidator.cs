namespace SharedStorage.Validators;

public static class TableNameValidator
{
  public static string ValidateTableName(string tableName)
  {
    Console.WriteLine($"DEBUG: TableNameValidator.ValidateTableName - Input={tableName}");

    if (string.IsNullOrEmpty(tableName))
    {
      Console.WriteLine($"DEBUG: TableNameValidator - Validation failed: Table name cannot be null or empty");
      throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));
    }

    if (tableName.Length < 3 || tableName.Length > 63)
    {
      Console.WriteLine($"DEBUG: TableNameValidator - Validation failed: Table name length {tableName.Length} not between 3 and 63");
      throw new ArgumentException("Table name must be between 3 and 63 characters long.", nameof(tableName));
    }

    if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z][a-zA-Z0-9]{2,62}$"))
    {
      Console.WriteLine($"DEBUG: TableNameValidator - Validation failed: Table name {tableName} does not match regex pattern");
      throw new ArgumentException("Table name must start with a letter and only contain alphanumeric characters.", nameof(tableName));
    }

    var validatedName = tableName.ToLowerInvariant();
    Console.WriteLine($"DEBUG: TableNameValidator - Validation successful: Input={tableName}, Validated={validatedName}");
    return validatedName;
  }
}