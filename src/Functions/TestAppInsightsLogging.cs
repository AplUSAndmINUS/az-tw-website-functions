using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Collections.Generic;
using Utils;
using Utils.Validation;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Functions;

// Simple diagnostic function to test Application Insights logging
public class TestAppInsightsLogging
{
    private readonly IAppInsightsLogger<TestAppInsightsLogging> _logger;
    private readonly TelemetryClient _telemetryClient;

    public TestAppInsightsLogging(IAppInsightsLogger<TestAppInsightsLogging> logger, TelemetryClient telemetryClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
    }

    [Function("TestAppInsightsLogging")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test-logging")] HttpRequestData req)
    {
        _logger.LogInformation("TestAppInsightsLogging function triggered at: {time}", DateTime.UtcNow);

        try
        {
            // 1. Log via AppInsightsLogger
            _logger.LogInformation("This is a test information message");
            _logger.LogWarning("This is a test warning message");
            
            try
            {
                // Create an exception to log
                throw new InvalidOperationException("This is a test exception");
            }
            catch (Exception ex)
            {
                _logger.LogError("This is a test error message", ex);
            }

            // Test specific logging methods
            _logger.LogBlobQuery("test-container", "TestAppInsightsLogging", "test-prefix", 50, null);
            _logger.LogTableQuery("test-table", "TestAppInsightsLogging", "PartitionKey eq 'test'", 50, null);
            _logger.LogTableEntryUpsert("test-table", "TestAppInsightsLogging", "test-partition", "test-row");
            _logger.LogBlobUpload("test-container", "TestAppInsightsLogging", "test-blob.txt", 1024);
            
            // 2. Direct TelemetryClient tracking
            _telemetryClient.TrackEvent("TestAppInsightsEvent", new Dictionary<string, string>
            {
                { "TestProperty", "TestValue" },
                { "Timestamp", DateTime.UtcNow.ToString() }
            });
            
            // Track a dependency call (simulated)
            var startTime = DateTime.UtcNow;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            
            // Simulate some work
            await Task.Delay(50);
            timer.Stop();
            
            _telemetryClient.TrackDependency(
                "Simulated Dependency", 
                "SimulatedTarget",
                "SimulatedCommand", 
                startTime,
                timer.Elapsed, 
                true);
                
            // Track a metric
            _telemetryClient.TrackMetric("TestMetric", 42);
            
            // Track availability
            _telemetryClient.TrackAvailability(
                "TestAppInsightsLogging",
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(100),
                "TestLocation",
                true,
                "Success");
                
            // Ensure everything is flushed
            _telemetryClient.Flush();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { 
                message = "Test logging completed successfully", 
                timestamp = DateTime.UtcNow,
                testTypes = new[] {
                    "IAppInsightsLogger methods",
                    "Direct TelemetryClient tracking",
                    "Dependency tracking",
                    "Metric tracking",
                    "Availability tracking"
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred during test logging", ex);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = "An error occurred during test logging" });
            return response;
        }
    }
}
