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

        try
        {
            _logger.LogInformation("Initializing EmailService with environment configuration...");
            
            // Get SMTP configuration from environment variables
            _smtpHost = System.Environment.GetEnvironmentVariable("SMTP_SERVER") ??
                       System.Environment.GetEnvironmentVariable("SMTP_HOST") ??
                       "smtp.gmail.com";
            
            _logger.LogInformation("Using SMTP host: {SmtpHost} (detected from environment or default)", _smtpHost);

            _smtpPort = int.TryParse(System.Environment.GetEnvironmentVariable("SMTP_PORT"), out int port) ? port : 587;

            // Check for required environment variables
            var usernameEnv = System.Environment.GetEnvironmentVariable("SMTP_USERNAME");
            if (string.IsNullOrEmpty(usernameEnv))
            {
                _logger.LogWarning("SMTP_USERNAME environment variable is missing. Email functionality will be disabled.");
                throw new InvalidOperationException("SMTP_USERNAME environment variable is required");
            }
            _smtpUsername = usernameEnv; // Now we know it's not null

            var passwordEnv = System.Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            if (string.IsNullOrEmpty(passwordEnv))
            {
                _logger.LogWarning("SMTP_PASSWORD environment variable is missing. Email functionality will be disabled.");
                throw new InvalidOperationException("SMTP_PASSWORD environment variable is required");
            }
            _smtpPassword = passwordEnv; // Now we know it's not null

            _fromEmail = System.Environment.GetEnvironmentVariable("FROM_EMAIL") ?? _smtpUsername;
            _fromName = System.Environment.GetEnvironmentVariable("FROM_NAME") ?? "TerenceWaters.com";
            _toEmail = System.Environment.GetEnvironmentVariable("TO_EMAIL") ?? _smtpUsername;

            _logger.LogInformation("Email service initialized successfully with SMTP server: {SmtpHost}:{SmtpPort}, Username: {Username}",
                _smtpHost, _smtpPort, _smtpUsername);
            
            _logger.LogInformation("Email service configuration: FromEmail={FromEmail}, FromName={FromName}, ToEmail={ToEmail}",
                _fromEmail, _fromName, _toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to initialize email service: {ErrorMessage}. Email functionality will be disabled.", ex.Message);
            throw; // Re-throw to allow proper handling upstream
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email address cannot be null or empty", nameof(to));
        
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be null or empty", nameof(subject));
        
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body cannot be null or empty", nameof(body));

        // Validate email format
        if (!IsValidEmailAddress(to))
            throw new ArgumentException($"Invalid email address format: {to}", nameof(to));

        try
        {
            _logger.LogInformation("Preparing to send email to {To} with subject: {Subject} via {SmtpServer}:{SmtpPort}",
                to, subject, _smtpHost, _smtpPort);

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false, // Explicitly disable default credentials for Office 365
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000 // 30 seconds timeout
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            message.To.Add(to);

            _logger.LogInformation("Attempting to send email from {FromEmail} to {ToEmail} via {SmtpServer}",
                _fromEmail, to, _smtpHost);

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

    /// <summary>
    /// Validates if the provided email address has a valid format
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email format is valid, false otherwise</returns>
    private static bool IsValidEmailAddress(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}