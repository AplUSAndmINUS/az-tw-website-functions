namespace Utils.Validation;

public static class DataValidation
{
    public static string Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Field '{fieldName}' is required.");
        return value;
    }

    public static string? SafeTrim(string? value, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static string? RequireMinLength(string? value, int minLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < minLength)
            throw new ArgumentException($"Field '{fieldName}' must be at least {minLength} characters long.");
        return value;
    }

    public static string? IsValidEmail(string? email, string fieldName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(email ?? string.Empty, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException($"Field '{fieldName}' must be a valid email address.");
        return email;
    }

    public static bool TryValidateEmail(string? email)
    {
        try
        {
            _ = IsValidEmail(email, "Email");
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static int? RequirePositiveInt(int? value, string fieldName)
    {
        if (!value.HasValue || value <= 0)
            throw new ArgumentException($"Field '{fieldName}' must be a positive integer.");
        return value.Value;
    }

    public static long? RequirePositiveLong(long? value, string fieldName)
    {
        if (!value.HasValue || value <= 0)
            throw new ArgumentException($"Field '{fieldName}' must be a positive long integer.");
        return value.Value;
    }

    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Ensure URL starts with http:// or https://
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url.TrimStart('/');
        }

        return url;
    }

    public static 
}