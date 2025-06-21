using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;

namespace Utils;

public interface IAppInsightsLogger<T>
    where T : notnull
{
    void LogInformation(string message);
    void LogError(string message, Exception ex);
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

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
        _telemetryClient.TrackTrace(message);
    }

    public void LogError(string message, Exception ex)
    {
        _logger.LogError(ex, message);
        _telemetryClient.TrackException(ex, new Dictionary<string, string> { { "Message", message } });
    }
}