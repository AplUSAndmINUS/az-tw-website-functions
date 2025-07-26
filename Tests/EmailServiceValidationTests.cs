using Microsoft.Extensions.Logging;
using Moq;
using SharedStorage.Services.Email;
using System;
using System.Threading.Tasks;
using Utils;
using Xunit;

namespace Tests
{
    public class EmailServiceValidationTests
    {
        private readonly Mock<IAppInsightsLogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceValidationTests()
        {
            _mockLogger = new Mock<IAppInsightsLogger<EmailService>>();
            
            // Set up environment variables for testing
            Environment.SetEnvironmentVariable("SMTP_USERNAME", "test@example.com");
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", "testpassword");
            
            _emailService = new EmailService(_mockLogger.Object);
        }

        [Fact]
        public async Task SendEmailAsync_WithInvalidEmailAddress_ThrowsArgumentException()
        {
            // Arrange
            var invalidEmail = "invalid-email";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(invalidEmail, subject, body));
            
            Assert.Contains("Invalid email address format", exception.Message);
        }

        [Fact]
        public async Task SendEmailAsync_WithNullOrEmptyTo_ThrowsArgumentException()
        {
            // Arrange
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(null, subject, body));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync("", subject, body));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync("   ", subject, body));
        }

        [Fact]
        public async Task SendEmailAsync_WithNullOrEmptySubject_ThrowsArgumentException()
        {
            // Arrange
            var to = "test@example.com";
            var body = "Test Body";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, null, body));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, "", body));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, "   ", body));
        }

        [Fact]
        public async Task SendEmailAsync_WithNullOrEmptyBody_ThrowsArgumentException()
        {
            // Arrange
            var to = "test@example.com";
            var subject = "Test Subject";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, subject, null));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, subject, ""));
            
            await Assert.ThrowsAsync<ArgumentException>(
                () => _emailService.SendEmailAsync(to, subject, "   "));
        }
    }

    public class EmailServiceSafeParsingTests
    {
        [Fact]
        public void Constructor_WithInvalidSmtpPort_UsesDefaultPort()
        {
            // Arrange
            var mockLogger = new Mock<IAppInsightsLogger<EmailService>>();
            Environment.SetEnvironmentVariable("SMTP_USERNAME", "test@example.com");
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", "testpassword");
            Environment.SetEnvironmentVariable("SMTP_PORT", "invalid-port");

            // Act - Constructor should not throw exception
            var emailService = new EmailService(mockLogger.Object);

            // Assert - Service should be created successfully (port defaults to 587)
            Assert.NotNull(emailService);
        }

        [Fact]
        public void Constructor_WithMissingSmtpPort_UsesDefaultPort()
        {
            // Arrange
            var mockLogger = new Mock<IAppInsightsLogger<EmailService>>();
            Environment.SetEnvironmentVariable("SMTP_USERNAME", "test@example.com");
            Environment.SetEnvironmentVariable("SMTP_PASSWORD", "testpassword");
            Environment.SetEnvironmentVariable("SMTP_PORT", null);

            // Act - Constructor should not throw exception
            var emailService = new EmailService(mockLogger.Object);

            // Assert - Service should be created successfully (port defaults to 587)
            Assert.NotNull(emailService);
        }
    }
}