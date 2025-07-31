using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.ApplicationInsights.DataContracts;

namespace Utils;

public interface IAppInsightsLogger<T>
    where T : notnull
{
    void LogInformation(string message, params object[] args);
    void LogError(string message, Exception ex, params object[] args);
    void LogWarning(string message, params object[] args);

    void LogBlobQuery(string containerName, string functionName, string? prefix, int pageSize, string? continuationToken);
    void LogTableQuery(string tableName, string functionName, string? filter, int pageSize, string? continuationToken);
    void LogTableEntryUpsert(string tableName, string functionName, string partitionKey, string rowKey);
    void LogTableEntryDelete(string tableName, string functionName, string partitionKey, string rowKey);
    void LogBlobDownload(string containerName, string functionName, string blobName);
    void LogBlobUpload(string containerName, string functionName, string blobName, long size);
}

public class AppInsightsLogger<T> : IAppInsightsLogger<T>
    where T : notnull
{
    private readonly ILogger<T> _appLogger;
    private readonly TelemetryClient _telemetryClient;

    public AppInsightsLogger(ILogger<T> logger, TelemetryClient telemetryClient)
    {
        _appLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    private string SafeFormat(string message, params object[] args)
    {
        try
        {
            // Convert structured logging placeholders to indexed placeholders
            if (args != null && args.Length > 0)
            {
                // For structured logging, we need to use the original message format
                // Let's just return the original message with args appended for safety
                return $"{message} [Args: {string.Join(", ", args)}]";
            }
            return message;
        }
        catch (Exception ex)
        {
            _appLogger.LogWarning("Failed to format message. Original message: {Message}, Args: {Args}", message, args != null ? string.Join(", ", args) : "null");
            _appLogger.LogWarning("Exception: {Exception}", ex.ToString());
            return $"Failed to format message. Original message: {message}, Args: {(args != null ? string.Join(", ", args) : "null")}";
        }
    }

    public void LogInformation(string message, params object[] args)
    {
        string finalMessage = SafeFormat(message, args);
        _appLogger.LogInformation(finalMessage);
        _telemetryClient.TrackTrace(finalMessage);
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogError(string message, Exception ex, params object[] args)
    {
        string finalMessage = SafeFormat(message, args);
        _appLogger.LogError(ex, finalMessage);
        _telemetryClient.TrackException(ex, new Dictionary<string, string> { { "Message", finalMessage } });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogWarning(string message, params object[] args)
    {
        string finalMessage = SafeFormat(message, args);
        _appLogger.LogWarning(finalMessage);
        _telemetryClient.TrackTrace(finalMessage, SeverityLevel.Warning, new Dictionary<string, string> { { "Message", finalMessage } });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogBlobQuery(string containerName, string functionName, string? prefix, int pageSize, string? continuationToken)
    {
        _appLogger.LogInformation("Blob query issued: Container={Container}, Function={Function}, Prefix={Prefix}, PageSize={PageSize}, ContinuationToken={Token}",
        containerName, functionName, prefix ?? "<null>", pageSize, continuationToken ?? "<null>");

        _telemetryClient.TrackTrace("Blob query executed", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "Prefix", prefix ?? "<null>" },
            { "PageSize", pageSize.ToString() },
            { "ContinuationToken", continuationToken ?? "<null>" }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogTableQuery(string tableName, string functionName, string? filter, int pageSize, string? continuationToken)
    {
        _appLogger.LogInformation("Table query issued: Table={Table}, Function={Function}, Filter={Filter}, PageSize={PageSize}, ContinuationToken={Token}",
            tableName, functionName, filter ?? "<null>", pageSize, continuationToken ?? "<null>");

        _telemetryClient.TrackTrace("Table query executed", new Dictionary<string, string>
        {
            { "TableName", tableName },
            { "FunctionName", functionName },
            { "Filter", filter ?? "<null>" },
            { "PageSize", pageSize.ToString() },
            { "ContinuationToken", continuationToken ?? "<null>" }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogTableEntryUpsert(string tableName, string functionName, string partitionKey, string rowKey)
    {
        _appLogger.LogInformation("Table upsert initiated: Table={Table}, Function={Function}, PartitionKey={PartitionKey}, RowKey={RowKey}",
            tableName, functionName, partitionKey, rowKey);

        _telemetryClient.TrackTrace("Table upsert initiated", new Dictionary<string, string>
        {
            { "TableName", tableName },
            { "FunctionName", functionName },
            { "PartitionKey", partitionKey },
            { "RowKey", rowKey }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogTableEntryDelete(string tableName, string functionName, string partitionKey, string rowKey)
    {
        _appLogger.LogInformation("Table delete initiated: Table={Table}, Function={Function}, PartitionKey={PartitionKey}, RowKey={RowKey}",
            tableName, functionName, partitionKey, rowKey);

        _telemetryClient.TrackTrace("Table delete initiated", new Dictionary<string, string>
        {
            { "TableName", tableName },
            { "FunctionName", functionName },
            { "PartitionKey", partitionKey },
            { "RowKey", rowKey }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogBlobDownload(string containerName, string functionName, string blobName)
    {
        _appLogger.LogInformation("Blob download initiated: Container={Container}, Function={Function}, Blob={Blob}", containerName, functionName, blobName);
        _telemetryClient.TrackTrace("Blob download initiated", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "BlobName", blobName }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }

    public void LogBlobUpload(string containerName, string functionName, string blobName, long size)
    {
        _appLogger.LogInformation("Blob upload initiated: Container={Container}, Function={Function}, Blob={Blob}, Size={Size} bytes", containerName, functionName, blobName, size);
        _telemetryClient.TrackTrace("Blob upload initiated", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "BlobName", blobName },
            { "Size", size.ToString() }
        });
        _telemetryClient.Flush(); // Ensure telemetry is sent immediately
    }
}