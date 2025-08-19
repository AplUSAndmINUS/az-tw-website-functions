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

                // Register the appropriate API Key Validator based on environment
                var environment = EnvironmentHelper.GetCurrentEnvironment();
                Console.WriteLine($"DEBUG: Detected environment: {environment}");
                if (environment == "localhost")
                {
                    Console.WriteLine("DEBUG: Registering simple API key validator for localhost");
                    // Use simple API key validator for local development
                    services.AddSingleton<IAPIKeyValidator>(sp =>
                    {
                        var validApiKey = configuration["X_API_ENVIRONMENT_KEY"]
                            ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
                            ?? "test-api-key"; // Default key for local development

                        var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
                        Console.WriteLine($"DEBUG: Simple API key validator created with key: {validApiKey}");
                        return new ApiKeyValidator(validApiKey, appLogger);
                    });
                }
                else
                {
                    Console.WriteLine($"DEBUG: Registering Key Vault API key validator for environment: {environment}");
                    // Register Key Vault Service for deployed environments
                    services.AddSingleton<IKeyVaultService>(sp =>
                    {
                        var keyVaultUri = EnvironmentHelper.GetKeyVaultUri();
                        var logger = sp.GetRequiredService<ILogger<KeyVaultService>>();
                        return new KeyVaultService(keyVaultUri, logger);
                    });

                    // Use Key Vault-based validator for deployed environments
                    services.AddSingleton<IAPIKeyValidator>(sp =>
                    {
                        var keyVaultService = sp.GetRequiredService<IKeyVaultService>();
                        var appLogger = sp.GetRequiredService<IAppInsightsLogger<KeyVaultApiKeyValidator>>();

                        return new KeyVaultApiKeyValidator(keyVaultService, environment, appLogger);
                    });
                }

                // Keep the fallback validator for backward compatibility during migration
                services.AddSingleton<ApiKeyValidator>(sp =>
                {
                    var validApiKey = configuration["X_API_ENVIRONMENT_KEY"]
                        ?? Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY")
                        ?? "test-api-key"; // Default key for local development

                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<ApiKeyValidator>>();
                    return new ApiKeyValidator(validApiKey, appLogger);
                });

                // Register PublicAPIKeyValidator for public endpoints that allow GET without API key
                // This needs to be registered after the base IAPIKeyValidator
                services.AddSingleton<PublicAPIKeyValidator>(sp =>
                {
                    var baseValidator = sp.GetRequiredService<IAPIKeyValidator>();
                    var appLogger = sp.GetRequiredService<IAppInsightsLogger<PublicAPIKeyValidator>>();
                    return new PublicAPIKeyValidator(baseValidator, appLogger);
                });
            })
            .ConfigureFunctionsWorkerDefaults(builder => {
                // Register IP throttling middleware first (runs before other middleware)
                builder.UseMiddleware<Functions.Middleware.IPThrottlingMiddleware>();
                
                // Register our telemetry middleware
                builder.UseMiddleware<Utils.Middleware.TelemetryMiddleware>();
            })
            .Build();

        Console.WriteLine("az_tw_website_functions function app is starting...");

        host.Run();
    }
}