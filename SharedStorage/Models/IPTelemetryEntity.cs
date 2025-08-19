using Azure;
using Azure.Data.Tables;

namespace SharedStorage.Models;

/// <summary>
/// Entity for tracking IP-based request telemetry in Azure Table Storage
/// Used for throttling protection against abuse and DDoS-style attacks
/// </summary>
public class IPTelemetryEntity : ITableEntity
{
    /// <summary>
    /// IP Address (serves as PartitionKey for efficient querying by IP)
    /// </summary>
    public string PartitionKey { get; set; } = string.Empty;

    /// <summary>
    /// Unique request identifier (UUID or timestamp-based, serves as RowKey)
    /// </summary>
    public string RowKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the request (auto-managed by Azure Table Storage)
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Azure Table Storage ETag for optimistic concurrency
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// The endpoint that was requested
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP Referer header value
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// User-Agent header value
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP method used (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request timestamp in UTC (for precise time-based filtering)
    /// </summary>
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Constructor for creating a new IP telemetry entry
    /// </summary>
    public IPTelemetryEntity()
    {
    }

    /// <summary>
    /// Constructor with parameters for easy creation
    /// </summary>
    public IPTelemetryEntity(string ipAddress, string requestId, string endpoint, string httpMethod, string? referer = null, string? userAgent = null)
    {
        PartitionKey = ipAddress;
        RowKey = requestId;
        Endpoint = endpoint;
        HttpMethod = httpMethod;
        Referer = referer;
        UserAgent = userAgent;
        RequestTimestamp = DateTime.UtcNow;
    }
}