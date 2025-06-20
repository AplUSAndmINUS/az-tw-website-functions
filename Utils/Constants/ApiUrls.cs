namespace Utils.Constants;

public static class ApiUrls
{
  // Base URL for the API
  public const string MockBaseDevUrl = "https://mock-dev-api.terencewaters.com";
  public const string MockBaseTestUrl = "https://mock-tst-api.terencewaters.com";
  public const string BaseUrl = "https://api.terencewaters.com";

  // CDN endpoints for blob content types
  public const string CdnEndpointDocuments = "https://documents.terencewaters.com";
  public const string CdnEndpointImages = "https://images.terencewaters.com";
  public const string CdnEndpointMusic = "https://music.terencewaters.com";
  public const string CdnEndpointVideos = "https://videos.terencewaters.com";
  public const string CdnEndpointMedia = "https://media.terencewaters.com";

  // Mock Azure storage URL which point directly to Azure Blob Storage
  public const string MockCdnBlobStorageUrl = "https://aztwwebsitestorage.blob.core.windows.net";
  public const string MockCdnTableStorageUrl = "https://aztwwebsitestorage.table.core.windows.net";
}