using SharedStorage.Models;

namespace SharedStorage.Services.Media.Handlers;

public interface IMediaTypeHandler
{
  string SupportedType { get; }
  Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorSlug = null);
  Task<MediaEntity> GetAsync(string id);
  Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null);
  Task<bool> DeleteAsync(string id);
}

public class MediaHandler : IMediaTypeHandler
{
  public string SupportedType { get; private set; }

  public MediaHandler(string supportedType)
  {
    SupportedType = supportedType;
  }

  public Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorSlug = null)
  {
    // Implementation for uploading media
    throw new NotImplementedException();
  }

  public Task<MediaEntity> GetAsync(string id)
  {
    // Implementation for getting a specific media entity
    throw new NotImplementedException();
  }

  public Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
  {
    // Implementation for getting all media entities
    throw new NotImplementedException();
  }

  public Task<bool> DeleteAsync(string id)
  {
    // Implementation for deleting a media entity
    throw new NotImplementedException();
  }
}