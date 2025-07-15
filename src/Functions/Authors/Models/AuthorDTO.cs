namespace Functions.Authors.Models;

public class AuthorDTO
{
    public string AuthorSlug { get; set; } = default!; // e.g. "terence-waters"
    public string DisplayName { get; set; } = default!; // e.g. "terencewaters"
    public string FirstName { get; set; } = default!; // e.g. "Terence"
    public string LastName { get; set; } = default!; // e.g. "Waters"
    public string Email { get; set; } = default!; // e.g. "terence.waters@example.com" - Now included as required
    public string Username { get; set; } = default!; // e.g. "terencewaters"

    // Rest of the properties remain the same
    public string? Location { get; set; } // e.g. "San Francisco, CA"
    public string? Bio { get; set; } // e.g. "Software Engineer with a passion for open source."
    public string? Website { get; set; } // e.g. "https://terencewaters.com"
    public string? TwitterHandle { get; set; } // e.g. "@terencewaters"
    public string? InstagramHandle { get; set; } // e.g. "@terencewaters"
    public string? LinkedInHandle { get; set; } // e.g. "https://www.linkedin.com/in/terencewaters"
    public string? BlueskyHandle { get; set; } // e.g. "@terencewaters.bsky.social"

    // Media properties
    public bool HasValidProfileImage { get; set; } = false; // Indicates if the profile image is valid and available
    public string? ProfileImageId { get; set; } // The MediaId of the profile image
    public string? ProfileImageFileName { get; set; } // e.g. "terence-waters.jpg"
    public string? ProfileImageCdnUrl { get; set; } // e.g. "https://example.com/images/terence-waters.jpg"
    public string? ThumbnailCdnUrl { get; set; } // e.g. "https://example.com/images/terence-waters-thumbnail.jpg"
    public string MediaReferencesJson { get; set; } = "[]"; // JSON array of MediaIds
}

public static class AuthorDTOMapper
{
    public static AuthorDTO ToDTO(AuthorEntity author, AuthorImagesMetadataEntity? image = null)
    {
        return new AuthorDTO
        {
            AuthorSlug = author.AuthorSlug,
            DisplayName = author.DisplayName,
            FirstName = author.FirstName,
            LastName = author.LastName,
            Email = author.Email,
            Username = author.Username,
            Location = author.Location,
            Bio = author.Bio,
            Website = author.Website,
            TwitterHandle = author.TwitterHandle,
            InstagramHandle = author.InstagramHandle,
            LinkedInHandle = author.LinkedInHandle,
            BlueskyHandle = author.BlueskyHandle,

            // Media properties
            HasValidProfileImage = author.HasValidProfileImage,
            ProfileImageId = author.ProfileImageId,
            ProfileImageFileName = author.ProfileImageFileName ?? image?.ProfileImageFileName,
            ProfileImageCdnUrl = author.ProfileImageCdnUrl ?? image?.ProfileImageCdnUrl,
            ThumbnailCdnUrl = author.ThumbnailCdnUrl ?? image?.ThumbnailCdnUrl,
            MediaReferencesJson = author.MediaReferencesJson
        };
    }
}