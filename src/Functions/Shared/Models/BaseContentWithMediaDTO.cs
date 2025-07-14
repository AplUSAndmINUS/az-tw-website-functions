using System.Collections.Generic;
using SharedStorage.Models;

namespace Functions.Shared.Models;

/// <summary>
/// Base class for DTOs that combine content with media items
/// Can be extended by BlogPostWithMediaDTO, PortfolioPostWithMediaDTO, AuthorWithMediaDTO, etc.
/// </summary>
public abstract class BaseContentWithMediaDTO<T>
{
  // The primary content object (BlogPost, PortfolioPiece, Author, etc.)
  public T Content { get; set; } = default!;

  // All media items associated with this content
  public List<MediaItemModel> MediaItems { get; set; } = new List<MediaItemModel>();

  // Optional featured/primary media for convenience
  public MediaItemModel? FeaturedImage { get; set; }
  public MediaItemModel? FeaturedVideo { get; set; }
  public MediaItemModel? FeaturedAudio { get; set; }

  // Helper method to set the featured media based on the media type and purpose
  protected void SetFeaturedMedia(MediaItemModel media)
  {
    if (media == null) return;

    switch (media.MediaType.ToLowerInvariant())
    {
      case "image" when media.Purpose.Contains("featured") || media.Purpose.Contains("cover"):
        FeaturedImage = media;
        break;
      case "video" when media.Purpose.Contains("featured") || media.Purpose.Contains("intro"):
        FeaturedVideo = media;
        break;
      case "audio" when media.Purpose.Contains("featured"):
        FeaturedAudio = media;
        break;
    }
  }

  // Helper method to initialize featured media from the full list
  protected void InitializeFeaturedMedia()
  {
    foreach (var media in MediaItems)
    {
      SetFeaturedMedia(media);
    }
  }
}
