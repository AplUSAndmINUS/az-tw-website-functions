using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Functions.Authors.Helpers;

public static class SlugGenerator
{
  public static string FromString(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    // Normalize accents, like é → e
    string normalized = input.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();

    foreach (var ch in normalized)
    {
      var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
      if (uc != UnicodeCategory.NonSpacingMark)
      {
        sb.Append(ch);
      }
    }

    string clean = sb.ToString().Normalize(NormalizationForm.FormC);

    // Convert to lower, remove invalid chars, collapse spaces/hyphens, trim
    clean = clean.ToLowerInvariant();
    clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");            // Remove all non-alphanumeric chars
    clean = Regex.Replace(clean, @"[\s-]+", "-");                 // Convert multiple spaces/hyphens to single hyphen
    clean = clean.Trim('-');                                      // Trim leading/trailing hyphens

    return clean;
  }

  public static string FromName(string firstName, string lastName)
  {
    return FromString($"{firstName}-{lastName}");
  }

  public static string FromAnonymous()
  {
    // Generate a random slug for anonymous authors
    var random = new Random();
    return $"anonymous-{random.Next(1000, 9999)}";
  }
}