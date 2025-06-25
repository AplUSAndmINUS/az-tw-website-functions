namespace Functions.BlogPosts.Models;

public class BlogPostDTO
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string PartitionKey { get; set; } = string.Empty;
  public string RowKey { get; set; } = string.Empty;
  public DateTimeOffset? Timestamp { get; set; }
  public string Title { get; set; } = string.Empty;
  public string AuthorSlug { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public string Content { get; set; } = string.Empty;
  public string Slug { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string Status { get; set; } = "Draft";
  public string? MediaUrl { get; set; }
  public string? MediaDescription { get; set; }
  public string? ImageUrl { get; set; }
  public string? ImageDescription { get; set; }
  public DateTime PublishDate { get; set; }
  public DateTime LastModified { get; set; }
  public string[] TagsList { get; set; } = [];
}