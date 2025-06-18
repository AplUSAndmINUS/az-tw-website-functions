using System.Text.Json;

namespace Utils.PostmanGenerator;

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