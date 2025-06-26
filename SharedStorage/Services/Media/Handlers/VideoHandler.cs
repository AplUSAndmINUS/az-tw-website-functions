using SharedStorage.Models;

namespace SharedStorage.Services.Media.Handlers;

public class VideoHandler : MediaHandler, IMediaTypeHandler
{
    public string SupportedType => "video";

    public VideoHandler() : base("video")
    {
    }

    public override Task<MediaEntity> UploadAsync(Stream stream, string fileName, string contentType, string? authorSlug = null)
    {
        // Implementation for uploading an image
        throw new NotImplementedException();
    }

    public override Task<MediaEntity> GetAsync(string id)
    {
        // Implementation for getting a specific image entity
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<MediaEntity>> GetAllAsync(string? authorSlug = null, int? limit = null)
    {
        // Implementation for getting all image entities
        throw new NotImplementedException();
    }

    public override Task<bool> DeleteAsync(string id)
    {
        // Implementation for deleting an image entity
        throw new NotImplementedException();
    }
}