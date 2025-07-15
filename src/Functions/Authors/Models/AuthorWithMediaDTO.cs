using SharedStorage.Models;
using System.Collections.Generic;
using Functions.Authors.Models;

namespace Functions.Authors.Models;

/// <summary>
/// DTO for combining an author with their associated media items
/// </summary>
public class AuthorWithMediaDTO
{
  // The author model
  public AuthorModel Author { get; set; } = new AuthorModel();

  // All media items associated with this author
  public List<MediaItemModel> MediaItems { get; set; } = new List<MediaItemModel>();

  // Convenience properties for featured media
  public MediaItemModel? ProfileImage { get; set; }
  public MediaItemModel? BackgroundImage { get; set; }

  // JSON serialized list of media references
  public string MediaReferencesJson => Author.MediaReferencesJson;

  // Constructor with minimal setup
  public AuthorWithMediaDTO()
  {
    Author = new AuthorModel();
    MediaItems = new List<MediaItemModel>();
  }

  // Constructor with author and media items
  public AuthorWithMediaDTO(AuthorModel author, IEnumerable<MediaItemModel> mediaItems)
  {
    Author = author;
    MediaItems = new List<MediaItemModel>(mediaItems);
    InitializeFeaturedMedia();
  }

  // Helper method to initialize featured media from the full list
  private void InitializeFeaturedMedia()
  {
    foreach (var media in MediaItems)
    {
      if (media.MediaType?.ToLowerInvariant() == "image")
      {
        if (media.Purpose?.Contains("profile") == true)
        {
          ProfileImage = media;
        }
        else if (media.Purpose?.Contains("background") == true || media.Purpose?.Contains("cover") == true)
        {
          BackgroundImage = media;
        }
      }
    }
  }

  // Compatibility properties with the original AuthorModel
  // These will allow seamless transition for existing code

  public string? AuthorSlug => Author.AuthorSlug;
  public string? FirstName => Author.FirstName;
  public string? LastName => Author.LastName;
  public string? Email => Author.Email;
  public string? Username => Author.Username;
  public string? DisplayName => Author.DisplayName;
  public string? Location => Author.Location;
  public string? Bio => Author?.Bio;
  public string? Website => Author?.Website;

  // Social media handles
  public string? TwitterHandle => Author?.TwitterHandle;
  public string? InstagramHandle => Author?.InstagramHandle;
  public string? LinkedInHandle => Author?.LinkedInHandle;
  public string? BlueskyHandle => Author?.BlueskyHandle;

  // Get media URLs from the MediaItems collection for backward compatibility
  public string ProfileImageCdnUrl => ProfileImage?.Url ?? Author?.ProfileImageCdnUrl ?? "/images/default-profile.png";
  public string ThumbnailCdnUrl => ProfileImage?.ThumbnailUrl ?? Author?.ThumbnailCdnUrl ?? "/images/default-profile-thumbnail.png";
}
