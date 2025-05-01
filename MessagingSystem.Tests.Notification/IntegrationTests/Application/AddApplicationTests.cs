using FluentValidation;
using MessagingSystem.Services.Notification.Application;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Encryptor.Decryption;

namespace MessagingSystem.Tests.Notification.IntegrationTests.Application;

public class ApplicationLayerIntegrationTests
{
    private readonly ServiceCollection _services;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<INotification> _notificationMock;
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock;

    public ApplicationLayerIntegrationTests()
    {
        _services = new ServiceCollection();
        _configurationMock = new Mock<IConfiguration>();
        _notificationMock = new Mock<INotification>();
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        
        _services.AddSingleton(_notificationMock.Object);
        _services.AddSingleton(_decryptionInfoMock.Object);
    }

    [Fact]
    public void AddApplicationLayer_RegistersValidatorAndOrchestrator()
    {
        // Act
        _services.AddApplicationLayer(_configurationMock.Object);
        var provider = _services.BuildServiceProvider();

        // Assert: UserValidator
        var validator = provider.GetService<IValidator<UserDto>>();
        Assert.NotNull(validator);
        Assert.IsType<UserValidator>(validator);

        // Assert: NotificationOrchestrator
        var orchestrator = provider.GetService<INotificationOrchestrator>();
        Assert.NotNull(orchestrator);
        Assert.IsType<NotificationOrchestrator>(orchestrator);
    }

    [Fact]
    public void UserValidator_ValidatesUserDto_Correctly()
    {
        // Arrange
        _services.AddApplicationLayer(_configurationMock.Object);
        var provider = _services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IValidator<UserDto>>();

        var validUser = new UserDto("UserTest", "user@example.com");
        var invalidUser = new UserDto("", "");

        // Act
        var validResult = validator.Validate(validUser);
        var invalidResult = validator.Validate(invalidUser);

        // Assert
        Assert.True(validResult.IsValid);               
        Assert.False(invalidResult.IsValid);
        Assert.NotEmpty(invalidResult.Errors);
    }

    [Fact]
    public async Task NotificationOrchestrator_CallsNotification()
    {
        // Arrange
        _services.AddApplicationLayer(_configurationMock.Object);
        var provider = _services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<INotificationOrchestrator>();

        var user = new UserDto("TestUser", "test@example.com");

        _notificationMock
            .Setup(n => n.SendConfirmEmailAsync(user, It.Is<string>(link => link.Contains("http://link.com"))))
            .ReturnsAsync(true);

        // Act
        var result = await orchestrator.SendConfirmEmailAsync(user, "http://link.com");

        // Assert
        Assert.NotNull(result.Data);
        _notificationMock.Verify(n => n.SendConfirmEmailAsync(user, It.Is<string>(link => link.Contains("http://link.com"))), Times.Once);
    }
}
