namespace az_tw_website_functions.src.Functions.Authors.Models;

public class AuthorModelMapper
{
  public static string? NormalizeUrl(string? url)
  {
    if (string.IsNullOrWhiteSpace(url))
      return null;

    // Ensure URL starts with http:// or https://
    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
      url = "https://" + url.TrimStart('/');
    }

    return url;
  }

  public static string Required(string? value, string name) =>
    string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException($"{name} is required.")
        : value;

  public static string? SafeTrim(string? value, int maxLength = 100)
  {
    if (string.IsNullOrWhiteSpace(value))
      return null;

    return value.Length <= maxLength ? value : value.Substring(0, maxLength);
  }
  public static AuthorModel Map(AuthorEntity author, AuthorImagesMetadataEntity? image)
  {
    ArgumentNullException.ThrowIfNull(author);
    ArgumentNullException.ThrowIfNull(author.FirstName);
    ArgumentNullException.ThrowIfNull(author.LastName);
    ArgumentNullException.ThrowIfNull(author.Email);
    ArgumentNullException.ThrowIfNull(author.Username);

    return new AuthorModel
    {
      AuthorSlug = author.AuthorSlug,
      FirstName = Required(SafeTrim(author.FirstName), nameof(author.FirstName)),
      LastName = Required(SafeTrim(author.LastName), nameof(author.LastName)),
      Email = Required(SafeTrim(author.Email), nameof(author.Email)),
      Username = Required(SafeTrim(author.Username), nameof(author.Username)),
      DisplayName = SafeTrim(author?.DisplayName) ?? author.Username!, // Fallback to username if display name is empty
      Location = SafeTrim(author?.Location),
      Bio = SafeTrim(author?.Bio),
      Website = NormalizeUrl(author?.Website),
      TwitterHandle = SafeTrim(author?.TwitterHandle),
      InstagramHandle = SafeTrim(author?.InstagramHandle),
      LinkedInHandle = SafeTrim(author?.LinkedInHandle),

      ProfileImageUrl = image?.CdnUrl,
      ThumbnailUrl = image?.ThumbnailCdnUrl,
      ImageContentType = image?.ContentType,
      ImageSizeBytes = image?.SizeInBytes,
      ImageWidth = image?.Width,
      ImageHeight = image?.Height,

      CdnUrl = image?.CdnUrl ?? "/images/default-profile.png",
      ThumbnailCdnUrl = image?.ThumbnailCdnUrl ?? "/images/default-profile-thumbnail.png"
    };
  }
}