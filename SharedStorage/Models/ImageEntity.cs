namespace SharedStorage.Models;

public class ImageEntity : MediaEntity
{
    public string ImageType { get; set; } = "image"; // Default type for image entities
    public string ImgPurpose { get; set; } = "coverImage"; // Default purpose for images
    public string Resolution { get; set; } = "96"; // Default resolution for images

    public ImageEntity()
  {
    MediaType = ImageType;
    PartitionKey = AuthorId;
    RowKey = Id;
  }
}