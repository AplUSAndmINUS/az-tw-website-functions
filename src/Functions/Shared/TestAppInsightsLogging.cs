using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using Utils;
using Utils.Validation;
using Microsoft.ApplicationInsights;

namespace Functions;

// Simple diagnostic function to test Application Insights logging
public class TestAppInsightsLogging
{
    private readonly IAppInsightsLogger<TestAppInsightsLogging> _logger;

    public TestAppInsightsLogging(IAppInsightsLogger<TestAppInsightsLogging> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("TestAppInsightsLogging")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test-logging")] HttpRequestData req)
    {
        _logger.LogInformation("TestAppInsightsLogging function triggered at: {time}", DateTime.UtcNow);

        try
        {
            // Log various message types
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = "Test logging completed successfully", timestamp = DateTime.UtcNow });
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
