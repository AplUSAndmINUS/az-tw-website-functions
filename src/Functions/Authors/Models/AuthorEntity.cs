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
  public string FullName => $"{FirstName} {LastName}".Trim();
  public string Email { get; set; } = default!;
  public string Username { get; set; } = default!; // e.g. "terencewaters"
  public string DisplayName { get; set; } = default!; // e.g. "Terence Waters"
  public string? Location { get; set; } = default!; // e.g. "San Francisco, CA"
  public string? Bio { get; set; } = default!;
  public string? Website { get; set; } = default!; // e.g. "https://terencewaters.com"

  public string? TwitterHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? InstagramHandle { get; set; } = default!; // e.g. "@terencewaters"
  public string? LinkedInHandle { get; set; } = default!;
  public string? BlueskyHandle { get; set; } = default!; // e.g. "@terencewaters.bsky.social"
  public string AuthorSlug => PartitionKey; // e.g. "terence-waters"
  public string? ProfileImageFileName { get; set; } = default!;
  public string? ProfileImageBlobContainer { get; set; } = default!;

  public AuthorEntity()
  {
    DisplayName = $"{FirstName} {LastName}".Trim();
  }

  public static AuthorEntity FromModel(AuthorModel model, string partitionKey, string rowKey)
  {
    return new AuthorEntity
    {
      PartitionKey = partitionKey,
      RowKey = rowKey,
      FirstName = DataValidation.Required(DataValidation.SafeTrim(model.FirstName), nameof(model.FirstName)),
      LastName = DataValidation.Required(DataValidation.SafeTrim(model.LastName), nameof(model.LastName)),
      Email = DataValidation.Required(DataValidation.SafeTrim(model.Email), nameof(model.Email)),
      Username = DataValidation.Required(DataValidation.SafeTrim(model.Username), nameof(model.Username)),
      DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
            ? model.Username
            : model.DisplayName,
      Location = DataValidation.SafeTrim(model.Location),
      Bio = DataValidation.SafeTrim(model.Bio),
      Website = DataValidation.SafeTrim(model.Website),
      TwitterHandle = DataValidation.SafeTrim(model.TwitterHandle),
      InstagramHandle = DataValidation.SafeTrim(model.InstagramHandle),
      LinkedInHandle = DataValidation.SafeTrim(model.LinkedInHandle),
      ProfileImageFileName = DataValidation.SafeTrim(model.ProfileImageUrl),
      ProfileImageBlobContainer = null // you can assign if it's known
    };
  }
}