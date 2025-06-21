// using Azure;
// using Azure.Data.Tables;

// namespace az_tw_website_functions.BlogPosts.Models;

// public class BlogPostEntity : ITableEntity
// {
//     public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique identifier for the blog post
//     public string PartitionKey { get; set; } // Initialize in the constructor
//     public string RowKey { get; set; } = string.Empty; // Unique RowKey based on date and time
//     public DateTimeOffset? Timestamp { get; set; }
//     public string Title { get; set; } = string.Empty;
//     public string AuthorId { get; set; } = string.Empty;
//     public string Description { get; set; } = string.Empty;
//     public string Content { get; set; } = string.Empty; // Main content of the blog post
//     public string MediaUrl { get; set; } = string.Empty; // Reference to Blob Storage for media files
//     public string? MediaDescription { get; set; } = string.Empty; // Description of the media
//     public string ImageUrl { get; set; } = string.Empty; // Reference to Blob Storage
//     public string? ImageDescription { get; set; } = string.Empty; // Description of the image
//     public DateTime PublishDate { get; set; } = DateTime.UtcNow;
//     public DateTime LastModified { get; set; } = DateTime.UtcNow;
//     public string Category { get; set; } = string.Empty; // Category of the blog
//     public string[] TagsList { get; set; } = []; // Array of tags
//     public string Slug { get; set; } = string.Empty; // URL-friendly version of the title
//     public string Status { get; set; } = "Draft"; // Draft, Published, Archived
//     public bool IsPublished => Status == "Published";
//     public ETag ETag { get; set; }

//   public BlogPostEntity()
//   {
//     PartitionKey = PublishDate.ToString("yyyy-MM"); // Initialize PartitionKey in the constructor to month/year grouping
//     RowKey = PublishDate.ToString("yyyyMMddHHmmss") + Id; // Unique RowKey based on date and time
//   }
// }