using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Utils.Middleware;

public class TelemetryMiddleware : IFunctionsWorkerMiddleware
{
    private readonly TelemetryClient _telemetryClient;

    public TelemetryMiddleware(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        string functionName = context.FunctionDefinition.Name;
        var startTime = DateTime.UtcNow;
        
        // Track function start
        _telemetryClient.TrackTrace($"Starting function: {functionName}", 
            Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information,
            new Dictionary<string, string> {
                { "FunctionName", functionName },
                { "InvocationId", context.InvocationId },
                { "StartTime", startTime.ToString("o") }
            });

        Exception? exception = null;
        
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            
            // Track the exception
            _telemetryClient.TrackException(ex, new Dictionary<string, string> {
                { "FunctionName", functionName },
                { "InvocationId", context.InvocationId }
            });
            
            throw;
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            // Track function completion
            _telemetryClient.TrackRequest(
                functionName,
                startTime,
                duration,
                exception == null ? "200" : "500",
                exception == null);

            // Ensure telemetry is sent
            _telemetryClient.Flush();
        }
    }
}
