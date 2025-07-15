using SharedStorage.Models;

namespace SharedStorage.Services.Media.Handlers;

public interface IMediaTypeHandler
{
  string SupportedType { get; }
  Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorSlug = null, string? contentId = null, string? relatedContentType = null);
  Task<MediaEntity> GetAsync(string id);
  Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null);
  Task<bool> DeleteAsync(string id);
}

public abstract class MediaHandler : IMediaTypeHandler
{
  public virtual string SupportedType { get; protected set; }

  protected MediaHandler(string supportedType)
  {
    SupportedType = supportedType;
  }

  public virtual Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorSlug = null, string? contentId = null, string? relatedContentType = null)
  {
    // Implementation for uploading media
    throw new NotImplementedException();
  }

  public virtual Task<MediaEntity> GetAsync(string id)
  {
    // Implementation for getting a specific media entity
    throw new NotImplementedException();
  }

  public virtual Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
  {
    // Implementation for getting all media entities
    throw new NotImplementedException();
  }

  public virtual Task<bool> DeleteAsync(string id)
  {
    // Implementation for deleting a media entity
    throw new NotImplementedException();
  }
}