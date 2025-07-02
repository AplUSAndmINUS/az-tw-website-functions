using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils;

public static class JsonHelper
{
  private static readonly JsonSerializerOptions SerializeOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
  };

  private static readonly JsonSerializerOptions DeserializeOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip
  };

  /// <summary>
  /// Serialize object to JSON string with camelCase naming for React frontend.
  /// Always returns valid JSON, never throws exceptions.
  /// </summary>
  public static string Serialize<T>(T obj)
  {
    try
    {
      if (obj == null) return "null";
      return JsonSerializer.Serialize(obj, SerializeOptions);
    }
    catch
    {
      return "{}";
    }
  }

  /// <summary>
  /// Deserialize JSON string to object with C# property naming support.
  /// Always returns a valid object, never throws exceptions.
  /// </summary>
  public static T Deserialize<T>(string json) where T : new()
  {
    try
    {
      if (string.IsNullOrWhiteSpace(json))
        return new T();

      var result = JsonSerializer.Deserialize<T>(json, DeserializeOptions);
      return result ?? new T();
    }
    catch
    {
      return new T();
    }
  }

}