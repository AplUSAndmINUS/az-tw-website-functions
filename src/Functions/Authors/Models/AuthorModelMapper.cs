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
      Email = DataValidation.Required(DataValidation.SafeTrim(author.Email), nameof(author.Email)),
      Username = DataValidation.Required(DataValidation.SafeTrim(author.Username), nameof(author.Username)),
      DisplayName = DataValidation.SafeTrim(author.DisplayName) ?? author.Username, // Fallback to username if display name is empty
      Location = DataValidation.SafeTrim(author.Location),
      Bio = DataValidation.SafeTrim(author.Bio),
      Website = DataValidation.NormalizeUrl(author.Website),
      TwitterHandle = DataValidation.SafeTrim(author.TwitterHandle),
      InstagramHandle = DataValidation.SafeTrim(author.InstagramHandle),
      LinkedInHandle = DataValidation.SafeTrim(author.LinkedInHandle),
      BlueskyHandle = DataValidation.SafeTrim(author.BlueskyHandle),

      ProfileImageBlobContainer = DataValidation.SafeTrim(image?.ProfileImageBlobContainer) ?? "authors-images",
      ProfileImageFileName = DataValidation.SafeTrim(image?.ProfileImageFileName),
      ProfileImageCdnUrl = DataValidation.SafeTrim(image?.ProfileImageCdnUrl) ?? "/images/default-profile.png",
      ThumbnailCdnUrl = DataValidation.SafeTrim(image?.ThumbnailCdnUrl) ?? "/images/default-profile-thumbnail.png",

      ImageContentType = DataValidation.SafeTrim(image?.ImageContentType),
      ImageSizeBytes = DataValidation.RequirePositiveLong(image?.ImageSizeBytes, nameof(image.ImageSizeBytes)),
      ImageWidth = DataValidation.RequirePositiveInt(image?.ImageWidth, nameof(image.ImageWidth)),
      ImageHeight = DataValidation.RequirePositiveInt(image?.ImageHeight, nameof(image.ImageHeight)),
    };
  }
}