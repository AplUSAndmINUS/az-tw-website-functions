using System.Text.Json;

namespace Utils.Validation;

public static class DataValidation
{
    #region Basic Validation

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

    #endregion

    #region Email Validation

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

    #endregion

    #region Numeric Validation

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

    #endregion

    #region URL Validation

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

    #endregion

    #region Content Model Validation

    /// <summary>
    /// Validates common content model required fields
    /// </summary>
    /// <param name="title">The title of the content</param>
    /// <param name="authorSlug">The author slug</param>
    /// <param name="content">The main content</param>
    /// <param name="slug">The content slug</param>
    /// <param name="category">The content category</param>
    public static void ValidateContentRequiredFields(string? title, string? authorSlug, string? content, string? slug, string? category)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(authorSlug);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(slug);
        ArgumentNullException.ThrowIfNull(category);
    }

    /// <summary>
    /// Ensures that status and isPublished values are consistent
    /// </summary>
    /// <param name="status">The current status string</param>
    /// <param name="isPublished">Whether the content is published</param>
    /// <returns>The corrected status value</returns>
    public static string EnsureStatusConsistency(string? status, bool isPublished)
    {
        string safeStatus = SafeTrim(status, 20) ?? "Draft";

        if (isPublished && safeStatus != "Published")
        {
            return "Published";
        }
        else if (!isPublished && safeStatus == "Published")
        {
            return "Draft";
        }

        return safeStatus;
    }

    /// <summary>
    /// Safely deserialize tags from JSON string
    /// </summary>
    /// <param name="tagsJson">The JSON string representing tags</param>
    /// <returns>An array of tag strings</returns>
    public static string[] DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrEmpty(tagsJson))
            return Array.Empty<string>();

        try
        {
            return JsonSerializer.Deserialize<string[]>(tagsJson) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    #endregion
}