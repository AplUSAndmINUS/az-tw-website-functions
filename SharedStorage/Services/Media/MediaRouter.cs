namespace SharedStorage.Services.Media;
using SharedStorage.Services.Media.Handlers;
using SharedStorage.Models;

public class MediaRouter
{
  private readonly Dictionary<string, IMediaTypeHandler> _handlers;

  public MediaRouter(IEnumerable<IMediaTypeHandler> handlers)
  {
    _handlers = handlers.ToDictionary(h => h.SupportedType);
  }

  public Task<MediaEntity> HandleUploadAsync(string mediaType, Stream stream, ...)
  {
    if (!_handlers.TryGetValue(mediaType.ToLower(), out var handler))
    {
      throw new InvalidOperationException("Unsupported media type");
    }

    return handler.UploadAsync(stream, fileName, contentType, authorSlug);
  }
}