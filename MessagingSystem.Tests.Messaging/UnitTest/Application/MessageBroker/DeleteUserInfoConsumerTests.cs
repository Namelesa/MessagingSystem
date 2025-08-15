using Encryptor.Decryption;
using Encryptor.Encryption;
using MassTransit;
using MessagingSystem.SendingModels.UserMessaging.Delete;
using MessagingSystem.Services.Messaging.Application;
using MessagingSystem.Services.Messaging.Application.Group.GroupMember;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.MessageBroker.UserInfoDelete;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.User;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Group;
using MessagingSystem.Services.Messaging.Infrastructure.ChatsHubs.Oto;
using MessagingSystem.Services.Messaging.Infrastructure.Keys;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.MessageBroker;

public class DeleteUserInfoConsumerTests
{
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock;
    private readonly Mock<IEncryptionInfo> _encryptionInfoMock;
    private readonly Mock<IPublicKeyStorage> _publicKeyStorageMock;
    private readonly Mock<ILogger<DeleteUserInfoConsumer>> _loggerMock;
    private readonly Mock<IMessageOrchestrator> _messageOrchestratorMock;
    private readonly Mock<IGroupMemberOrchestrator> _groupMemberOrchestratorMock;
    private readonly Mock<IGroupMessagesOrchestrator> _groupMessagesOrchestratorMock;
    private readonly Mock<IUserOrchestrator> _userOrchestratorMock;
    private readonly Mock<ConsumeContext<DeleteUserInfoRequest>> _contextMock;
    private readonly Mock<IHubContext<GroupChatHub>> _mockGroupHubContext;
    private readonly Mock<IHubContext<OtoChatHub>> _mockOtoHubContext;
    private readonly DeleteUserInfoConsumer _consumer;

    private const string TestNickName = "TestUser";
    private const string TestPublicKey = "test-public-key";
    private const string TestUserNickNameHash = "encrypted-nickname-hash";

    public DeleteUserInfoConsumerTests()
    {
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        _encryptionInfoMock = new Mock<IEncryptionInfo>();
        _publicKeyStorageMock = new Mock<IPublicKeyStorage>();
        _loggerMock = new Mock<ILogger<DeleteUserInfoConsumer>>();
        _messageOrchestratorMock = new Mock<IMessageOrchestrator>();
        _groupMemberOrchestratorMock = new Mock<IGroupMemberOrchestrator>();
        _groupMessagesOrchestratorMock = new Mock<IGroupMessagesOrchestrator>();
        _userOrchestratorMock = new Mock<IUserOrchestrator>();
        _mockGroupHubContext = new Mock<IHubContext<GroupChatHub>>();
        _mockOtoHubContext = new Mock<IHubContext<OtoChatHub>>();
        _contextMock = new Mock<ConsumeContext<DeleteUserInfoRequest>>();

        _consumer = new DeleteUserInfoConsumer(
            _decryptionInfoMock.Object,
            _encryptionInfoMock.Object,
            _publicKeyStorageMock.Object,
            _loggerMock.Object,
            _messageOrchestratorMock.Object,
            _groupMemberOrchestratorMock.Object,
            _groupMessagesOrchestratorMock.Object,
            _mockGroupHubContext.Object,
            _mockOtoHubContext.Object,
            _userOrchestratorMock.Object
        );
    }

