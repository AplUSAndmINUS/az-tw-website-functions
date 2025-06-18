namespace Utils.Constants;

public enum ApiUrls
{
  // Base URL for the API
  MockBaseDevUrl = "https://mock-dev-api.terencewaters.com",
  MockBaseTestUrl = "https://mock-tst-api.terencewaters.com",
  BaseUrl = "https://api.terencewaters.com",

  // CDN endpoints for content types
  CdnEndpointDocuments = "https://documents.terencewaters.com",
  CdnEndpointImages = "https://images.terencewaters.com",
  CdnEndpointMusic = "https://music.terencewaters.com",
  CdnEndpointVideos = "https://videos.terencewaters.com",
  CdnEndpointMedia = "https://media.terencewaters.com",

  // Mock Azure storage URL which point directly to Azure Blob Storage
  MockCdnBlobStorageUrl = "https://aztwwebsitestorage.blob.core.windows.net",
  MockCdnTableStorageUrl = "https://aztwwebsitestorage.table.core.windows.net",
}