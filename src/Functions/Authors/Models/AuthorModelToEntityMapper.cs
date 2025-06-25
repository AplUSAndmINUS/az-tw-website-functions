namespace Functions.Authors.Models;

using Utils.Validation;

public static class AuthorModelToEntityMapper
{

  public static AuthorEntity Map(AuthorModel model, string partitionKey, string rowKey = "profile")
  {
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(model.FirstName);
    ArgumentNullException.ThrowIfNull(model.LastName);
    ArgumentNullException.ThrowIfNull(model.Email);
    ArgumentNullException.ThrowIfNull(model.Username);

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
      DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Username : model.DisplayName,
      Location = string.IsNullOrWhiteSpace(model.Location) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.Location), 2, nameof(model.Location)),
      Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.Bio), 10, nameof(model.Bio)),
      Website = DataValidation.NormalizeUrl(DataValidation.SafeTrim(model.Website)) ?? null,
      TwitterHandle = string.IsNullOrWhiteSpace(model.TwitterHandle) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.TwitterHandle), 3, nameof(model.TwitterHandle)),
      InstagramHandle = string.IsNullOrWhiteSpace(model.InstagramHandle) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.InstagramHandle), 3, nameof(model.InstagramHandle)),
      LinkedInHandle = string.IsNullOrWhiteSpace(model.LinkedInHandle) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.LinkedInHandle), 3, nameof(model.LinkedInHandle)),
      BlueskyHandle = string.IsNullOrWhiteSpace(model.BlueskyHandle) ? null : DataValidation.RequireMinLength(DataValidation.SafeTrim(model.BlueskyHandle), 3, nameof(model.BlueskyHandle)),

      ProfileImageBlobContainer = DataValidation.SafeTrim(model.ProfileImageBlobContainer) ?? "authors-images",
      ProfileImageFileName = DataValidation.SafeTrim(model.ProfileImageFileName),
      ProfileImageCdnUrl = DataValidation.SafeTrim(model.ProfileImageCdnUrl) ?? "/images/default-profile.png",
      ThumbnailCdnUrl = DataValidation.SafeTrim(model.ThumbnailCdnUrl) ?? "/images/default-profile-thumbnail.png",
    };
  }
}