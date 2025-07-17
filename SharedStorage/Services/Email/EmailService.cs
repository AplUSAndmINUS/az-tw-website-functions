using System.Net;
using System.Net.Mail;
using System.Text;
using Utils;

namespace SharedStorage.Services.Email;

/// <summary>
/// Email service implementation for sending formatted emails
/// </summary>
public class EmailService : IEmailService
{
    private readonly IAppInsightsLogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _toEmail;

    public EmailService(IAppInsightsLogger<EmailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get SMTP configuration from environment variables
        _smtpHost = System.Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        _smtpPort = int.Parse(System.Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
        _smtpUsername = System.Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? throw new InvalidOperationException("SMTP_USERNAME environment variable is required");
        _smtpPassword = System.Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? throw new InvalidOperationException("SMTP_PASSWORD environment variable is required");
        _fromEmail = System.Environment.GetEnvironmentVariable("FROM_EMAIL") ?? _smtpUsername;
        _fromName = System.Environment.GetEnvironmentVariable("FROM_NAME") ?? "TerenceWaters.com";
        _toEmail = System.Environment.GetEnvironmentVariable("TO_EMAIL") ?? _smtpUsername;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        try
        {
            _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send email to {To}", ex, to);
            throw;
        }
    }

    public string FormatContactEmail(string name, string email, string message, DateTime submittedAt, string userAgent = "", string ipAddress = "")
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine("CONTACT FORM SUBMISSION");
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine();
        
        sb.AppendLine($"From: {name}");
        sb.AppendLine($"Email: {email}");
        sb.AppendLine($"Submitted: {submittedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        sb.AppendLine("MESSAGE:");
        sb.AppendLine("-".PadRight(60, '-'));
        sb.AppendLine(message);
        sb.AppendLine("-".PadRight(60, '-'));
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            sb.AppendLine("TECHNICAL DETAILS:");
            sb.AppendLine($"User Agent: {userAgent}");
        }
        
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            sb.AppendLine($"IP Address: {ipAddress}");
        }
        
        sb.AppendLine();
        sb.AppendLine("=".PadRight(60, '='));
        sb.AppendLine("End of submission");
        sb.AppendLine("=".PadRight(60, '='));
        
        return sb.ToString();
    }
}