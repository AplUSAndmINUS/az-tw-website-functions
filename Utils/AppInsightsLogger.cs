using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

namespace Utils;

public interface IAppInsightsLogger<T>
    where T : notnull
{
    void LogInformation(string message, params object[] args);
    void LogError(string message, Exception ex, params object[] args);
    void LogWarning(string message, params object[] args);

    void LogBlobQuery(string containerName, string functionName, string? prefix, int pageSize, string? continuationToken);
    void LogTableQuery(string tableName, string functionName, string? filter, int pageSize, string? continuationToken);
    void LogBlobDownload(string containerName, string functionName, string blobName);
    void LogBlobUpload(string containerName, string functionName, string blobName, long size);
}

public class AppInsightsLogger<T> : IAppInsightsLogger<T>
    where T : notnull
{
    private readonly ILogger<T> _logger;
    private readonly TelemetryClient _telemetryClient;

    public AppInsightsLogger(ILogger<T> logger, TelemetryClient telemetryClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public void LogInformation(string message, params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            message = string.Format(message, args);
        }

        // Log to both ILogger and Application Insights
        {
            _logger.LogInformation(message);
            _telemetryClient.TrackTrace(message);
        }
    }

    public void LogError(string message, Exception ex, params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            message = string.Format(message, args);
        }

        // Log to both ILogger and Application Insights
        {
            _logger.LogError(ex, message);
            _telemetryClient.TrackException(ex, new Dictionary<string, string> { { "Message", message } });
        }
    }

    public void LogWarning(string message, params object[] args)
    {
        if (args != null && args.Length > 0)
        {
            message = string.Format(message, args);
        }

        // Log to both ILogger and Application Insights
        {
            _logger.LogWarning(message);
            _telemetryClient.TrackTrace(message);
        }
    }

    public void LogBlobQuery(string containerName, string functionName, string? prefix, int pageSize, string? continuationToken)
    {
        _logger.LogInformation("Blob query issued: Container={Container}, Function={Function}, Prefix={Prefix}, PageSize={PageSize}, ContinuationToken={Token}",
        containerName, functionName, prefix ?? "<null>", pageSize, continuationToken ?? "<null>");

        _telemetryClient.TrackTrace("Blob query executed", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "Prefix", prefix ?? "<null>" },
            { "PageSize", pageSize.ToString() },
            { "ContinuationToken", continuationToken ?? "<null>" }
        });
    }

    public void LogTableQuery(string tableName, string functionName, string? filter, int pageSize, string? continuationToken)
    {
        _logger.LogInformation("Table query issued: Table={Table}, Function={Function}, Filter={Filter}, PageSize={PageSize}, ContinuationToken={Token}",
            tableName, functionName, filter ?? "<null>", pageSize, continuationToken ?? "<null>");

        _telemetryClient.TrackTrace("Table query executed", new Dictionary<string, string>
        {
            { "TableName", tableName },
            { "FunctionName", functionName },
            { "Filter", filter ?? "<null>" },
            { "PageSize", pageSize.ToString() },
            { "ContinuationToken", continuationToken ?? "<null>" }
        });
    }

    public void LogBlobDownload(string containerName, string functionName, string blobName)
    {
        _logger.LogInformation("Blob download initiated: Container={Container}, Function={Function}, Blob={Blob}", containerName, functionName, blobName);
        _telemetryClient.TrackTrace("Blob download initiated", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "BlobName", blobName }
        });
    }

    public void LogBlobUpload(string containerName, string functionName, string blobName, long size)
    {
        _logger.LogInformation("Blob upload initiated: Container={Container}, Function={Function}, Blob={Blob}, Size={Size} bytes", containerName, functionName, blobName, size);
        _telemetryClient.TrackTrace("Blob upload initiated", new Dictionary<string, string>
        {
            { "ContainerName", containerName },
            { "FunctionName", functionName },
            { "BlobName", blobName },
            { "Size", size.ToString() }
        });
    }
}