using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Configuration;
using Microsoft.ApplicationInsights;

namespace Functions.Shared;

/// <summary>
/// Diagnostic function to help identify AppInsights and environment configuration issues
/// </summary>
public class DiagnosticsFunction
{
    private readonly IAppInsightsLogger<DiagnosticsFunction> _appLogger;
    private readonly TelemetryClient _telemetryClient;

    public DiagnosticsFunction(IAppInsightsLogger<DiagnosticsFunction> logger, TelemetryClient telemetryClient)
    {
        _appLogger = logger;
        _telemetryClient = telemetryClient;
    }

    [Function("AppInsightsDiagnostics")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "diagnostics/appinsights")] HttpRequestData req,
        FunctionContext executionContext)
    {
        _appLogger.LogInformation("AppInsights diagnostics function triggered");

        var diagnostics = new
        {
            Environment = EnvironmentHelper.GetCurrentEnvironment(),
            AppInsightsConfiguration = new
            {
                ConnectionString = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")) ? "Set" : "Not Set",
                InstrumentationKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")) ? "Set" : "Not Set",
                TelemetryClientConfigured = _telemetryClient != null,
                TelemetryClientInstrumentationKey = _telemetryClient?.InstrumentationKey ?? "Not Available"
            },
            FunctionAppSettings = new
            {
                WebsiteSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "Not Set",
                AzureWebJobsStorage = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AzureWebJobsStorage")) ? "Set" : "Not Set",
                KeyVaultUri = EnvironmentHelper.GetKeyVaultUri()
            },
            Timestamp = DateTime.UtcNow
        };

        // Test actual logging
        _appLogger.LogInformation("Diagnostics test - Information level log");
        _appLogger.LogWarning("Diagnostics test - Warning level log");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var json = JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await response.WriteStringAsync(json);
        return response;
    }
}