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
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Oto;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Microsoft.AspNetCore.SignalR;
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
        private readonly Mock<IHubContext<GroupChatHub>> _mockGroupHubContext;
        private readonly Mock<IHubContext<OtoChatHub>> _mockOtoHubContext;
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
            _mockGroupHubContext = new Mock<IHubContext<GroupChatHub>>();
            _mockOtoHubContext = new Mock<IHubContext<OtoChatHub>>();

            _consumer = new EditUserInfoConsumer(
                _mockGroupMemberOrchestrator.Object,
                _mockGroupMessagesOrchestrator.Object,
                _mockGroupInfoOrchestrator.Object,
                _mockUserOrchestrator.Object,
                _mockMessageOrchestrator.Object,
                _mockDecryptionInfo.Object,
                _mockEncryptionInfo.Object,
                _mockPublicKeyStorage.Object,
                _mockGroupHubContext.Object,
                _mockOtoHubContext.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task Consume_WhenPublicKeyNotFound_ShouldLogWarningAndReturn()
        {
            // Arrange
            var request = new EditUserInfoRequest("userHash", "userNickName", "imageData", "oldNickName");
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
        public async Task Consume_WhenMemberTaskHasData_ShouldLogInformation()
        {
            // Arrange
            var request = new EditUserInfoRequest("userHash789", "dataNickName", "dataImageData", "oldNickName");
            var publicKey = "publicKeyData";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var resultWithData = OperationResult<List<Guid>>.Ok([Guid.NewGuid()]);
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sending user info update notification")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task Consume_WhenExceptionThrown_ShouldReturnFallbackResponseAndLogError()
        {
            // Arrange
            var request = new EditUserInfoRequest("errorHash", "errorNickName", "errorImageData", "oldNickName");
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
            var request = new EditUserInfoRequest("testHash", "testNickName", "testImageData", "oldNickName");
            var publicKey = "testPublicKey";
            var encryptedImage = "encryptedTestImageData";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);
            _mockEncryptionInfo.Setup(x => x.Encrypt("testImageData")).Returns(encryptedImage);

            var successResult = OperationResult<string>.Ok("");
            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<List<Guid>>.Ok([Guid.NewGuid()]));
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
        public async Task Consume_WhenEncryptionThrowsException_ShouldReturnFallbackResponse()
        {
            // Arrange
            var request = new EditUserInfoRequest("encryptHash", "encryptNickName", "encryptImageData", "oldNickName");
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
        
        [Fact]
        public async Task Consume_WhenNotAllTasksSuccessful_ShouldNotCallNotifyAndReturnFalseResponse()
        {
            // Arrange
            var request = new EditUserInfoRequest("testHash", "testNickName", "testImageData", "oldNickName");
            var publicKey = "testPublicKey";

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);
    
            var successResult = OperationResult<string>.Ok("");
            var failedResult = OperationResult<string>.Fail("Some error");
    
            _mockGroupMemberOrchestrator.Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(OperationResult<List<Guid>>.Ok(new List<Guid>()));
            _mockGroupInfoOrchestrator.Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(failedResult);
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
                It.Is<EditUserRollBack>(r => r.IsSuccess == false)), Times.Once);
            
            _mockGroupHubContext.Verify(x => x.Clients.Group(It.IsAny<string>()), Times.Never);
            _mockOtoHubContext.Verify(x => x.Clients.All, Times.Never);
        }
        
        [Fact]
        public async Task NotifyUserInfoChanged_WhenExceptionInNotification_ShouldLogErrorAndSendFallbackResponse()
        {
            // Arrange
            var request = new EditUserInfoRequest("testHash", "testNickName", "testImageData", "oldNickName");
            var publicKey = "testPublicKey";
            var groupIds = new List<Guid> { Guid.NewGuid() };

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var successResult = OperationResult<string>.Ok("");
            var memberResult = OperationResult<List<Guid>>.Ok(groupIds);
    
            _mockGroupMemberOrchestrator
                .Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(memberResult);
            _mockGroupInfoOrchestrator
                .Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator
                .Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator
                .Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator
                .Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            var mockGroupClients = new Mock<IHubClients>();
            var mockGroupProxy = new Mock<IClientProxy>();
            var mockOtoClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockGroupClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroupProxy.Object);
            mockOtoClients.Setup(c => c.All).Returns(mockClientProxy.Object);

            _mockGroupHubContext.Setup(x => x.Clients).Returns(mockGroupClients.Object);
            _mockOtoHubContext.Setup(x => x.Clients).Returns(mockOtoClients.Object);
    
            mockGroupProxy
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("SignalR error"));

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to notify user info change")),
                    It.Is<InvalidOperationException>(ex => ex.Message == "SignalR error"),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unhandled exception in EditUserInfoConsumer")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task NotifyUserInfoChanged_WhenCalledWithValidData_ShouldSendNotifications()
        {
            // Arrange
            var request = new EditUserInfoRequest("validHash", "newName", "img", "oldName");
            var publicKey = "pubKey";
            var groupIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            _mockConsumeContext.Setup(x => x.Message).Returns(request);
            _mockPublicKeyStorage.Setup(x => x.Get("User")).Returns(publicKey);

            var successResult = OperationResult<string>.Ok("");
            var memberResult = OperationResult<List<Guid>>.Ok(groupIds);

            _mockGroupMemberOrchestrator
                .Setup(x => x.UpdateMemberInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(memberResult);
            _mockGroupInfoOrchestrator
                .Setup(x => x.EditGroupsAdminAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockMessageOrchestrator
                .Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockGroupMessagesOrchestrator
                .Setup(x => x.UpdateUserInfoInMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);
            _mockUserOrchestrator
                .Setup(x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(successResult);

            var mockGroupClients = new Mock<IHubClients>();
            var mockGroupProxy = new Mock<IClientProxy>();
            var mockOtoClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            _mockGroupHubContext.Setup(x => x.Clients).Returns(mockGroupClients.Object);
            _mockOtoHubContext.Setup(x => x.Clients).Returns(mockOtoClients.Object);

            mockGroupClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockGroupProxy.Object);
            mockGroupProxy
                .Setup(x => x.SendCoreAsync("UserInfoChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockOtoClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            mockClientProxy
                .Setup(x => x.SendCoreAsync("UserInfoChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _consumer.Consume(_mockConsumeContext.Object);

            // Assert
            foreach (var groupId in groupIds)
            {
                mockGroupClients.Verify(c => c.Group(groupId.ToString()), Times.Once);
            }

            mockGroupProxy.Verify(x => x.SendCoreAsync("UserInfoChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(groupIds.Count));
            mockClientProxy.Verify(x => x.SendCoreAsync("UserInfoChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully sent UserInfoChanged notifications")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}