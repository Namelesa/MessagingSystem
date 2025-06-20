using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using FluentValidation.Results;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Messages;
using MessagingSystem.Services.Messaging.Core;
using MessagingSystem.Services.Messaging.Core.Messages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Messages;

public class TestMessageOrchestrator(
    IHasher hasher,
    IMapper mapper,
    IEncryptionInfo encryptionInfo,
    IDecryptionInfo decryptionInfo,
    IValidator<TestCreateDto> createValidator,
    IValidator<EditMessageDto> editValidator,
    IMessageRepository<TestMessage> messageRepository)
    : MessageOrchestratorBase<TestMessage, TestCreateDto>(hasher, mapper, encryptionInfo, decryptionInfo,
        createValidator, editValidator, messageRepository)
{
    protected override void ApplyHashAndSet(TestCreateDto dto, TestMessage message)
    {
        message.SenderHash = "hashed_" + dto.Sender;
        message.RecipientHash = "hashed_" + dto.Recipient;
    }
    protected override void EditMessage(TestMessage message, EditMessageDto editDto)
    {
        message.Content = editDto.Content;
        message.LastModified = DateTime.UtcNow;
    }
}

public class TestCreateDto
{
    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
public class TestMessage : IMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime SendTime { get; set; } = DateTime.UtcNow;
    public string Content { get; set; } = string.Empty;
    public string SenderHash { get; set; } = string.Empty;
    public string RecipientHash { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
}

public class MessageOrchestratorBaseTests
{
    private readonly Mock<IHasher> _hasherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IEncryptionInfo> _encryptionInfoMock;
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock;
    private readonly Mock<IValidator<TestCreateDto>> _createValidatorMock;
    private readonly Mock<IValidator<EditMessageDto>> _editValidatorMock;
    private readonly Mock<IMessageRepository<TestMessage>> _messageRepositoryMock;
    private readonly TestMessageOrchestrator _orchestrator;