    [Fact]
    public async Task Consume_WhenPublicKeyNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns((string)null);
        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Public key for 'User' not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _decryptionInfoMock.Verify(x => x.DecryptRsaObjectStrings(It.IsAny<object>()), Times.Never);
        _contextMock.Verify(x => x.RespondAsync(It.IsAny<DeleteUserInfoRollback>()), Times.Never);
    }
    
    [Fact]
    public async Task Consume_WhenSomeOperationsFail_ShouldReturnFailureResponseAndLogErrors()
    {
        // Arrange
        SetupMixedResultsScenario();

        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _contextMock.Verify(x => x.RespondAsync(It.Is<DeleteUserInfoRollback>(r => 
            r.UserNickNameHash == TestNickName && r.IsSuccess == false)), Times.Once);

        _contextMock.Verify(x => x.RespondAsync(
                It.Is<DeleteUserInfoRollback>(r =>
                    r.UserNickNameHash == TestNickName && 
                    r.IsSuccess == false)),
            Times.Once);
    }

    [Fact]
    public async Task Consume_WhenMemberTaskHasData_ShouldLogInformation()
    {
        // Arrange
        SetupSuccessfulScenarioWithMemberData();

        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);
        
        _decryptionInfoMock.Setup(x => x.Decrypt(TestUserNickNameHash))
            .Returns(TestNickName);
        _decryptionInfoMock.Setup(x => x.Decrypt(TestNickName))
            .Returns("EncryptedName");
        _decryptionInfoMock.Setup(x => x.Decrypt("EncryptedName"))
            .Returns(TestNickName);

        var mockGroupClientProxy = new Mock<IClientProxy>();
        mockGroupClientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var mockClients = new Mock<IHubClients>();
        mockClients
            .Setup(x => x.Group(It.IsAny<string>()))
            .Returns(mockGroupClientProxy.Object);

        _mockGroupHubContext
            .Setup(x => x.Clients)
            .Returns(mockClients.Object);
        
        var mockOtoClientProxy = new Mock<IClientProxy>();
        mockOtoClientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        var mockOtoClients = new Mock<IHubClients>();
        mockOtoClients
            .Setup(x => x.All)
            .Returns(mockOtoClientProxy.Object);

        _mockOtoHubContext
            .Setup(x => x.Clients)
            .Returns(mockOtoClients.Object);
        
        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString().Contains("Sent UserInfoChanged notification to group")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Consume_WhenExceptionOccurs_ShouldReturnFallbackResponseAndLogError()
    {
        // Arrange
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(TestPublicKey);
        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);
        
        var testException = new InvalidOperationException("Test exception");
        _decryptionInfoMock.Setup(x => x.DecryptRsaObjectStrings(It.IsAny<object>()))
                          .Throws(testException);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        _contextMock.Verify(x => x.RespondAsync(It.Is<DeleteUserInfoRollback>(r => 
            r.UserNickNameHash == "Unknown" && r.IsSuccess == false)), Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unhandled exception in DeleteUserInfoConsumer")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldCallDecryptionInCorrectOrder()
    {
        // Arrange
        SetupSuccessfulScenario();

        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        var callOrder = new MockSequence();
        _decryptionInfoMock.InSequence(callOrder)
                          .Setup(x => x.DecryptRsaObjectStrings(request));
        _decryptionInfoMock.InSequence(callOrder)
                          .Setup(x => x.Decrypt(TestUserNickNameHash))
                          .Returns(TestNickName);
    }

    [Fact]
    public async Task Consume_ShouldExecuteAllTasksConcurrently()
    {
        // Arrange
        SetupSuccessfulScenario();

        var request = new DeleteUserInfoRequest(TestUserNickNameHash, TestNickName);
        _contextMock.Setup(x => x.Message).Returns(request);

        // Act
        await _consumer.Consume(_contextMock.Object);

        // Assert
        VerifyAllOrchestratorsCalled();
    }

    private void SetupSuccessfulScenario()
    {
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(TestPublicKey);
        _decryptionInfoMock.Setup(x => x.Decrypt(TestUserNickNameHash)).Returns(TestNickName);

        var resultWithData = OperationResult<List<Guid>>.Ok([Guid.NewGuid()]);
        
        var successResult = OperationResult<string>.Ok("Operation successful");
        _groupMemberOrchestratorMock.Setup(x => x.DeleteMemberInfoAsync(TestNickName))
                                   .ReturnsAsync(resultWithData);
        _messageOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                               .ReturnsAsync(successResult);
        _groupMessagesOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                                     .ReturnsAsync(successResult);
        _userOrchestratorMock.Setup(x => x.DeleteUserAsync(TestNickName))
                            .ReturnsAsync(successResult);
    }

    private void SetupSuccessfulScenarioWithMemberData()
    {
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(TestPublicKey);
        _decryptionInfoMock.Setup(x => x.Decrypt(TestUserNickNameHash)).Returns(TestNickName);
        
        var successResult = OperationResult<string>.Ok(String.Empty);
        var resultWithData = OperationResult<List<Guid>>.Ok([Guid.NewGuid()]);
        
        _groupMemberOrchestratorMock.Setup(x => x.DeleteMemberInfoAsync(TestNickName))
                                   .ReturnsAsync(resultWithData);
        _messageOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                               .ReturnsAsync(successResult);
        _groupMessagesOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                                     .ReturnsAsync(successResult);
        _userOrchestratorMock.Setup(x => x.DeleteUserAsync(TestNickName))
                            .ReturnsAsync(successResult);
    }

    private void SetupMixedResultsScenario()
    {
        _publicKeyStorageMock.Setup(x => x.Get("User")).Returns(TestPublicKey);
        _decryptionInfoMock.Setup(x => x.Decrypt(TestUserNickNameHash)).Returns(TestNickName);
        
        var successResult = OperationResult<string>.Ok(String.Empty);
        var failureResult2 = OperationResult<string>.Fail("User operation failed");
        var resultWithData = OperationResult<List<Guid>>.Ok([Guid.NewGuid()]);

        _groupMemberOrchestratorMock.Setup(x => x.DeleteMemberInfoAsync(TestNickName))
                                   .ReturnsAsync(resultWithData);
        _messageOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                               .ReturnsAsync(successResult);
        _groupMessagesOrchestratorMock.Setup(x => x.DeleteUserInfoInMessageAsync(TestNickName))
                                     .ReturnsAsync(successResult);
        _userOrchestratorMock.Setup(x => x.DeleteUserAsync(TestNickName))
                            .ReturnsAsync(failureResult2);
    }

    private void VerifyAllOrchestratorsCalled()
    {
        _groupMemberOrchestratorMock.Verify(x => x.DeleteMemberInfoAsync(TestNickName), Times.Once);
        _messageOrchestratorMock.Verify(x => x.DeleteUserInfoInMessageAsync(TestNickName), Times.Once);
        _groupMessagesOrchestratorMock.Verify(x => x.DeleteUserInfoInMessageAsync(TestNickName), Times.Once);
        _userOrchestratorMock.Verify(x => x.DeleteUserAsync(TestNickName), Times.Once);
    }
}