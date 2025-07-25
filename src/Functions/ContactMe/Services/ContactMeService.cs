using Functions.ContactMe.Models;
using SharedStorage.Services.BaseServices;
using SharedStorage.Services.Email;
using SharedStorage.Environment;
using Utils;

namespace Functions.ContactMe.Services;

/// <summary>
/// Service for handling contact form submissions
/// </summary>
public class ContactMeService : IContactMeService
{
    private readonly ITableStorageService _tableStorageService;
    private readonly IEmailService _emailService;
    private readonly IAppInsightsLogger<ContactMeService> _logger;
    private readonly IAppMode _appMode;
    private readonly string _toEmail;

    public ContactMeService(
        ITableStorageService tableStorageService,
        IEmailService emailService,
        IAppInsightsLogger<ContactMeService> logger,
        IAppMode appMode)
    {
        _tableStorageService = tableStorageService ?? throw new ArgumentNullException(nameof(tableStorageService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appMode = appMode ?? throw new ArgumentNullException(nameof(appMode));

        try
        {
            var toEmail = System.Environment.GetEnvironmentVariable("TO_EMAIL") ?? System.Environment.GetEnvironmentVariable("SMTP_USERNAME");
            if (string.IsNullOrEmpty(toEmail))
            {
                _logger.LogWarning("TO_EMAIL and SMTP_USERNAME environment variables are missing. Email notifications will be disabled.");
                throw new InvalidOperationException("TO_EMAIL or SMTP_USERNAME environment variable is required");
            }
            _toEmail = toEmail;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Email recipient configuration error: {ErrorMessage}. Email notifications will be disabled.", ex.Message);
            throw; // Re-throw to allow proper handling upstream
        }
    }

    public async Task ProcessContactSubmissionAsync(ContactMeModel model)
    {
        try
        {
            _logger.LogInformation("Processing contact submission from {Name} ({Email})", model.Name, model.Email);

            // Always store the contact data
            await StoreContactSubmissionAsync(model);

            try
            {
                // Try to send email, but don't fail the entire process if it fails
                await SendContactEmailAsync(model);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SMTP") || ex.Message.Contains("environment variable"))
            {
                // Log the error but don't re-throw as we've already stored the contact data
                _logger.LogWarning("Email configuration error: {ErrorMessage}. Contact data was saved but email notification was not sent.", ex.Message);
            }

            _logger.LogInformation("Successfully processed contact submission for {Name}", model.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to process contact submission for {Name} ({Email})", ex, model.Name, model.Email);
            throw;
        }
    }

    public async Task StoreContactSubmissionAsync(ContactMeModel model)
    {
        try
        {
            var tableName = GetTableName();
            var entity = new ContactMeEntity(model);

            _logger.LogInformation("Storing contact submission in table {TableName} for {Name}", tableName, model.Name);

            await _tableStorageService.UpsertEntityAsync(tableName, entity);

            _logger.LogInformation("Successfully stored contact submission for {Name}", model.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to store contact submission for {Name}", ex, model.Name);
            throw;
        }
    }

    private async Task SendContactEmailAsync(ContactMeModel model)
    {
        try
        {
            var subject = $"TerenceWaters.com Email Submission - {model.Name}";
            var body = _emailService.FormatContactEmail(
                model.Name,
                model.Email,
                model.Message,
                model.SubmittedAt,
                model.UserAgent,
                model.IpAddress
            );

            _logger.LogInformation("Sending contact email for {Name} to {ToEmail}", model.Name, _toEmail);

            await _emailService.SendEmailAsync(_toEmail, subject, body, isHtml: false);

            _logger.LogInformation("Successfully sent contact email for {Name}", model.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send contact email for {Name}", ex, model.Name);
            throw;
        }
    }

    private string GetTableName()
    {
        return _appMode.UseMockStorage ? "mockcontact" : "contact";
    }
}