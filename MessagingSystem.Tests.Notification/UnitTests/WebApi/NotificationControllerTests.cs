using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Application.Notification;
using MessagingSystem.Services.Notification.WebApi;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MessagingSystem.Services.Notification.Application;

namespace MessagingSystem.Tests.Notification.UnitTests.WebApi
{
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationOrchestrator> _notificationOrchestratorMock;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            _notificationOrchestratorMock = new Mock<INotificationOrchestrator>();
            _controller = new NotificationController(_notificationOrchestratorMock.Object);
        }

        [Fact]
        public async Task SendConfirmEmailAsync_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var nickName = "Johnny";
            var operationResult = OperationResult<string>.Ok("Confirmation email sent");

            _notificationOrchestratorMock
                .Setup(x => x.SendConfirmEmailAsync(userDto, nickName))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendConfirmEmailAsync(userDto, nickName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Confirmation email sent", okResult.Value);
        }

        [Fact]
        public async Task SendConfirmEmailAsync_ShouldReturnBadRequest_WhenFailed()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var nickName = "Johnny";
            var operationResult = OperationResult<string>.Fail("Error sending confirmation email");

            _notificationOrchestratorMock
                .Setup(x => x.SendConfirmEmailAsync(userDto, nickName))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendConfirmEmailAsync(userDto, nickName);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error sending confirmation email", badRequestResult.Value);
        }

        [Fact]
        public async Task SendDeleteUserEmailAsync_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var operationResult = OperationResult<string>.Ok("User deletion email sent");

            _notificationOrchestratorMock
                .Setup(x => x.SendDeleteUserInfoEmailAsync(userDto))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendDeleteUserEmailAsync(userDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User deletion email sent", okResult.Value);
        }

        [Fact]
        public async Task SendDeleteUserEmailAsync_ShouldReturnBadRequest_WhenFailed()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var operationResult = OperationResult<string>.Fail("Error sending deletion email");

            _notificationOrchestratorMock
                .Setup(x => x.SendDeleteUserInfoEmailAsync(userDto))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendDeleteUserEmailAsync(userDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error sending deletion email", badRequestResult.Value);
        }

        [Fact]
        public async Task SendEditUserEmailAsync_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var operationResult = OperationResult<string>.Ok("User edit email sent");

            _notificationOrchestratorMock
                .Setup(x => x.SendEditUserInfoEmailAsync(userDto))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendEditUserEmailAsync(userDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("User edit email sent", okResult.Value);
        }

        [Fact]
        public async Task SendEditUserEmailAsync_ShouldReturnBadRequest_WhenFailed()
        {
            // Arrange
            var userDto = new UserDto("john", "john@example.com");
            var operationResult = OperationResult<string>.Fail("Error sending edit email");

            _notificationOrchestratorMock
                .Setup(x => x.SendEditUserInfoEmailAsync(userDto))
                .ReturnsAsync(operationResult);

            // Act
            var result = await _controller.SendEditUserEmailAsync(userDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Error sending edit email", badRequestResult.Value);
        }
    }
}
