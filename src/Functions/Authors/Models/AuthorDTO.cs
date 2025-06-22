namespace Functions.Authors.Models;

public class AuthorDTO
{
  public string AuthorSlug { get; set; } = default!; // e.g. "terence-waters"
  public string DisplayName { get; set; } = default!; // e.g. "terencewaters"
  public string FirstName { get; set; } = default!; // e.g. "Terence"
  public string LastName { get; set; } = default!; // e.g. "Waters"
  // public string Email { get; set; } = default!; // e.g. "terence.waters@example.com"
  public string Username { get; set; } = default!; // e.g. "terencewaters"
  public string? Location { get; set; } = default!; // e.g. "San Francisco, CA"
  public string? Bio { get; set; } = default!; // e.g. "Software Engineer with a passion for open source."
  public string? Website { get; set; } = default!; // e.g. "https://terencewaters.com"
  public string? TwitterHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? InstagramHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? LinkedInHandle { get; set; } = default!; // e.g. "https://www.linkedin.com/in/terencewaters"
  public string? BlueskyHandle { get; set; } = default!; // e.g. "@terencewaters.bsky.social"

  public bool HasValidProfileImage { get; set; } = false; // Indicates if the profile image is valid and available

  public string? ProfileImageFileName { get; set; } = default!; // e.g. "terence-waters.jpg"
  public string? ProfileImageCdnUrl { get; set; } = default!; // e.g. "https://example.com/images/terence-waters.jpg"
  public string? ThumbnailCdnUrl { get; set; } = default!; // e.g. "https://example.com/images/terence-waters-thumbnail.jpg"
}