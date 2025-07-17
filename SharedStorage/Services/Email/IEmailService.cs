namespace SharedStorage.Services.Email;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body content</param>
    /// <param name="isHtml">Whether the body is HTML formatted</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);

    /// <summary>
    /// Formats contact form data into a professional email
    /// </summary>
    /// <param name="name">Sender's name</param>
    /// <param name="email">Sender's email</param>
    /// <param name="message">Message content</param>
    /// <param name="submittedAt">Submission timestamp</param>
    /// <param name="userAgent">User agent information</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Formatted email body</returns>
    string FormatContactEmail(string name, string email, string message, DateTime submittedAt, string userAgent = "", string ipAddress = "");
}