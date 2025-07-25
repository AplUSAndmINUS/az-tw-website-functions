using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Functions.ContactMe.Models;
using Functions.ContactMe.Services;
using System.Net;
using System.Text.Json;
using Utils;
using Utils.Validation;

namespace Functions.ContactMe.Functions;

/// <summary>
/// Azure Function for handling contact form submissions
/// </summary>
public class ContactMeFunction
{
    private readonly IContactMeService _contactMeService;
    private readonly IAppInsightsLogger<ContactMeFunction> _logger;
    private readonly IAPIKeyValidator _apiKeyValidator;

    public ContactMeFunction(
        IContactMeService contactMeService,
        IAppInsightsLogger<ContactMeFunction> logger,
        IAPIKeyValidator apiKeyValidator)
    {
        _contactMeService = contactMeService ?? throw new ArgumentNullException(nameof(contactMeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKeyValidator = apiKeyValidator ?? throw new ArgumentNullException(nameof(apiKeyValidator));
    }

    [Function("ContactMe")]
    public async Task<HttpResponseData> SubmitContactForm(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contact")] HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {
            _logger.LogInformation("Processing contact form submission");

            // Validate API key
            var apiKeyValidationResponse = await _apiKeyValidator.ValidateApiKeyAsync(req, _logger, "ContactMe");
            if (apiKeyValidationResponse != null)
            {
                return apiKeyValidationResponse;
            }

            // Read and deserialize request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                _logger.LogWarning("Empty request body received");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Request body is required");
                return badRequestResponse;
            }

            ContactMeDTO? contactDto;
            try
            {
                contactDto = JsonSerializer.Deserialize<ContactMeDTO>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                _logger.LogWarning("Invalid JSON in request body");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid JSON format");
                return badRequestResponse;
            }

            if (contactDto == null)
            {
                _logger.LogWarning("Null contact data received");
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Contact data is required");
                return badRequestResponse;
            }

            // Validate required fields
            var validationResponse = ValidateContactData(req, contactDto);
            if (validationResponse != null)
            {
                return validationResponse;
            }

            // Create contact model with additional metadata
            var contactModel = new ContactMeModel
            {
                Name = contactDto.Name.Trim(),
                Email = contactDto.Email.Trim(),
                Message = contactDto.Message.Trim(),
                Subject = contactDto.Subject?.Trim() ?? string.Empty,   // Added subject field
                Phone = contactDto.Phone?.Trim(),                       // Added phone field
                Company = contactDto.Company?.Trim(),                   // Added company field
                Website = contactDto.Website?.Trim(),                   // Added website field
                SubmittedAt = DateTime.UtcNow,
                UserAgent = req.Headers.TryGetValues("User-Agent", out var userAgentValues) ? userAgentValues.FirstOrDefault() ?? "" : "",
                IpAddress = GetClientIpAddress(req)
            };

            try
            {
                // Process the contact submission (store and send email)
                await _contactMeService.ProcessContactSubmissionAsync(contactModel);

                _logger.LogInformation("Successfully processed contact form submission from {Name} ({Email})", contactModel.Name, contactModel.Email);

                // Return success response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { success = true, message = "Contact form submitted successfully" }));
                response.Headers.Add("Content-Type", "application/json");
                return response;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SMTP_USERNAME") || ex.Message.Contains("environment variable"))
            {
                // Specific handling for missing email configuration
                _logger.LogError("Email service configuration error: {ErrorMessage}", ex, ex.Message);

                // Save the contact data anyway, even if we can't send the email
                _logger.LogInformation("Contact data saved for {Name} ({Email}), but email notification was not sent due to configuration error", contactModel.Name, contactModel.Email);

                // Return a more specific error message
                var configErrorResponse = req.CreateResponse(HttpStatusCode.OK);
                await configErrorResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    success = true,
                    message = "Your message was received, but email notifications are currently disabled. The site administrator will see your message in the system."
                }));
                configErrorResponse.Headers.Add("Content-Type", "application/json");
                return configErrorResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error processing contact form submission", ex);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("An error occurred while processing your request");
            return errorResponse;
        }
    }

    private HttpResponseData? ValidateContactData(HttpRequestData req, ContactMeDTO contactDto)
    {
        var errors = new List<string>();

        // Validate name
        if (string.IsNullOrWhiteSpace(contactDto.Name))
        {
            errors.Add("Name is required");
        }
        else if (contactDto.Name.Trim().Length < 2)
        {
            errors.Add("Name must be at least 2 characters long");
        }

        // Validate email
        if (string.IsNullOrWhiteSpace(contactDto.Email))
        {
            errors.Add("Email is required");
        }
        else if (!DataValidation.TryValidateEmail(contactDto.Email))
        {
            errors.Add("Please provide a valid email address");
        }

        // Validate message
        if (string.IsNullOrWhiteSpace(contactDto.Message))
        {
            errors.Add("Message is required");
        }
        else if (contactDto.Message.Trim().Length < 10)
        {
            errors.Add("Message must be at least 10 characters long");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Validation errors: {Errors}", string.Join(", ", errors));
            var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            var errorResponse = JsonSerializer.Serialize(new { errors });
            badRequestResponse.WriteStringAsync(errorResponse);
            badRequestResponse.Headers.Add("Content-Type", "application/json");
            return badRequestResponse;
        }

        return null;
    }

    private string GetClientIpAddress(HttpRequestData req)
    {
        try
        {
            // Try to get the real client IP from various headers
            if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                    return ip;
            }

            if (req.Headers.TryGetValues("X-Real-IP", out var realIp))
            {
                var ip = realIp.FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                    return ip;
            }

            if (req.Headers.TryGetValues("X-Client-IP", out var clientIp))
            {
                var ip = clientIp.FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                    return ip;
            }

            // Fallback to a default value
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}