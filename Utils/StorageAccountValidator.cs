namespace Utils;

public static class StorageAccountValidator
{
    public static void ValidateStorageAccountName(string storageAccountName)
    {
        if (string.IsNullOrEmpty(storageAccountName))
        {
            throw new ArgumentException("Storage account name cannot be null or empty.", nameof(storageAccountName));
        }
        if (storageAccountName.Length < 3 || storageAccountName.Length > 24)
        {
            throw new ArgumentException("Storage account name must be between 3 and 24 characters long.", nameof(storageAccountName));
        }
    }
}