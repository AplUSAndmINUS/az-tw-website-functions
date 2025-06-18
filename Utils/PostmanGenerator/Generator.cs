using System.Text.Json;

public class Generator
{
  public static void Main(string[] args)
  {
    var environment = args.FirstOrDefault() ?? "mockdev";
    var collection = PostmanCollectionBuilder.Build(environment);

    var json = JsonSerializer.Serialize(collection, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText($"tw-api-{environment}.postman_collection.json", json);

    Console.WriteLine($"✅ Generated collection for {environment}");
  }
}
public class PostmanCollectionBuilder
{
  public static object Build(string env)
  {
    var apiKey = env switch
    {
      "develop" => "dev-api-key-TW-website",
      "test" => "***REMOVED***-TW-website",
      "master" => "prod-api-key-TW-website",
      _ => throw new ArgumentException("Invalid environment specified", nameof(env))
    };
    if (env == null)
      throw new ArgumentNullException(nameof(env), "Environment cannot be null");

    string? rawUrl = env switch
    {
      "master" => "https://api.terencewaters.com/",
      "test" => "https://mock-tst-api.terencewaters.com/",
      "develop" => "https://mock-dev-api.terencewaters.com/",
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
              name = "Health Check",
              request = new
              {
                  method = "GET",
                  url = new
                  {
                      raw = rawUrl,
                      protocol = "https",
                      host = new[] { $"mock{env}-api", "terencewaters", "com" },
                      path = new[] { "api", "health" }
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