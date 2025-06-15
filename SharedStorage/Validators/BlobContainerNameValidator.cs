namespace SharedStorage.Validators;

public static class BlobContainerNameValidator
{
    public static void ValidateBlobContainerName(string containerName)
    {
        if (string.IsNullOrEmpty(containerName))
        {
            throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
        }
        if (containerName.Length < 3 || containerName.Length > 63)
        {
            throw new ArgumentException("Container name must be between 3 and 63 characters long.", nameof(containerName));
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(containerName, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
        {
            throw new ArgumentException("Container name can only contain lowercase alphanumeric characters and hyphens, and must start and end with an alphanumeric character.", nameof(containerName));
        }
    }
}