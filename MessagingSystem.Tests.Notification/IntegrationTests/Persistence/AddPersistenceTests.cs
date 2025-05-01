using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using MessagingSystem.Services.Notification.Persistence;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MessagingSystem.Tests.Notification.IntegrationTests.Persistence;

public class PersistenceLayerIntegrationTests
{
    private readonly ServiceCollection _services;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ITemplateReader> _templateReaderMock;

    public PersistenceLayerIntegrationTests()
    {
        _services = new ServiceCollection();
        _configurationMock = new Mock<IConfiguration>();
        _emailSenderMock = new Mock<IEmailSender>();
        _templateReaderMock = new Mock<ITemplateReader>();

        _services.AddSingleton(_emailSenderMock.Object);
        _services.AddSingleton(_templateReaderMock.Object);
    }

    [Fact]
    public void AddPersistenceLayer_RegistersNotificationService()
    {
        // Act
        _services.AddPersistenceLayer(_configurationMock.Object);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var notificationService = serviceProvider.GetService<INotification>();
        Assert.NotNull(notificationService);
        Assert.IsType<Services.Notification.Persistence.Notification>(notificationService);
    }

    [Fact]
    public async Task ResolvedNotificationService_CanSendEmail()
    {
        // Arrange
        var testUser = new UserDto("TestUser", "test@example.com");
        var htmlTemplate = "<html><body>{UserName} - {link}</body></html>";

        _templateReaderMock
            .Setup(r => r.ReadTemplateAsync(It.IsAny<string>()))
            .ReturnsAsync(htmlTemplate);

        _emailSenderMock
            .Setup(e => e.SendEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _services.AddPersistenceLayer(_configurationMock.Object);
        var serviceProvider = _services.BuildServiceProvider();

        var notification = serviceProvider.GetRequiredService<INotification>();

        // Act
        var result = await notification.SendConfirmEmailAsync(testUser, "http://link.com");

        // Assert
        Assert.True(result);
        _templateReaderMock.Verify(r => r.ReadTemplateAsync(Wc.ConfirmEmailTemplate), Times.Once);
        _emailSenderMock.Verify(e =>
            e.SendEmailAsync("test@example.com", Wc.ConfirmEmail, It.Is<string>(body => body.Contains("TestUser") && body.Contains("http://link.com"))),
            Times.Once);
    }
}