    public MessageOrchestratorBaseTests()
    {
        _hasherMock = new Mock<IHasher>();
        _mapperMock = new Mock<IMapper>();
        _encryptionInfoMock = new Mock<IEncryptionInfo>();
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        _createValidatorMock = new Mock<IValidator<TestCreateDto>>();
        _editValidatorMock = new Mock<IValidator<EditMessageDto>>();
        _messageRepositoryMock = new Mock<IMessageRepository<TestMessage>>();

        _orchestrator = new TestMessageOrchestrator(
            _hasherMock.Object,
            _mapperMock.Object,
            _encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _createValidatorMock.Object,
            _editValidatorMock.Object,
            _messageRepositoryMock.Object
        );
    }

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_ValidDto_ReturnsSuccessResult()
    {
        // Arrange
        var createDto = new TestCreateDto { Sender = "Alice", Recipient = "Bob", Content = "Hello" };
        var message = new TestMessage { Id = Guid.NewGuid(), SendTime = DateTime.UtcNow };
        var validationResult = new ValidationResult();

        _createValidatorMock.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(validationResult);
        _mapperMock.Setup(m => m.Map<TestMessage>(createDto))
            .Returns(message);
        _messageRepositoryMock.Setup(r => r.CreateMessageAsync(It.IsAny<TestMessage>()))
            .ReturnsAsync(message);

        // Act
        var result = await _orchestrator.SendMessageAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(message.Id, result.Data.MessageId);
        Assert.Equal(message.SendTime, result.Data.SentTime);

        _encryptionInfoMock.Verify(e => e.EncryptObjectStrings(message), Times.Once);
        _messageRepositoryMock.Verify(r => r.CreateMessageAsync(message), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_InvalidDto_ReturnsFailureResult()
    {
        // Arrange
        var createDto = new TestCreateDto();
        var validationFailures = new List<ValidationFailure>
        {
            new("Sender", "Sender is required"),
            new("Content", "Content cannot be empty")
        };
        var validationResult = new ValidationResult(validationFailures);

        _createValidatorMock.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _orchestrator.SendMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender is required", result.Message);
        Assert.Contains("Content cannot be empty", result.Message);
        Assert.Null(result.Data);

        _mapperMock.Verify(m => m.Map<TestMessage>(It.IsAny<TestCreateDto>()), Times.Never);
        _messageRepositoryMock.Verify(r => r.CreateMessageAsync(It.IsAny<TestMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_RepositoryReturnsNull_ReturnsFailureResult()
    {
        // Arrange
        var createDto = new TestCreateDto { Sender = "Alice", Recipient = "Bob", Content = "Hello" };
        var message = new TestMessage();
        var validationResult = new ValidationResult();

        _createValidatorMock.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(validationResult);
        _mapperMock.Setup(m => m.Map<TestMessage>(createDto))
            .Returns(message);
        _messageRepositoryMock.Setup(r => r.CreateMessageAsync(It.IsAny<TestMessage>()))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.SendMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to send message", result.Message);
        Assert.Null(result.Data);
    }

    #endregion

    #region EditMessageAsync Tests

    [Fact]
    public async Task EditMessageAsync_ValidEdit_ReturnsSuccessResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var editDto = new EditMessageDto("Updated content");
        var existingMessage = new TestMessage { Id = messageId };
        var validationResult = new ValidationResult();

        _editValidatorMock.Setup(v => v.ValidateAsync(editDto, default))
            .ReturnsAsync(validationResult);
        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(existingMessage);
        _messageRepositoryMock.Setup(r => r.EditMessageAsync(existingMessage))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _orchestrator.EditMessageAsync(messageId, editDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(messageId.ToString(), result.Data);

        _encryptionInfoMock.Verify(e => e.EncryptObjectStrings(existingMessage), Times.Once);
        _messageRepositoryMock.Verify(r => r.EditMessageAsync(existingMessage), Times.Once);
    }

    [Fact]
    public async Task EditMessageAsync_InvalidDto_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var editDto = new EditMessageDto("");
        var validationFailures = new List<ValidationFailure>
        {
            new("Content", "Content is required")
        };
        var validationResult = new ValidationResult(validationFailures);

        _editValidatorMock.Setup(v => v.ValidateAsync(editDto, default))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _orchestrator.EditMessageAsync(messageId, editDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Content is required", result.Message);

        _messageRepositoryMock.Verify(r => r.FindMessageByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EditMessageAsync_MessageNotFound_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var editDto = new EditMessageDto("Updated content");
        var validationResult = new ValidationResult();

        _editValidatorMock.Setup(v => v.ValidateAsync(editDto, default))
            .ReturnsAsync(validationResult);
        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.EditMessageAsync(messageId, editDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Message not found", result.Message);

        _messageRepositoryMock.Verify(r => r.EditMessageAsync(It.IsAny<TestMessage>()), Times.Never);
    }

    #endregion

    #region DeleteMessageAsync Tests

    [Fact]
    public async Task DeleteMessageAsync_ExistingMessage_ReturnsSuccessResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new TestMessage { Id = messageId };

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(existingMessage);
        _messageRepositoryMock.Setup(r => r.DeleteMessageAsync(existingMessage))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _orchestrator.DeleteMessageAsync(messageId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(messageId.ToString(), result.Data);

        _messageRepositoryMock.Verify(r => r.DeleteMessageAsync(existingMessage), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_MessageNotFound_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.DeleteMessageAsync(messageId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Message not found", result.Message);

        _messageRepositoryMock.Verify(r => r.DeleteMessageAsync(It.IsAny<TestMessage>()), Times.Never);
    }

    #endregion

    #region SoftDeleteMessageAsync Tests

    [Fact]
    public async Task SoftDeleteMessageAsync_ExistingMessage_ReturnsSuccessResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new TestMessage { Id = messageId };

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(existingMessage);
        _messageRepositoryMock.Setup(r => r.SoftDeleteMessageAsync(existingMessage))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _orchestrator.SoftDeleteMessageAsync(messageId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(messageId.ToString(), result.Data);

        _messageRepositoryMock.Verify(r => r.SoftDeleteMessageAsync(existingMessage), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteMessageAsync_MessageNotFound_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.SoftDeleteMessageAsync(messageId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Message not found", result.Message);

        _messageRepositoryMock.Verify(r => r.SoftDeleteMessageAsync(It.IsAny<TestMessage>()), Times.Never);
    }

    #endregion

    #region FindMessageByIdAsync Tests

    [Fact]
    public async Task FindMessageByIdAsync_ExistingMessage_ReturnsSuccessResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingMessage = new TestMessage { Id = messageId };

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(existingMessage);

        // Act
        var result = await _orchestrator.FindMessageByIdAsync(messageId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(messageId.ToString(), result.Data);
    }

    [Fact]
    public async Task FindMessageByIdAsync_MessageNotFound_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.FindMessageByIdAsync(messageId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Message not found", result.Message);
    }

    #endregion

    #region ReplyForMessageAsync Tests

    [Fact]
    public async Task ReplyForMessageAsync_ExistingMessage_ReturnsSuccessResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var replyId = Guid.NewGuid();
        var existingMessage = new TestMessage { Id = messageId };
        var replyMessage = new TestMessage { Id = replyId };

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(existingMessage);
        _messageRepositoryMock.Setup(r => r.ReplyMessageAsync(replyId, existingMessage))
            .ReturnsAsync(replyMessage);

        // Act
        var result = await _orchestrator.ReplyForMessageAsync(messageId, replyId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(replyMessage, result.Data);

        _decryptionInfoMock.Verify(d => d.DecryptObjectStrings(replyMessage), Times.Once);
    }

    [Fact]
    public async Task ReplyForMessageAsync_MessageNotFound_ReturnsFailureResult()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var replyId = Guid.NewGuid();

        _messageRepositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((TestMessage?)null);

        // Act
        var result = await _orchestrator.ReplyForMessageAsync(messageId, replyId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Can not find message", result.Message);

        _messageRepositoryMock.Verify(r => r.ReplyMessageAsync(It.IsAny<Guid>(), It.IsAny<TestMessage>()), Times.Never);
    }

    #endregion

    #region DeleteUserInfoInMessageAsync Tests

    [Fact]
    public async Task DeleteUserInfoInMessageAsync_SuccessfulDeletion_ReturnsSuccessResult()
    {
        // Arrange
        var userHash = "user_hash_123";
        var affectedRows = 5;

        _messageRepositoryMock.Setup(r => r.DeleteUserHashesAsync(userHash))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.DeleteUserInfoInMessageAsync(userHash);

        // Assert
        Assert.True(result.Success);
        Assert.Contains($"Delete successful. Rows affected: {affectedRows}", result.Data);
    }

    [Fact]
    public async Task DeleteUserInfoInMessageAsync_NoRowsDeleted_ReturnsFailureResult()
    {
        // Arrange
        var userHash = "nonexistent_hash";

        _messageRepositoryMock.Setup(r => r.DeleteUserHashesAsync(userHash))
            .ReturnsAsync(-1);

        // Act
        var result = await _orchestrator.DeleteUserInfoInMessageAsync(userHash);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No rows were deleted. Possibly invalid user hash.", result.Message);
    }

    [Fact]
    public async Task DeleteUserInfoInMessageAsync_ThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var userHash = "user_hash_123";
        var exceptionMessage = "Database connection failed";

        _messageRepositoryMock.Setup(r => r.DeleteUserHashesAsync(userHash))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _orchestrator.DeleteUserInfoInMessageAsync(userHash);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Exception occurred: {exceptionMessage}", result.Message);
    }

    #endregion

    #region UpdateUserInfoInMessageAsync Tests

    [Fact]
    public async Task UpdateUserInfoInMessageAsync_SuccessfulUpdate_ReturnsSuccessResult()
    {
        // Arrange
        var newNickName = "NewNick";
        var oldUserHashName = "old_hash";
        var newUserHash = "hashed_NewNick";
        var encryptedNickName = "encrypted_NewNick";
        var affectedRows = 3;

        _hasherMock.Setup(h => h.Hash(newNickName))
            .Returns(newUserHash);
        _encryptionInfoMock.Setup(e => e.Encrypt(newNickName))
            .Returns(encryptedNickName);
        _messageRepositoryMock.Setup(r => r.UpdateUserHashesAsync(oldUserHashName, encryptedNickName, newUserHash))
            .ReturnsAsync(affectedRows);

        // Act
        var result = await _orchestrator.UpdateUserInfoInMessageAsync(newNickName, oldUserHashName);

        // Assert
        Assert.True(result.Success);
        Assert.Contains($"Update successful. Rows affected: {affectedRows}", result.Data);
    }

    [Fact]
    public async Task UpdateUserInfoInMessageAsync_NoRowsUpdated_ReturnsFailureResult()
    {
        // Arrange
        var newNickName = "NewNick";
        var oldUserHashName = "nonexistent_hash";

        _hasherMock.Setup(h => h.Hash(newNickName))
            .Returns("new_hash");
        _encryptionInfoMock.Setup(e => e.Encrypt(newNickName))
            .Returns("encrypted_nick");
        _messageRepositoryMock.Setup(r => r.UpdateUserHashesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(-1);

        // Act
        var result = await _orchestrator.UpdateUserInfoInMessageAsync(newNickName, oldUserHashName);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No rows were updated. Possibly invalid user hash.", result.Message);
    }

    [Fact]
    public async Task UpdateUserInfoInMessageAsync_ThrowsException_ReturnsFailureResult()
    {
        // Arrange
        var newNickName = "NewNick";
        var oldUserHashName = "old_hash";
        var exceptionMessage = "Database error";

        _hasherMock.Setup(h => h.Hash(newNickName))
            .Returns("new_hash");
        _encryptionInfoMock.Setup(e => e.Encrypt(newNickName))
            .Returns("encrypted_nick");
        _messageRepositoryMock.Setup(r => r.UpdateUserHashesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _orchestrator.UpdateUserInfoInMessageAsync(newNickName, oldUserHashName);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"Exception occurred: {exceptionMessage}", result.Message);
    }

    #endregion

    #region FindMessagesAsync Tests

    [Fact]
    public async Task FindMessagesAsync_WithFilter_ReturnsDecryptedMessages()
    {
        // Arrange
        var messageFilter = new MessageFilter
        {
            Sender = "Alice",
            Recipient = "Bob",
            Date = DateTime.Today
        };
        var senderHash = "hashed_Alice";
        var recipientHash = "hashed_Bob";
        var encryptedMessages = new List<TestMessage>
        {
            new() { Id = Guid.NewGuid(), Content = "encrypted_content_1" },
            new() { Id = Guid.NewGuid(), Content = "encrypted_content_2" }
        };

        _hasherMock.Setup(h => h.Hash("Alice")).Returns(senderHash);
        _hasherMock.Setup(h => h.Hash("Bob")).Returns(recipientHash);
        _messageRepositoryMock.Setup(r => r.FindMessagesAsync(It.IsAny<MessageFilter>()))
            .ReturnsAsync(encryptedMessages);

        // Act
        var result = await _orchestrator.FindMessagesAsync(messageFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        _messageRepositoryMock.Verify(r => r.FindMessagesAsync(It.Is<MessageFilter>(f =>
            f.Sender == senderHash &&
            f.Recipient == recipientHash &&
            f.Date == messageFilter.Date)), Times.Once);
        
        _decryptionInfoMock.Verify(d => d.DecryptObjectStrings(It.IsAny<TestMessage>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FindMessagesAsync_RepositoryReturnsNull_ReturnsEmptyList()
    {
        // Arrange
        var messageFilter = new MessageFilter { Sender = "Alice" };

        _hasherMock.Setup(h => h.Hash("Alice")).Returns("hashed_Alice");
        _messageRepositoryMock.Setup(r => r.FindMessagesAsync(It.IsAny<MessageFilter>()))
            .ReturnsAsync((List<TestMessage>?)null);

        // Act
        var result = await _orchestrator.FindMessagesAsync(messageFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FindMessagesAsync_WithNullSenderAndRecipient_DoesNotHashNullValues()
    {
        // Arrange
        var messageFilter = new MessageFilter { Date = DateTime.Today };
        var messages = new List<TestMessage>();

        _messageRepositoryMock.Setup(r => r.FindMessagesAsync(It.IsAny<MessageFilter>()))
            .ReturnsAsync(messages);

        // Act
        await _orchestrator.FindMessagesAsync(messageFilter);

        // Assert
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        _messageRepositoryMock.Verify(r => r.FindMessagesAsync(It.Is<MessageFilter>(f =>
            f.Sender == null &&
            f.Recipient == null &&
            f.Date == messageFilter.Date)), Times.Once);
    }

    #endregion
    
    #region Model Tests

    [Fact]
    public void TestMessage_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var message = new TestMessage();

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.True(message.SendTime <= DateTime.UtcNow);
        Assert.Equal(string.Empty, message.Content);
        Assert.Equal(string.Empty, message.SenderHash);
        Assert.Equal(string.Empty, message.RecipientHash);
        Assert.Null(message.LastModified);
    }

    [Fact]
    public void TestMessage_Properties_CanBeSetAndGet()
    {
        // Arrange
        var message = new TestMessage();
        var testId = Guid.NewGuid();
        var testSendTime = DateTime.UtcNow.AddMinutes(-5);
        var testContent = "Test content";
        var testSenderHash = "sender_hash";
        var testRecipientHash = "recipient_hash";
        var testLastModified = DateTime.UtcNow;

        // Act
        message.Id = testId;
        message.SendTime = testSendTime;
        message.Content = testContent;
        message.SenderHash = testSenderHash;
        message.RecipientHash = testRecipientHash;
        message.LastModified = testLastModified;

        // Assert
        Assert.Equal(testId, message.Id);
        Assert.Equal(testSendTime, message.SendTime);
        Assert.Equal(testContent, message.Content);
        Assert.Equal(testSenderHash, message.SenderHash);
        Assert.Equal(testRecipientHash, message.RecipientHash);
        Assert.Equal(testLastModified, message.LastModified);
    }

    [Fact]
    public void TestCreateDto_DefaultConstructor_SetsEmptyStrings()
    {
        // Act
        var dto = new TestCreateDto();

        // Assert
        Assert.Equal(string.Empty, dto.Sender);
        Assert.Equal(string.Empty, dto.Recipient);
        Assert.Equal(string.Empty, dto.Content);
    }

    [Fact]
    public void TestCreateDto_Properties_CanBeSetAndGet()
    {
        // Arrange
        var dto = new TestCreateDto();
        var testSender = "Alice";
        var testRecipient = "Bob";
        var testContent = "Hello World";

        // Act
        dto.Sender = testSender;
        dto.Recipient = testRecipient;
        dto.Content = testContent;

        // Assert
        Assert.Equal(testSender, dto.Sender);
        Assert.Equal(testRecipient, dto.Recipient);
        Assert.Equal(testContent, dto.Content);
    }

    #endregion

    #region TestMessageOrchestrator Specific Tests

    [Fact]
    public void TestMessageOrchestrator_ApplyHashAndSet_SetsCorrectHashes()
    {
        // Arrange
        var dto = new TestCreateDto 
        { 
            Sender = "Alice", 
            Recipient = "Bob", 
            Content = "Test message" 
        };
        var message = new TestMessage();

        // Act
        var method = typeof(TestMessageOrchestrator).GetMethod("ApplyHashAndSet", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_orchestrator, [dto, message]);

        // Assert
        Assert.Equal("hashed_Alice", message.SenderHash);
        Assert.Equal("hashed_Bob", message.RecipientHash);
    }

    [Fact]
    public void TestMessageOrchestrator_EditMessage_UpdatesContentAndTime()
    {
        // Arrange
        var message = new TestMessage 
        { 
            Content = "Original content",
            LastModified = null
        };
        var editDto = new EditMessageDto("Updated content");
        var beforeEdit = DateTime.UtcNow;

        // Act
        var method = typeof(TestMessageOrchestrator).GetMethod("EditMessage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(_orchestrator, [message, editDto]);

        // Assert
        Assert.Equal("Updated content", message.Content);
        Assert.NotNull(message.LastModified);
        Assert.True(message.LastModified >= beforeEdit);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task SendMessageAsync_EmptyStringsInDto_HandlesGracefully()
    {
        // Arrange
        var createDto = new TestCreateDto 
        { 
            Sender = "", 
            Recipient = "", 
            Content = "" 
        };
        var message = new TestMessage();
        var validationResult = new ValidationResult();

        _createValidatorMock.Setup(v => v.ValidateAsync(createDto, default))
            .ReturnsAsync(validationResult);
        _mapperMock.Setup(m => m.Map<TestMessage>(createDto))
            .Returns(message);
        _messageRepositoryMock.Setup(r => r.CreateMessageAsync(It.IsAny<TestMessage>()))
            .ReturnsAsync(message);

        // Act
        var result = await _orchestrator.SendMessageAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("hashed_", message.SenderHash);
        Assert.Equal("hashed_", message.RecipientHash);
    }

    [Fact]
    public async Task FindMessagesAsync_WithEmptyStrings_HashesEmptyStrings()
    {
        // Arrange
        var messageFilter = new MessageFilter
        {
            Sender = "",
            Recipient = ""
        };
        var messages = new List<TestMessage>();

        _hasherMock.Setup(h => h.Hash("")).Returns("hashed_empty");
        _messageRepositoryMock.Setup(r => r.FindMessagesAsync(It.IsAny<MessageFilter>()))
            .ReturnsAsync(messages);

        // Act
        await _orchestrator.FindMessagesAsync(messageFilter);

        // Assert
        _hasherMock.Verify(h => h.Hash(""), Times.Exactly(2)); // Для Sender и Recipient
        _messageRepositoryMock.Verify(r => r.FindMessagesAsync(It.Is<MessageFilter>(f =>
            f.Sender == "hashed_empty" &&
            f.Recipient == "hashed_empty")), Times.Once);
    }

    #endregion
}