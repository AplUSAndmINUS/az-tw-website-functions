namespace Functions.ContactMe.Models;

/// <summary>
/// Data Transfer Object for contact form submissions
/// </summary>
public class ContactMeDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Subject { get; set; } = string.Empty; // Optional subject field
    public string? Phone { get; set; } = null;          // Optional phone field 
    public string? Company { get; set; } = null;        // Optional company field
    public string? Website { get; set; } = null;        // Optional website field
}