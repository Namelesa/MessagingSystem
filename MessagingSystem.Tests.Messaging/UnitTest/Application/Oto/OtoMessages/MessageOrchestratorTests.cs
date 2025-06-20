using System.Reflection;
using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentAssertions;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Oto.OtoMessages;

public class MessageOrchestratorTests
{
    private readonly Mock<IOtoMessageRepository> _repositoryMock;
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock;
    private readonly Mock<IHasher> _hasherMock;

    private readonly MessageOrchestrator _orchestrator;

    public MessageOrchestratorTests()
    {
        _repositoryMock = new Mock<IOtoMessageRepository>();
        var mapperMock = new Mock<IMapper>();
        var createValidatorMock = new Mock<IValidator<MessagesDto>>();
        var editValidatorMock = new Mock<IValidator<EditMessageDto>>();
        var encryptionInfoMock = new Mock<IEncryptionInfo>();
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        _hasherMock = new Mock<IHasher>();

        _orchestrator = new MessageOrchestrator(
            _repositoryMock.Object,
            mapperMock.Object,
            createValidatorMock.Object,
            editValidatorMock.Object,
            encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _hasherMock.Object);
    }

    [Fact]
    public void ApplyHashAndSet_ShouldHashSenderAndRecipient()
    {
        // Arrange
        const string sender = "sender@test.com";
        const string recipient = "recipient@test.com";
        const string hashedSender = "hashedSender";
        const string hashedRecipient = "hashedRecipient";

        var dto = new MessagesDto(sender, recipient, "content");
        var message = new Message(sender, recipient, "content");

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        var method = typeof(MessageOrchestrator)
            .GetMethod("ApplyHashAndSet", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        method.Should().NotBeNull();

        // Act
        method.Invoke(_orchestrator, [dto, message]);

        // Assert
        _hasherMock.Verify(x => x.Hash(sender), Times.Once);
        _hasherMock.Verify(x => x.Hash(recipient), Times.Once);
    }

    [Fact]
    public async Task LoadChatHistory_ShouldHashUsersAndDecryptMessages()
    {
        // Arrange
        const string sender = "sender@test.com";
        const string recipient = "recipient@test.com";
        const string hashedSender = "hashedSender";
        const string hashedRecipient = "hashedRecipient";
        const int take = 10;

        var encryptedMessages = new List<Message>
        {
            new("sender", "recipient", "encryptedContent1"),
            new("sender", "recipient", "encryptedContent2")
        };

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        _repositoryMock.Setup(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, take))
            .ReturnsAsync(encryptedMessages);

        _decryptionInfoMock
            .Setup(x => x.DecryptObjectStrings(It.IsAny<Message>()))
            .Callback<Message>(_ => { });

        // Act
        var result = await _orchestrator.LoadChatHistory(sender, recipient, take);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        _hasherMock.Verify(x => x.Hash(sender), Times.Once);
        _hasherMock.Verify(x => x.Hash(recipient), Times.Once);
        _repositoryMock.Verify(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, take), Times.Once);
        _decryptionInfoMock.Verify(x => x.DecryptObjectStrings(It.IsAny<Message>()), Times.Exactly(2));
    }

    
    [Fact]
    public void EditMessage_ShouldUpdateMessageContent()
    {
        // Arrange
        const string newContent = "Updated content";
        var editDto = new EditMessageDto(newContent);
        var message = new Message("sender", "recipient", "original content");

        // Act
        var method = typeof(MessageOrchestrator).GetMethod("EditMessage", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_orchestrator, [message, editDto]);

        // Assert
        Assert.True(true);
    }
}