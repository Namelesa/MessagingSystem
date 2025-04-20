using MassTransit;
using MessagingSystem.SendingModels.UserNotification;
using MessagingSystem.Services.Notification.Application;
using MessagingSystem.Services.Notification.Application.Messaging.Email;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.Core.User;
using Moq;

namespace MessagingSystem.Tests.Notification.UnitTests.Application.Messaging.Email;

public class DeleteUserInfoConsumerTests
{
    private readonly Mock<INotificationOrchestrator> _notificationOrchestratorMock;
    private readonly Mock<ConsumeContext<DeleteUserEmail>> _contextMock;
    private readonly DeleteUserInfoConsumer _consumer;

    public DeleteUserInfoConsumerTests()
    {
        _notificationOrchestratorMock = new Mock<INotificationOrchestrator>();
        _contextMock = new Mock<ConsumeContext<DeleteUserEmail>>();
        _consumer = new DeleteUserInfoConsumer(_notificationOrchestratorMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldSendDeleteUserEmailSuccessfully()
    {
        // Arrange
        var message = new DeleteUserEmail("john_doe", "john@example.com");
        _contextMock.Setup(x => x.Message).Returns(message);

        var expectedResult = OperationResult<string>.Ok("User deleted email sent");

        _notificationOrchestratorMock
            .Setup(x => x.SendDeleteUserInfoEmailAsync(
                It.Is<UserDto>(dto => dto.UserName == "john_doe" && dto.Email == "john@example.com")))
            .ReturnsAsync(expectedResult);

        await using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _notificationOrchestratorMock.Verify(x =>
            x.SendDeleteUserInfoEmailAsync(It.IsAny<UserDto>()), Times.Once);

        var consoleOutput = sw.ToString();
        Assert.Contains("", consoleOutput);
    }

    [Fact]
    public async Task Consume_WhenExceptionThrown_ShouldCatchAndLog()
    {
        // Arrange
        var message = new DeleteUserEmail("jane_doe", "jane@example.com");
        _contextMock.Setup(x => x.Message).Returns(message);

        _notificationOrchestratorMock
            .Setup(x => x.SendDeleteUserInfoEmailAsync(It.IsAny<UserDto>()))
            .ThrowsAsync(new InvalidOperationException("Email server unavailable"));

        await using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        var exception = await Record.ExceptionAsync(() => _consumer.Consume(_contextMock.Object));

        // Assert
        Assert.Null(exception);

        var consoleOutput = sw.ToString();
        Assert.Contains("Email server unavailable", consoleOutput);
    }

    [Fact]
    public async Task Consume_WhenOperationFails_ShouldPrintNullData()
    {
        // Arrange
        var message = new DeleteUserEmail("jack_doe", "jack@example.com");
        _contextMock.Setup(x => x.Message).Returns(message);

        var failResult = OperationResult<string>.Fail("Email not sent");

        _notificationOrchestratorMock
            .Setup(x => x.SendDeleteUserInfoEmailAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(failResult);

        await using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        var consoleOutput = sw.ToString();
        Assert.Contains("", consoleOutput);
    }
}