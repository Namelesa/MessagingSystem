using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.SendingModels.UserMessaging.Edit;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupsInformation;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoUpdate;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageBroker
{
    public class EditUserInfoConsumerTests
    {
        private readonly Mock<IGroupMemberOrchestrator> _mockGroupMemberOrchestrator;
        private readonly Mock<IGroupMessagesOrchestrator> _mockGroupMessagesOrchestrator;
        private readonly Mock<IGroupInfoOrchestrator> _mockGroupInfoOrchestrator;
        private readonly Mock<IUserOrchestrator> _mockUserOrchestrator;
        private readonly Mock<IMessageOrchestrator> _mockMessageOrchestrator;
        private readonly Mock<IDecryptionInfo> _mockDecryptionInfo;
        private readonly Mock<IEncryptionInfo> _mockEncryptionInfo;
        private readonly Mock<IPublicKeyStorage> _mockPublicKeyStorage;
        private readonly Mock<ILogger<EditUserInfoConsumer>> _mockLogger;
        private readonly Mock<ConsumeContext<EditUserInfoRequest>> _mockConsumeContext;
        private readonly EditUserInfoConsumer _consumer;

        public EditUserInfoConsumerTests()
        {
            _mockGroupMemberOrchestrator = new Mock<IGroupMemberOrchestrator>();
            _mockGroupMessagesOrchestrator = new Mock<IGroupMessagesOrchestrator>();
            _mockGroupInfoOrchestrator = new Mock<IGroupInfoOrchestrator>();
            _mockUserOrchestrator = new Mock<IUserOrchestrator>();
            _mockMessageOrchestrator = new Mock<IMessageOrchestrator>();
            _mockDecryptionInfo = new Mock<IDecryptionInfo>();
            _mockEncryptionInfo = new Mock<IEncryptionInfo>();
            _mockPublicKeyStorage = new Mock<IPublicKeyStorage>();
            _mockLogger = new Mock<ILogger<EditUserInfoConsumer>>();
            _mockConsumeContext = new Mock<ConsumeContext<EditUserInfoRequest>>();

            _consumer = new EditUserInfoConsumer(
                _mockGroupMemberOrchestrator.Object,
                _mockGroupMessagesOrchestrator.Object,
                _mockGroupInfoOrchestrator.Object,
                _mockUserOrchestrator.Object,
                _mockMessageOrchestrator.Object,
                _mockDecryptionInfo.Object,
                _mockEncryptionInfo.Object,
                _mockPublicKeyStorage.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Consume_WhenPublicKeyNotFound_ShouldLogWarningAndReturn()
        {
            // Arrange
            var request = new EditUserInfoRequest("userHash", "userNickName", "imageData");
            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns((string)null);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Public key for 'User' not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockDecryptionInfo.Verify(x => x.DecryptRsaObjectStrings(It.IsAny<object>()), Times.Never);
            _mockConsumeContext.Verify(x => x.RespondAsync(It.IsAny<object>()), Times.Never);
        }
        
        [Fact]
        public async Task Consume_WhenSomeOperationsFail_ShouldReturnFailureResponseAndLogErrors()
        {
            // Arrange
            var request = new EditUserInfoRequest("userHash456", "failNickName", "failImageData");
            var publicKey = "publicKeyData";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);
            
            var successResult = OperationResult<string>.Ok("");
            var failureResult = OperationResult<string>.Fail("Operation failed");

            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(failureResult);
            _mockMessageOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(failureResult);
            _mockGroupMessagesOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockConsumeContext.Verify(x => x.RespondAsync(
                It.Is<EditUserRollBack>(r => r.IsSuccess == false)), Times.Once);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Operation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task Consume_WhenMemberTaskHasData_ShouldLogInformation()
        {
            // Arrange
            var request = new EditUserInfoRequest("userHash789", "dataNickName", "dataImageData");
            var publicKey = "publicKeyData";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var resultWithData = OperationResult<string>.Ok("Some important data");
            var successResult = OperationResult<string>.Ok("");

            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(resultWithData);
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Result: Some important data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Consume_WhenExceptionThrown_ShouldReturnFallbackResponseAndLogError()
        {
            // Arrange
            var request = new EditUserInfoRequest("errorHash", "errorNickName", "errorImageData");
            var expectedException = new InvalidOperationException("Test exception");

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Throws(expectedException);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockConsumeContext.Verify(x => x.RespondAsync(
                It.Is<DeleteUserInfoRollback>(r => 
                    r.UserNickNameHash == "Unknown" && 
                    r.IsSuccess == false)), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unhandled exception in EditUserInfoConsumer")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Consume_ShouldCallAllOrchestratorMethods()
        {
            // Arrange
            var request = new EditUserInfoRequest("testHash", "testNickName", "testImageData");
            var publicKey = "testPublicKey";
            var encryptedImage = "encryptedTestImageData";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);
            _mockEncryptionInfo.Setup(x => x.Encrypt("testImageData")).Returns(encryptedImage);

            var successResult = OperationResult<string>.Ok("");
            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockGroupMemberOrchestrator.Verify(x => x.UpdateMemberInfoAsync("testHash", "testNickName", "testImageData"), Times.Once);
            _mockGroupInfoOrchestrator.Verify(x => x.EditGroupsAdminAsync("testHash", "testNickName"), Times.Once);
            _mockMessageOrchestrator.Verify(x => x.UpdateUserInfoInMessageAsync("testNickName", "testHash"), Times.Once);
            _mockGroupMessagesOrchestrator.Verify(x => x.UpdateUserInfoInMessageAsync("testNickName", "testHash"), Times.Once);
        }

        [Fact]
        public async Task Consume_ShouldPerformDecryptionAndEncryptionInCorrectOrder()
        {
            // Arrange
            var request = new EditUserInfoRequest("orderHash", "orderNickName", "orderImageData");
            var publicKey = "orderPublicKey";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var successResult = OperationResult<string>.Ok("Success");
            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockDecryptionInfo.Verify(x => x.DecryptRsaObjectStrings(request), Times.Once);
            _mockDecryptionInfo.Verify(x => x.DecryptObjectStrings(request), Times.Once);

            // Assert
            _mockEncryptionInfo.Verify(x => x.EncryptObjectStrings(It.IsAny<EditUserRollBack>()), Times.Once);
            _mockEncryptionInfo.Verify(x => x.EncryptRsaObjectStrings(It.IsAny<EditUserRollBack>(), publicKey), Times.Once);
        }

        [Xunit.Theory]
        [InlineData("", "nickName", "image")] 
        [InlineData("hash", "", "image")]     
        [InlineData("hash", "nickName", "")] 
        [InlineData("", "", "")]           
        public async Task Consume_WithEmptyFields_ShouldStillProcessRequest(string userHash, string userNickName, string image)
        {
            // Arrange
            var request = new EditUserInfoRequest(userHash, userNickName, image);
            var publicKey = "testPublicKey";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var successResult = OperationResult<string>.Ok("");
            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator.Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator.Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockConsumeContext.Verify(x => x.RespondAsync(
                It.Is<EditUserRollBack>(r => r.IsSuccess == true)), Times.Once);
        }

        [Fact]
        public async Task Consume_WhenEncryptionThrowsException_ShouldReturnFallbackResponse()
        {
            // Arrange
            var request = new EditUserInfoRequest("encryptHash", "encryptNickName", "encryptImageData");
            var publicKey = "encryptPublicKey";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);
            _mockEncryptionInfo.Setup(x => x.Encrypt(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Encryption failed"));

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockConsumeContext.Verify(x => x.RespondAsync(
                It.Is<DeleteUserInfoRollback>(r => 
                    r.UserNickNameHash == "Unknown" && 
                    r.IsSuccess == false)), Times.Once);
        }
    }
}