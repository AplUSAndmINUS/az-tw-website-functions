using Azure.Data.Tables;
using Azure;

namespace az_tw_website_functions.src.Functions.Authors.Models;
public class AuthorImagesMetadataEntity : ITableEntity
{
  public string PartitionKey { get; set; } = default!; 
  public string RowKey { get; set; } = default!; // image file name or blob URI-safe ID
  public DateTimeOffset? Timestamp { get; set; }
  public ETag ETag { get; set; }

  public string AuthorSlug => PartitionKey;
  public string FileName { get; set; } = default!;
  public string BlobContainer { get; set; } = default!; // e.g. "authors-images"

  public string ContentType { get; set; } = default!;
  public long SizeInBytes { get; set; }

  public int Width { get; set; }
  public int Height { get; set; }

  // Used in Production ONLY
  public string? CdnUrl { get; set; } = default!;
  public string? ThumbnailCdnUrl { get; set; } = default!;
}
