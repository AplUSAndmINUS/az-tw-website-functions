namespace SharedStorage.Models;

/// <summary>
/// Represents a reference to a blob in Azure Blob Storage with a CDN URL
/// </summary>
public class BlobReference
{
  public string BlobName { get; }
  public string CdnUrl { get; }
  public string? ContentId { get; }
  public string? RelatedContentType { get; }

  public BlobReference(string blobName, string cdnUrl, string? contentId = null, string? relatedContentType = null)
  {
    BlobName = blobName ?? throw new ArgumentNullException(nameof(blobName));
    CdnUrl = cdnUrl ?? throw new ArgumentNullException(nameof(cdnUrl));
    ContentId = contentId;
    RelatedContentType = relatedContentType;
  }
}
