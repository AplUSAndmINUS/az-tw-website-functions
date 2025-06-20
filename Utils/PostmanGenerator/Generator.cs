using System.Text.Json;

namespace Utils.PostmanGenerator;

// this file is not used in the project
// it is a utility to generate Postman collections for the API
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