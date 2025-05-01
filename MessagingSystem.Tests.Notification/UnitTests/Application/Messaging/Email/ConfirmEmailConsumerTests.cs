using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application.Messaging.Email;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Microsoft.Extensions.Logging;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Application.Messaging.Email;

public class ConfirmEmailConsumerTests
{
    private readonly Mock<INotificationOrchestrator> _notificationOrchestratorMock;
    private readonly Mock<ConsumeContext<ConfirmUserEmail>> _contextMock;
    private readonly ConfirmEmailConsumer _consumer;

    public ConfirmEmailConsumerTests()
    {
        _notificationOrchestratorMock = new Mock<INotificationOrchestrator>();
        _contextMock = new Mock<ConsumeContext<ConfirmUserEmail>>();
        Mock<ILogger<ConfirmEmailConsumer>> loggerMock = new();
        _consumer = new ConfirmEmailConsumer(_notificationOrchestratorMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldSendConfirmEmailSuccessfully()
    {
        // Arrange
        var message = new ConfirmUserEmail("john", "john@example.com", "Johnny");

        _contextMock.Setup(x => x.Message).Returns(message);

        var expectedResult = Services.Notification.Application.OperationResult<string>.Ok("Confirmation email sent");

        _notificationOrchestratorMock
            .Setup(x => x.SendConfirmEmailAsync(
                It.Is<UserDto>(dto => dto.UserName == "john" && dto.Email == "john@example.com"),
                "Johnny"))
            .ReturnsAsync(expectedResult);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _notificationOrchestratorMock.Verify(x =>
            x.SendConfirmEmailAsync(It.IsAny<UserDto>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Consume_WhenExceptionThrown_ShouldCatchAndLog()
    {
        // Arrange
        var message = new ConfirmUserEmail("jane", "jane@example.com", "Janey");
        
        _contextMock.Setup(x => x.Message).Returns(message);

        _notificationOrchestratorMock
            .Setup(x => x.SendConfirmEmailAsync(It.IsAny<UserDto>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        // Act
        var exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

        // Assert
        Assert.Null(exception);
        _notificationOrchestratorMock.Verify(x =>
            x.SendConfirmEmailAsync(It.IsAny<UserDto>(), It.IsAny<string>()), Times.Once);
    }
}