namespace Utils;

public static class CdnUrlBuilder
{
  // CDN endpoints for content types
  private const string CdnEndpointDocuments = "https://documents.terencewaters.com";
  private const string CdnEndpointImages = "https://images.terencewaters.com";
  private const string CdnEndpointMusic = "https://music.terencewaters.com";
  private const string CdnEndpointVideos = "https://videos.terencewaters.com";

  // Blob container names
  private const string ContainerDocuments = "documents";
  private const string ContainerArtworkImages = "artwork-images";
  private const string ContainerBlogImages = "blog-images";
  private const string ContainerBooksImages = "books-images";
  private const string ContainerPortfolioImages = "portfolio-images";
  private const string ContainerLivestreamImages = "livestream-images";
  private const string ContainerLivestreamVideo = "livestream-video";
  private const string ContainerMusic = "music";
  private const string ContainerVideo = "video";

  public static string ResolveCdnUrl(string containerName, string blobName, string? paramsString = null)
  {
    if (string.IsNullOrWhiteSpace(containerName))
      throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
    if (string.IsNullOrWhiteSpace(blobName))
      throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));
    if (IsMockStorage.IsMockBlobName(blobName))
      throw new ArgumentException("Blob name cannot be a mock blob.", nameof(blobName));

    // Validate container name
    var validContainers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      ContainerDocuments,
      ContainerArtworkImages,
      ContainerBlogImages,
      ContainerBooksImages,
      ContainerPortfolioImages,
      ContainerLivestreamImages,
      ContainerLivestreamVideo,
      ContainerMusic,
      ContainerVideo
    };

    if (!validContainers.Contains(containerName, StringComparer.OrdinalIgnoreCase))
      throw new ArgumentException($"Unknown container name: {containerName}", nameof(containerName));

    // Build the CDN URL
    var cdnUrl = BuildCdnUrl(containerName, blobName);

    // Append query parameters if provided
    if (!string.IsNullOrWhiteSpace(paramsString))
    {
      cdnUrl += $"?{paramsString}";
    }

    return cdnUrl;
  }

  private static string BuildCdnUrl(string containerName, string blobName)
  {
    return containerName.ToLowerInvariant() switch
    {
      ContainerDocuments => $"{CdnEndpointDocuments}/{containerName}/{blobName}",
      ContainerArtworkImages or ContainerBlogImages or ContainerBooksImages or ContainerPortfolioImages or ContainerLivestreamImages => $"{CdnEndpointImages}/{containerName}/{blobName}",
      ContainerLivestreamVideo or ContainerVideo => $"{CdnEndpointVideos}/{containerName}/{blobName}",
      ContainerMusic => $"{CdnEndpointMusic}/{containerName}/{blobName}",
      _ => throw new ArgumentException($"Unknown container name: {containerName}", nameof(containerName)),
    };
  }
}