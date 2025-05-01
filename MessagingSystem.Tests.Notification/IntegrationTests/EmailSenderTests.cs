using Mailjet.Client;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;

namespace MessagingSystem.Tests.Notification.IntegrationTests;

public class EmailSenderTests
{
    private readonly Mock<ILogger<EmailSender>> _loggerMock;
    private readonly IConfiguration _configuration;

    public EmailSenderTests()
    {
        _loggerMock = new Mock<ILogger<EmailSender>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "invalid-api-key" },
            { "MailJet:SecretKey", "invalid-secret-key" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_MailJetSettings_Are_Null()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EmailSender>>();
        var emptyConfiguration = new ConfigurationBuilder().Build();
        var emailSender = new EmailSender(emptyConfiguration, loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("test@example.com", "Subject", "<p>Message</p>");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => 
                    state.ToString()!.Contains("MailJet settings are null")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_Should_Not_LogError_When_MailJetSettings_Are_Valid()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EmailSender>>();
    
        var validSettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "valid-api-key" },
            { "MailJet:SecretKey", "valid-secret-key" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(validSettings)
            .Build();

        var emailSender = new EmailSender(configuration, loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("test@example.com", "Subject", "<p>Message</p>");

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("MailJet settings are null")
                ),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_Response_IsNotSuccessful()
    {
        // Arrange
        var emailSender = new EmailSender(_configuration, _loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("invalid@example.com", "Subject", "<p>Message</p>");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            ), Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_Should_LogInformation_When_Email_Sent_Successfully()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "" },
            { "MailJet:SecretKey", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        var mailjetClientMock = new Mock<IMailjetClient>();
        
        mailjetClientMock
            .Setup(x => x.PostAsync(It.IsAny<MailjetRequest>()))
            .ReturnsAsync(new MailjetResponse(true, 200, new JObject())); 

        var emailSender = new EmailSender(configuration, _loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("valid-email@example.com", "Test Subject", "<p>Test Message</p>");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email sent successfully")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            ), Times.Once
        );
    }

    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_Email_Fails_To_Send()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "your-valid-or-dummy-api-key" },
            { "MailJet:SecretKey", "your-valid-or-dummy-secret-key" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        var mailjetClientMock = new Mock<IMailjetClient>();
        
        mailjetClientMock
            .Setup(x => x.PostAsync(It.IsAny<MailjetRequest>()))
            .ReturnsAsync(new MailjetResponse(false, 400, new JObject()));

        var emailSender = new EmailSender(configuration, _loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("invalid-email-address", "Test Subject", "<p>Test Message</p>");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            ), Times.Once
        );
    }
    
    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_401Occurs()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "invalid-api-key" }, 
            { "MailJet:SecretKey", "invalid-secret-key" } 
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var loggerMock = new Mock<ILogger<EmailSender>>();
        var emailSender = new EmailSender(configuration, loggerMock.Object);
        
        await emailSender.SendEmailAsync("test@example.com", "Subject", "Message");

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send email. Status: 401")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
    
    [Fact]
    private async Task SendEmailAsync_Should_LogError_When_EmailIsNull()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "" },
            { "MailJet:SecretKey", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        // Arrange
        var emailSender = new EmailSender(configuration, _loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync(null, "Subject", "<p>Message</p>");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Email is null or empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
    
    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_MessageIsEmpty()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "MailJet:ApiKey", "" },
            { "MailJet:SecretKey", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        var emailSender = new EmailSender(configuration, _loggerMock.Object);

        // Act
        await emailSender.SendEmailAsync("test@example.com", "Subject", "");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Message is empty")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
    
    [Fact]
    public async Task SendEmailAsync_Should_LogError_When_MailJetApiKeyIsMissing()
    {
        // Arrange
        var incompleteSettings = new Dictionary<string, string?>
        {
            { "MailJet:SecretKey", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(incompleteSettings)
            .Build();

        var emailSender = new EmailSender(configuration, _loggerMock.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => emailSender.SendEmailAsync("test@example.com", "Subject", "<p>Message</p>"));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentNullException>(exception);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("An error occurred while sending email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}
