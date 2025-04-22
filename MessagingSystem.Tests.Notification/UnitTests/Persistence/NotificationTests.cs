using Moq;
using MessagingSystem.Services.Notification.Core.User;
using MessagingSystem.Services.Notification.Infrastructure.ReaderTemplate;
using Microsoft.AspNetCore.Identity.UI.Services;
using FluentAssertions;
using MessagingSystem.Services.Notification.Infrastructure.MailJet;

namespace MessagingSystem.Tests.Notification.UnitTests.Persistence
{
    public class NotificationTests
    {
        private readonly Mock<IEmailSender> _emailSenderMock;
        private readonly Mock<ITemplateReader> _templateReaderMock;
        private readonly Services.Notification.Persistence.Notification _notification;

        public NotificationTests()
        {
            _emailSenderMock = new Mock<IEmailSender>();
            _templateReaderMock = new Mock<ITemplateReader>();
            _notification = new Services.Notification.Persistence.Notification(_emailSenderMock.Object, _templateReaderMock.Object);
        }

        [Fact]
        public async Task SendConfirmEmailAsync_ShouldSendEmail_WhenTemplateIsFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");
            var link = "http://example.com/confirm";
            var template = "<html><body>Welcome {UserName}, confirm your email <a href='{link}'>here</a>.</body></html>";

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync(template);

            // Act
            var result = await _notification.SendConfirmEmailAsync(userDto, link);

            // Assert
            result.Should().BeTrue();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(userDto.Email, Wc.ConfirmEmail, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendConfirmEmailAsync_ShouldReturnFalse_WhenTemplateIsNotFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");
            var link = "http://example.com/confirm";

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync((string)null);

            // Act
            var result = await _notification.SendConfirmEmailAsync(userDto, link);

            // Assert
            result.Should().BeFalse();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendEditUserInfoEmailAsync_ShouldSendEmail_WhenTemplateIsFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");
            var template = "<html><body>Dear {UserName}, your user info has been edited.</body></html>";

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync(template);

            // Act
            var result = await _notification.SendEditUserInfoEmailAsync(userDto);

            // Assert
            result.Should().BeTrue();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(userDto.Email, Wc.EditUser, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendEditUserInfoEmailAsync_ShouldReturnFalse_WhenTemplateIsNotFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync((string)null);

            // Act
            var result = await _notification.SendEditUserInfoEmailAsync(userDto);

            // Assert
            result.Should().BeFalse();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendDeleteUserEmailAsync_ShouldSendEmail_WhenTemplateIsFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");
            var template = "<html><body>Dear {UserName}, your account is scheduled for deletion.</body></html>";

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync(template);

            // Act
            var result = await _notification.SendDeleteUserEmailAsync(userDto);

            // Assert
            result.Should().BeTrue();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(userDto.Email, Wc.DeleteUser, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendDeleteUserEmailAsync_ShouldReturnFalse_WhenTemplateIsNotFound()
        {
            // Arrange
            var userDto = new UserDto("johnDoe", "john@example.com");

            _templateReaderMock.Setup(r => r.ReadTemplateAsync(It.IsAny<string>())).ReturnsAsync((string)null);

            // Act
            var result = await _notification.SendDeleteUserEmailAsync(userDto);

            // Assert
            result.Should().BeFalse();
            _templateReaderMock.Verify(r => r.ReadTemplateAsync(It.IsAny<string>()), Times.Once);
            _emailSenderMock.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
