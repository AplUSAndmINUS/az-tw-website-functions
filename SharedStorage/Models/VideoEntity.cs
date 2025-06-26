namespace SharedStorage.Models;

public class VideoEntity : MediaEntity
{
    public string VideoType { get; set; } = "video"; // Default type for video entities
    public string VidPurpose { get; set; } = "introVideo"; // Default purpose for videos
    public string Resolution { get; set; } = "1080p"; // Default resolution for videos

    public VideoEntity()
  {
    MediaType = VideoType;
    PartitionKey = AuthorId;
    RowKey = Id;
  }
}