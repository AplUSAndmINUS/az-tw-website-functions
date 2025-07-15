namespace SharedStorage.Models;

/// <summary>
/// Represents references to media stored in Azure Blob Storage with CDN URLs
/// </summary>
public class MediaReference
{
  public string BlobName { get; }
  public string ThumbnailBlobName { get; }
  public string CdnUrl { get; }
  public string ThumbnailCdnUrl { get; }
  public string? ContentId { get; }
  public string? RelatedContentType { get; }

  public MediaReference(string blobName, string thumbnailBlobName, string cdnUrl, string thumbnailCdnUrl,
                       string? contentId = null, string? relatedContentType = null)
  {
    BlobName = blobName ?? throw new ArgumentNullException(nameof(blobName));
    ThumbnailBlobName = thumbnailBlobName ?? throw new ArgumentNullException(nameof(thumbnailBlobName));
    CdnUrl = cdnUrl ?? throw new ArgumentNullException(nameof(cdnUrl));
    ThumbnailCdnUrl = thumbnailCdnUrl ?? throw new ArgumentNullException(nameof(thumbnailCdnUrl));
    ContentId = contentId;
    RelatedContentType = relatedContentType;
  }
}
