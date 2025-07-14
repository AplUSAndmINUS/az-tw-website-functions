using SharedStorage.Models;
using Functions.Shared.Models;
using System.Collections.Generic;

namespace Functions.PortfolioPiece.Models;

/// <summary>
/// DTO for combining a portfolio piece with its associated media items
/// </summary>
public class PortfolioPieceWithMediaDTO : BaseContentWithMediaDTO<PortfolioPieceModel>
{
  // Constructor with minimal setup
  public PortfolioPieceWithMediaDTO()
  {
    Content = new PortfolioPieceModel();
    MediaItems = new List<MediaItemModel>();
  }

  // Constructor with post and media items
  public PortfolioPieceWithMediaDTO(PortfolioPieceModel post, IEnumerable<MediaItemModel> mediaItems)
  {
    Content = post;
    MediaItems = new List<MediaItemModel>(mediaItems);
    InitializeFeaturedMedia();
  }

  // Compatibility property for backward compatibility (will be removed in future versions)
  public PortfolioPieceModel Post
  {
    get => Content;
    set => Content = value;
  }
}