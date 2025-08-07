namespace Functions.Proxy.Models;

/// <summary>
/// Configuration settings for the proxy function
/// </summary>
public class ProxyConfiguration
{
    /// <summary>
    /// Base URL for forwarding requests to target functions
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// List of allowed origins for CORS validation
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new();

    /// <summary>
    /// List of allowed IP addresses or ranges for IP filtering
    /// </summary>
    public List<string> AllowedIpRanges { get; set; } = new();

    /// <summary>
    /// Maximum request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable detailed request logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// Key Vault secret name for the API key
    /// </summary>
    public string ApiKeySecretName { get; set; } = "X-API-ENVIRONMENT-KEY";

    /// <summary>
    /// Cache duration for API keys in minutes
    /// </summary>
    public int ApiKeyCacheDurationMinutes { get; set; } = 5;
}

/// <summary>
/// Result of proxy request validation
/// </summary>
public class ProxyValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;

    public static ProxyValidationResult Success() => new() { IsValid = true };
    public static ProxyValidationResult Failure(string errorMessage, string errorCode = "VALIDATION_FAILED") 
        => new() { IsValid = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}