namespace Utils;

using Utils.Constants;

public static class CdnUrlBuilder
{
  public static string ResolveCdnUrl(ContentSections section, AssetType? assetType, string blobName, string? paramsString = null, bool isMockStorage = false)
  {
    if (string.IsNullOrWhiteSpace(blobName))
      throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));
    if (blobName.Contains("mock"))
      throw new ArgumentException("Blob name cannot be a mock blob.", nameof(blobName));

    if (isMockStorage)
      return $"{ApiUrls.MockCdnBlobStorageUrl}/{ContentNameResolver.GetBlobContainerName(section, assetType, true)}/{blobName}";

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
      (ContentSections.Documents, _) => $"{ApiUrls.CdnEndpointDocuments}/{containerName}/{blobName}",
      (_, AssetType.Images) => $"{ApiUrls.CdnEndpointImages}/{containerName}/{blobName}",
      (_, AssetType.Video) => $"{ApiUrls.CdnEndpointVideos}/{containerName}/{blobName}",
      (_, AssetType.Media) => $"{ApiUrls.CdnEndpointMedia}/{containerName}/{blobName}",
      (ContentSections.Music, _) => $"{ApiUrls.CdnEndpointMusic}/{containerName}/{blobName}",
      _ => throw new ArgumentException($"No CDN endpoint configured for section {section} with asset type {assetType}", nameof(section))
    };
  }
}