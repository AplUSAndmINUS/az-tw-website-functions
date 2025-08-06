using System;
using System.IO;
using System.Collections.Generic;

namespace SharedStorage.Services.BaseServices
{
  public class MediaReference
  {
    public string BlobUrl { get; set; }
    public string CdnUrl { get; set; }
    public string ThumbnailBlobUrl { get; set; }
    public string ThumbnailCdnUrl { get; set; }
  }

  public class BlobItem
  {
    public string Name { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public DateTimeOffset? LastModified { get; set; }
  }

  public class BlobStorageResult
  {
    public List<BlobItem> Blobs { get; set; } = new List<BlobItem>();
    public string ContinuationToken { get; set; }
  }

  public class TableStorageResult<T> where T : class
  {
    public List<T> Entities { get; set; } = new List<T>();
    public string ContinuationToken { get; set; }
  }
}
