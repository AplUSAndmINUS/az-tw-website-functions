namespace Utils.Constants;

public static class ApiUrls
{
  // Base URL for the API
  public const string MockBaseDevUrl = "https://mock-dev-api.terencewaters.com";
  public const string MockBaseTestUrl = "https://mock-tst-api.terencewaters.com";
  public const string BaseUrl = "https://api.terencewaters.com";

  // CDN endpoints for blob content types using Azure CDN Edge URL
  // Will be updated to Front Door URLs in the future
  public const string CdnEndpointBase = "https://twmedia-cdn.azureedge.net";
  public const string MockCdnEndpointBase = "https://twmedia-cdn.azureedge.net";
  public const string CdnEndpointDocuments = "https://twmedia-cdn.azureedge.net";
  public const string CdnEndpointImages = "https://twmedia-cdn.azureedge.net";
  public const string CdnEndpointMusic = "https://twmedia-cdn.azureedge.net";
  public const string CdnEndpointVideos = "https://twmedia-cdn.azureedge.net";
  public const string CdnEndpointMedia = "https://twmedia-cdn.azureedge.net";

  // Mock Azure storage URL which point directly to Azure Blob Storage

  public const string CdnBlobStorageUrl = "https://aztwwebsitestorage.blob.core.windows.net";
  public const string MockCdnBlobStorageUrl = "https://aztwwebsitestorage.blob.core.windows.net";
  public const string CdnTableStorageUrl = "https://aztwwebsitestorage.table.core.windows.net";
  public const string MockCdnTableStorageUrl = "https://aztwwebsitestorage.table.core.windows.net";
}