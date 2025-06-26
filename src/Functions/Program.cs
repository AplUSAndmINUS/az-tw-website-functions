using SharedStorage.Services;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.MediaServices;
using SharedStorage.Services.ContentServices;
using SharedStorage.Extensions;
using Utils;
using Utils.Validation;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker;

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

                // Add media and storage services (includes base storage services and media handlers)
                services.AddMediaServices();
                services.AddStorageServices();

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

                // Register Author Service
                services.AddSingleton<Functions.Authors.Services.IAuthorService, Functions.Authors.Services.AuthorService>();

                // Register BlogPost Service
                services.AddSingleton<Functions.BlogPosts.Services.IBlogPostService, Functions.BlogPosts.Services.BlogPostService>();

            })
            .ConfigureFunctionsWorkerDefaults()
            .Build();

        Console.WriteLine("az_tw_website_functions function app is starting...");

        host.Run();
    }
}