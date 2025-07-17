namespace Functions.ContactMe.Models;

/// <summary>
/// Data Transfer Object for contact form submissions
/// </summary>
public class ContactMeDTO
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}