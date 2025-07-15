namespace Utils;

using Utils.Constants;

public static class CdnUrlBuilder
{
  public static string ResolveCdnUrl(ContentSections section, AssetType? assetType, string blobName, string? paramsString = null, bool isMockStorage = false)
  {
    if (string.IsNullOrWhiteSpace(blobName))
      throw new ArgumentException("Blob name cannot be null or empty.", nameof(blobName));

    // Remove check for "mock" in blob name as it can be part of legitimate paths

    // Always get the appropriate container name based on mock flag
    string containerName = ContentNameResolver.GetBlobContainerName(section, assetType, isMockStorage);

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