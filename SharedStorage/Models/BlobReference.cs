namespace SharedStorage.Models;

/// <summary>
/// Represents a reference to a blob in Azure Blob Storage with a CDN URL
/// </summary>
public class BlobReference
{
  public string BlobName { get; }
  public string CdnUrl { get; }

  public BlobReference(string blobName, string cdnUrl)
  {
    BlobName = blobName ?? throw new ArgumentNullException(nameof(blobName));
    CdnUrl = cdnUrl ?? throw new ArgumentNullException(nameof(cdnUrl));
  }
}
