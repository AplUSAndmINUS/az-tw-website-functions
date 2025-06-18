using System.Text.Json;
using Utils.Constants;

public class Generator
{
  public static void Main(string[] args)
  {
    var environment = args.FirstOrDefault() ?? "develop";
    var env = environment == "master" ? "production" : environment;
    var collection = PostmanCollectionBuilder.Build(env);

    var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText($"tw-api-{environment}.postman_collection.json", json);

    Console.WriteLine($"✅ Generated collection for {environment}");
  }
}
public class PostmanCollectionBuilder
{
  public static object Build(string env)
  {
    var apiKey = Environment.GetEnvironmentVariable("X_API_ENVIRONMENT_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
      throw new InvalidOperationException("Missing API key for the current environment.");

    if (env == null)
      throw new ArgumentNullException(nameof(env), "Environment cannot be null");

    string[] host = env switch
    {
      "develop" => ["mock-dev-api", "terencewaters", "com"],
      "test" => ["mock-tst-api", "terencewaters", "com"],
      "production" => ["api", "terencewaters", "com"],
      _ => throw new ArgumentException("Invalid environment specified", nameof(env))
    };

    string? rawUrl = env switch
    {
      "develop" => ApiUrls.MockBaseDevUrl,
      "test" => ApiUrls.MockBaseTestUrl,
      "production" => ApiUrls.BaseUrl,
      _ => throw new ArgumentException("Invalid environment specified", nameof(env))
    };

    return new
    {
      info = new
      {
        name = $"TW API ({env})",
        schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
      },
      item = new[]
      {
          new {
              name = "Get Blog Posts Check",
              request = new
              {
                  method = "GET",
                  url = new
                  {
                      raw = rawUrl,
                      protocol = "https",
                      host,
                      path = new[] { "blog" }
                  },
                  header = new[]
                  {
                      new { key = "x-api-key", value = apiKey }
                  }
              }
          }
        }
    };
  }
}