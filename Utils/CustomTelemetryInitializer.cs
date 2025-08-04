using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace Utils;

public class CustomTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry == null) return;
        
        // Add environment information to every telemetry item
        telemetry.Context.GlobalProperties["Environment"] = 
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
        
        telemetry.Context.GlobalProperties["HostName"] = 
            Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "Local";

        // Add application version (if available)
        telemetry.Context.GlobalProperties["AppVersion"] =
            Environment.GetEnvironmentVariable("APP_VERSION") ?? "Unknown";
    }
}
