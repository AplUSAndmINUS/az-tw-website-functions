using Azure.Data.Tables;
using Azure;

namespace Functions.Authors.Models;

public class AuthorImagesMetadataEntity : ITableEntity
{
  public string PartitionKey { get; set; } = default!;
  public string RowKey { get; set; } = default!; // image file name or blob URI-safe ID
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string AuthorSlug => PartitionKey;
  public string? ProfileImageCdnUrl { get; set; } = default!;
  public string? ThumbnailCdnUrl { get; set; } = default!;
  public string ImageContentType { get; set; } = default!;
  public long ImageSizeBytes { get; set; }
  public string ProfileImageFileName { get; set; } = default!;
  public string? ProfileImageBlobContainer { get; set; } = default!; // e.g. "authors-images"
  public int ImageWidth { get; set; }
  public int ImageHeight { get; set; }
}
