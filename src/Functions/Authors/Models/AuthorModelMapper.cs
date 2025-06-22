namespace Functions.Authors.Models;

using Utils.Validation;

public class AuthorModelMapper
{

  public static AuthorModel Map(AuthorEntity author, AuthorImagesMetadataEntity? image)
  {
    ArgumentNullException.ThrowIfNull(author);
    ArgumentNullException.ThrowIfNull(author.FirstName);
    ArgumentNullException.ThrowIfNull(author.LastName);
    ArgumentNullException.ThrowIfNull(author.Email);
    ArgumentNullException.ThrowIfNull(author.Username);

    return new AuthorModel
    {
      AuthorSlug = DataValidation.Required(DataValidation.SafeTrim(author.AuthorSlug), nameof(author.AuthorSlug)),
      FirstName = DataValidation.Required(DataValidation.SafeTrim(author.FirstName), nameof(author.FirstName)),
      LastName = DataValidation.Required(DataValidation.SafeTrim(author.LastName), nameof(author.LastName)),
      Email = DataValidation.Required(DataValidation.IsValidEmail(DataValidation.SafeTrim(author.Email), nameof(author.Email)), nameof(author.Email)),
      Username = DataValidation.Required(
        DataValidation.RequireMinLength(DataValidation.SafeTrim(author.Username), 5, nameof(author.Username)),
        nameof(author.Username)
      ),
      DisplayName = DataValidation.SafeTrim(author.DisplayName) ?? author.Username, // Fallback to username if display name is empty
      Location = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.Location), 2, nameof(author.Location)) ?? null,
      Bio = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.Bio), 10, nameof(author.Bio)) ?? null,
      Website = DataValidation.NormalizeUrl(author.Website),
      TwitterHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.TwitterHandle), 3, nameof(author.TwitterHandle)) ?? null,
      InstagramHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.InstagramHandle), 3, nameof(author.InstagramHandle)) ?? null,
      LinkedInHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.LinkedInHandle), 3, nameof(author.LinkedInHandle)) ?? null,
      BlueskyHandle = DataValidation.RequireMinLength(DataValidation.SafeTrim(author.BlueskyHandle), 3, nameof(author.BlueskyHandle)) ?? null,

      ProfileImageBlobContainer = DataValidation.SafeTrim(image?.ProfileImageBlobContainer) ?? "authors-images",
      ProfileImageFileName = DataValidation.SafeTrim(image?.ProfileImageFileName),
      ProfileImageCdnUrl = DataValidation.SafeTrim(image?.ProfileImageCdnUrl) ?? "/images/default-profile.png",
      ThumbnailCdnUrl = DataValidation.SafeTrim(image?.ThumbnailCdnUrl) ?? "/images/default-profile-thumbnail.png",

      HasValidProfileImage = image != null && !string.IsNullOrWhiteSpace(image.ProfileImageFileName) &&
                             !string.IsNullOrWhiteSpace(image.ProfileImageCdnUrl) &&
                             !string.IsNullOrWhiteSpace(image.ThumbnailCdnUrl),
      ImageContentType = DataValidation.SafeTrim(image?.ImageContentType),
      ImageSizeBytes = DataValidation.RequirePositiveLong(image?.ImageSizeBytes, nameof(image.ImageSizeBytes)),
      ImageWidth = DataValidation.RequirePositiveInt(image?.ImageWidth, nameof(image.ImageWidth)),
      ImageHeight = DataValidation.RequirePositiveInt(image?.ImageHeight, nameof(image.ImageHeight)),
    };
  }
}