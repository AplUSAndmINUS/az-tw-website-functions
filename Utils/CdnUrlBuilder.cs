namespace Utils;

using Utils.Constants;

public static class CdnUrlBuilder
{
  // CDN endpoints for content types
  private const string CdnEndpointDocuments = "https://documents.terencewaters.com";
  private const string CdnEndpointImages = "https://images.terencewaters.com";
  private const string CdnEndpointMusic = "https://music.terencewaters.com";
  private const string CdnEndpointVideos = "https://videos.terencewaters.com";
  private const string CdnEndpointMedia = "https://media.terencewaters.com";

  // Mock Azure storage URL which point directly to Azure Blob Storage
  private const string MockCdnBlobStorageUrl = "https://aztwwebsitestorage.blob.core.windows.net";
  private const string MockCdnTableStorageUrl = "https://aztwwebsitestorage.table.core.windows.net";

  public static string ResolveCdnUrl(ContentSections section, AssetType? assetType, string blobName, string? paramsString = null, bool isMockStorage = false)
  {
    if (string.IsNullOrWhiteSpace(blobName))
      throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));
    if (blobName.Contains("mock"))
      throw new ArgumentException("Blob name cannot be a mock blob.", nameof(blobName));

    if (isMockStorage)
      return $"{MockCdnBlobStorageUrl}/{ContentNameResolver.GetBlobContainerName(section, assetType, true)}/{blobName}";

    string containerName = ContentNameResolver.GetBlobContainerName(section, assetType);

    // Build the CDN URL
    var cdnUrl = BuildCdnUrl(section, assetType, containerName, blobName);

    // Append query parameters if provided
    if (!string.IsNullOrWhiteSpace(paramsString))
      cdnUrl += $"?{paramsString.TrimStart('?')}";

    return cdnUrl;
  }

  private static string BuildCdnUrl(ContentSections section, AssetType? assetType, string containerName, string blobName)
  {
    return (section, assetType) switch
    {
      (ContentSections.Documents, _) => $"{CdnEndpointDocuments}/{containerName}/{blobName}",
      (_, AssetType.Images) => $"{CdnEndpointImages}/{containerName}/{blobName}",
      (_, AssetType.Video) => $"{CdnEndpointVideos}/{containerName}/{blobName}",
      (_, AssetType.Media) => $"{CdnEndpointMedia}/{containerName}/{blobName}",
      (ContentSections.Music, _) => $"{CdnEndpointMusic}/{containerName}/{blobName}",
      _ => throw new ArgumentException($"No CDN endpoint configured for section {section} with asset type {assetType}", nameof(section))
    };
  }
}