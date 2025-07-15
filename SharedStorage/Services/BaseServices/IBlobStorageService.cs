using Azure.Storage.Blobs;

namespace SharedStorage.Services.BaseServices;

public record BlobPageResult(
    IEnumerable<BlobClient> Blobs,
    string? ContinuationToken,
    int TotalCount,
    bool HasMore
);

public record BlobDownloadResult
{
    public Stream? Content { get; }
    public long ContentLength { get; }
    public string? ContentType { get; }
    public string? ContentDisposition { get; }

    public BlobDownloadResult(Stream? content, long contentLength, string? contentType = null, string? contentDisposition = null)
    {
        Content = content;
        ContentLength = contentLength;
        ContentType = contentType; // Can be null if not provided
        ContentDisposition = contentDisposition; // Can be null if not provided
    }
}

public record BlobReference(string FileName, string CdnUrl, DateTimeOffset? LastModified = null, long? Size = null, string? ContentId = null, string? RelatedContentType = null)
{
    public BlobReference(string fileName, string cdnUrl)
        : this(fileName, cdnUrl, null, null, null, null) { }

    public BlobReference(string fileName, string cdnUrl, string? contentId, string? relatedContentType)
        : this(fileName, cdnUrl, null, null, contentId, relatedContentType) { }
}

public record MediaReference(string OriginalBlobName, string ThumbnailBlobName, string CdnUrl, string ThumbnailCdnUrl, string? ContentId = null, string? RelatedContentType = null);

public interface IBlobStorageService
{
    BlobContainerClient GetBlobContainerClient(string containerName);
    Task<BlobClient> GetBlobClientAsync(string containerName, string blobName);
    Task<BlobPageResult> GetBlobsAsync(
        string containerName,
        string? prefix = null,
        int pageSize = 25,
        string? continuationToken = null
    );

    Task<IList<BlobReference>> GetBlobReferencesAsync(
        string containerName,
        string? prefix = null,
        int pageSize = 25,
        string? continuationToken = null
    );

    Task<BlobReference> GetBlobReferenceAsync(string containerName, string blobName);

    Task<BlobDownloadResult> DownloadBlobAsync(string containerName, string blobName);
    Task<MediaReference> UploadBlobAsync(string containerName, string blobName, Stream content, string? contentId = null, string? relatedContentType = null);
    Task DeleteBlobAsync(string containerName, string blobName);
}