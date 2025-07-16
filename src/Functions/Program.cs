using SharedStorage.Services;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Extensions;
using Functions.Extensions;
using Utils;
using Utils.Validation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Linq;

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                // Register Application Insights telemetry
                services.AddApplicationInsightsTelemetryWorkerService();

                // Register AppInsightsLogger
                services.AddSingleton(typeof(IAppInsightsLogger<>), typeof(AppInsightsLogger<>));

                // Add storage services (base infrastructure)
                services.AddStorageServices();

                // Add media services (includes handlers and processing)
                services.AddMediaServices();

                // Add shared content services (from SharedStorage)
                services.AddContentServices();

                // Add Function-specific services (BlogPost, Author, etc.)
                services.AddFunctionServices();

                // Register APIKeyValidator
                services.AddSingleton<IAPIKeyValidator>(sp =>
                {
                    var validApiKey = configuration["X_API_ENVIRONMENT_KEY"]
                        ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

                    if (string.IsNullOrWhiteSpace(validApiKey))
                        throw new InvalidOperationException("Missing X_API_ENVIRONMENT_KEY in configuration.");

                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
                    return new ApiKeyValidator(validApiKey, appLogger);
                });
            })
            .ConfigureFunctionsWorkerDefaults()
            .Build();

        Console.WriteLine("az_tw_website_functions function app is starting...");

        host.Run();
    }
}