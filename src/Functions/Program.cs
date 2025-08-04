using SharedStorage.Services;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Media;
using SharedStorage.Extensions;
using Functions.Extensions;
using Utils;
using Utils.Validation;
using Utils.Services;
using Utils.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;

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

                // Log configuration diagnostic info at startup
                var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
                var instrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                
                if (string.IsNullOrEmpty(connectionString) && string.IsNullOrEmpty(instrumentationKey))
                {
                    Console.WriteLine("WARNING: APPINSIGHTS CONFIGURATION MISSING - Neither APPLICATIONINSIGHTS_CONNECTION_STRING nor APPINSIGHTS_INSTRUMENTATIONKEY environment variables are set.");
                    Console.WriteLine($"Current environment: {Utils.Configuration.EnvironmentHelper.GetCurrentEnvironment()}");
                    Console.WriteLine("To fix this issue, set the APPLICATIONINSIGHTS_CONNECTION_STRING environment variable in your Azure Function App settings.");
                }
                else
                {
                    Console.WriteLine($"AppInsights configuration found - ConnectionString: {!string.IsNullOrEmpty(connectionString)}, InstrumentationKey: {!string.IsNullOrEmpty(instrumentationKey)}");
                }

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

                // Register Key Vault Service
                services.AddSingleton<IKeyVaultService>(sp =>
                {
                    var keyVaultUri = EnvironmentHelper.GetKeyVaultUri();
                    var logger = sp.GetRequiredService<ILogger<KeyVaultService>>();
                    return new KeyVaultService(keyVaultUri, logger);
                });

                // Register Key Vault-based APIKeyValidator
                services.AddSingleton<IAPIKeyValidator>(sp =>
                {
                    var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
                    var environment = EnvironmentHelper.GetCurrentEnvironment();
                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<KeyVaultApiKeyValidator>>();

                    return new KeyVaultApiKeyValidator(keyVaultService, environment, appLogger);
                });

                // Keep the fallback validator for backward compatibility during migration
                services.AddSingleton<ApiKeyValidator>(sp =>
                {
                    var validApiKey = configuration["X_API_ENVIRONMENT_KEY"]
                        ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");

                    if (string.IsNullOrWhiteSpace(validApiKey))
                    {
                        // If no legacy key is found, that's fine - we're using Key Vault now
                        validApiKey = "fallback-key";
                    }

                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
                    return new ApiKeyValidator(validApiKey, appLogger);
                });
            })
            .ConfigureFunctionsWorkerDefaults(builder => {
                // Register our telemetry middleware
                builder.UseMiddleware<Utils.Middleware.TelemetryMiddleware>();
            })
            .Build();

        Console.WriteLine("az_tw_website_functions function app is starting...");

        host.Run();
    }
}