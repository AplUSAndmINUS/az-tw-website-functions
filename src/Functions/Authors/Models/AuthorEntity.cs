using Azure.Data.Tables;
using Azure;
using Utils.Validation;

namespace Functions.Authors.Models;

public class AuthorEntity : ITableEntity
{
  public string PartitionKey { get; set; } = default!; // e.g. "terence-waters"
  public string RowKey { get; set; } = "profile"; // optional—could be "profile" or "metadata"
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string FirstName { get; set; } = default!; // e.g. "Terence"
  public string LastName { get; set; } = default!; // e.g. "Waters"
  public string Email { get; set; } = default!;
  public string Username { get; set; } = default!; // e.g. "terencewaters"
  public string DisplayName { get; set; } = default!; // e.g. "Terence Waters"
  public string AuthorSlug => PartitionKey; // e.g. "terence-waters"
  public string? Location { get; set; } = default!; // e.g. "San Francisco, CA"
  public string? Bio { get; set; } = default!;
  public string? Website { get; set; } = default!; // e.g. "https://terencewaters.com"

  public string? TwitterHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? InstagramHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? LinkedInHandle { get; set; } = default!;
  public string? BlueskyHandle { get; set; } = default!; // e.g. "@terencewaters.bsky.social"
  public string? ProfileImageFileName { get; set; } = default!;
  public string? ProfileImageCdnUrl { get; set; } = default!; // e.g. "https://example.com/images/terence-waters.jpg"
  public string? ThumbnailCdnUrl { get; set; } = default!; // e.g. "https://example.com/images/terence-waters-thumbnail.jpg"
  public bool HasValidProfileImage { get; set; } = false; // Indicates if the profile image is valid and available
  public string? ImageContentType { get; set; } = default!; // e.g. "image/jpeg"
  public long? ImageSizeBytes { get; set; } // e.g. 204800 (200 KB)
  public int? ImageWidth { get; set; } // e.g. 800
  public int? ImageHeight { get; set; } // e.g. 600
  public string? ProfileImageBlobContainer { get; set; } = default!;

  /// <summary>
  /// Creates an AuthorEntity from an AuthorModel.
  /// </summary>
  /// <param name="model">The author model to convert.</param>
  /// <param name="partitionKey">The partition key to use for the entity.</param>
  /// <param name="rowKey">The row key to use for the entity. Defaults to "profile".</param>
  /// <returns>A new AuthorEntity instance.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any required field is null.</exception>
  /// <exception cref="ArgumentException">Thrown when validation fails for any field.</exception>
  public static AuthorEntity FromModel(AuthorModel model, string partitionKey, string rowKey = "profile")
  {
    // Validate all required fields are present
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(model.AuthorSlug);
    ArgumentNullException.ThrowIfNull(model.FirstName);
    ArgumentNullException.ThrowIfNull(model.LastName);
    ArgumentNullException.ThrowIfNull(model.Email);
    ArgumentNullException.ThrowIfNull(model.Username);
    ArgumentNullException.ThrowIfNull(model.DisplayName);

    return new AuthorEntity
    {
      PartitionKey = DataValidation.Required(DataValidation.SafeTrim(partitionKey), nameof(partitionKey)),
      RowKey = DataValidation.SafeTrim(rowKey) ?? "profile",
      FirstName = DataValidation.Required(DataValidation.SafeTrim(model.FirstName), nameof(model.FirstName)),
      LastName = DataValidation.Required(DataValidation.SafeTrim(model.LastName), nameof(model.LastName)),
      Email = DataValidation.Required(DataValidation.IsValidEmail(DataValidation.SafeTrim(model.Email), nameof(model.Email)), nameof(model.Email)),
      Username = DataValidation.Required(
        DataValidation.RequireMinLength(DataValidation.SafeTrim(model.Username), 5, nameof(model.Username)),
        nameof(model.Username)
      ),
      DisplayName = DataValidation.Required(DataValidation.SafeTrim(model.DisplayName), nameof(model.DisplayName)),
      Location = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.Location), 2, nameof(model.Location)) ?? null,
      Bio = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.Bio), 10, nameof(model.Bio)) ?? null,
      Website = DataValidation.NormalizeUrl(DataValidation.SafeTrim(model.Website)) ?? null,
      TwitterHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.TwitterHandle), 3, nameof(model.TwitterHandle)) ?? null,
      InstagramHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.InstagramHandle), 3, nameof(model.InstagramHandle)) ?? null,
      LinkedInHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.LinkedInHandle), 3, nameof(model.LinkedInHandle)) ?? null,
      BlueskyHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(model.BlueskyHandle), 3, nameof(model.BlueskyHandle)) ?? null,

      ProfileImageBlobContainer = DataValidation.SafeTrim(model.ProfileImageBlobContainer) ?? "authors-images",
      ProfileImageFileName = DataValidation.SafeTrim(model.ProfileImageFileName),
      ProfileImageCdnUrl = DataValidation.SafeTrim(model.ProfileImageCdnUrl) ?? "/images/default-profile.png",
      ThumbnailCdnUrl = DataValidation.SafeTrim(model.ThumbnailCdnUrl) ?? "/images/default-profile-thumbnail.png",

      HasValidProfileImage = !string.IsNullOrWhiteSpace(model.ProfileImageFileName) &&
                             !string.IsNullOrWhiteSpace(model.ProfileImageCdnUrl) &&
                             !string.IsNullOrWhiteSpace(model.ThumbnailCdnUrl),

      ImageContentType = DataValidation.SafeTrim(model.ImageContentType) ?? "image/jpeg",
      ImageSizeBytes = DataValidation.RequirePositiveLong(model.ImageSizeBytes, nameof(model.ImageSizeBytes)) ?? 0,
      ImageWidth = DataValidation.RequirePositiveInt(model.ImageWidth, nameof(model.ImageWidth)) ?? 0,
      ImageHeight = DataValidation.RequirePositiveInt(model.ImageHeight, nameof(model.ImageHeight)) ?? 0
    };
  }
}