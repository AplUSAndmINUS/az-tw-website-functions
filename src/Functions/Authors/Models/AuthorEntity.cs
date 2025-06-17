using Azure.Data.Tables;
using Azure;

namespace az_tw_website_functions.src.Functions.Authors.Models;

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
  public string AuthorSlug => PartitionKey; // e.g. "terence-waters"
  public string? ProfileImageFileName { get; set; } = default!;
  public string? ProfileImageBlobContainer { get; set; } = default!;

  public AuthorEntity()
  {
    DisplayName = $"{FirstName} {LastName}".Trim();
  }
}