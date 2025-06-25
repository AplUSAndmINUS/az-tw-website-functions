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

    public static string? RequireAtLeastOne(string? value1, string? value2, string fieldName1, string fieldName2)
    {
        if (string.IsNullOrWhiteSpace(value1) && string.IsNullOrWhiteSpace(value2))
            throw new ArgumentException($"At least one of '{fieldName1}' or '{fieldName2}' must be provided.");
        return value1 ?? value2;
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

    /// <summary>
    /// Normalizes and validates a URL string
    /// </summary>
    /// <param name="url">The URL to normalize</param>
    /// <returns>A normalized URL or null if invalid</returns>
    public static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();

        // Basic URL validation
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return uri.ToString();
        }

        return null;
    }
}