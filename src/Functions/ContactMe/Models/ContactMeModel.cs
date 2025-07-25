namespace Functions.ContactMe.Models;

/// <summary>
/// Model representing a contact form submission
/// </summary>
public class ContactMeModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Subject { get; set; } = string.Empty; // Optional subject field
    public string? Phone { get; set; } = null;         // Optional phone field
    public string? Company { get; set; } = null;       // Optional company field
    public string? Website { get; set; } = null;       // Optional website field
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string UserAgent { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}