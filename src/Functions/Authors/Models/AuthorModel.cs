namespace Functions.Authors.Models;

public class AuthorModel
{
  public string AuthorSlug { get; set; } = default!; // from PartitionKey
  public string FirstName { get; set; } = default!; // e.g. "Terence"
  public string LastName { get; set; } = default!; // e.g. "Waters"
  public string Email { get; set; } = default!; // e.g. "terence@waters.com"
  public string Username { get; set; } = default!;
  public string DisplayName { get; set; } = default!;
  public string? Location { get; set; } = default!; // e.g. "San Francisco, CA"
  public string? Bio { get; set; } = default!;
  public string? Website { get; set; } = default!;
  public string? TwitterHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? InstagramHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? LinkedInHandle { get; set; } = default!; // e.g. "https://linkedin.com/in/terencewaters"
  public string? BlueskyHandle { get; set; } = default!; // e.g. "@terencewaters.bsky.social"

  // Image metadata
  public string? ProfileImageBlobContainer { get; set; } = default!;
  public string? ProfileImageFileName { get; set; } = default!; // e.g. "terence-waters.jpg"
  public string? ProfileImageCdnUrl { get; set; } = default!;
  public string? ThumbnailCdnUrl { get; set; } = default!; // fallback if null
  public string? ImageContentType { get; set; } = default!;
  public long? ImageSizeBytes { get; set; }

  public int? ImageWidth { get; set; }
  public int? ImageHeight { get; set; }
}