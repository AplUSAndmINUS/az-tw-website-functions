// namespace az_tw_website_functions.src.Functions.Authors.Models;

// public class AuthorModel
// {
//   public string AuthorSlug { get; set; } = default!; // from PartitionKey
//   public string FirstName { get; set; } = default!; // e.g. "Terence"
//   public string LastName { get; set; } = default!; // e.g. "Waters"
//   public string FullName => $"{FirstName} {LastName}".Trim();
//   public string Email { get; set; } = default!; // e.g. "terence@waters.com"
//   public string Username { get; set; } = default!;
//   public string DisplayName { get; set; } = default!;
//   public string? Location { get; set; } = default!; // e.g. "San Francisco, CA"
//   public string? Bio { get; set; } = default!;
//   public string? Website { get; set; } = default!;
//   public string? TwitterHandle { get; set; } = default!; // e.g. "@terencewaters"
//   public string? InstagramHandle { get; set; } = default!; // e.g. "@terencewaters"
//   public string? LinkedInHandle { get; set; } = default!; // e.g. "https://linkedin.com/in/terencewaters"

//   // Image metadata
//   public string? ProfileImageUrl { get; set; } = default!;
//   public string? ThumbnailUrl { get; set; } = default!; // fallback if null
//   public string? ImageContentType { get; set; } = default!;
//   public long? ImageSizeBytes { get; set; }

//   public int? ImageWidth { get; set; }
//   public int? ImageHeight { get; set; }

//   // Used in Production ONLY
//   public string? CdnUrl { get; set; } = default!;
//   public string? ThumbnailCdnUrl { get; set; } = default!;
// }