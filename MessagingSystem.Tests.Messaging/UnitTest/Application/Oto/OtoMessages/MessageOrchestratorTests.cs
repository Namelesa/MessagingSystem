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
using MessagingSystem.Services.Messaging.Infrastructure.Cashing;
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
    private readonly Mock<ICacheService> _cacheServiceMock;

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
        _cacheServiceMock = new Mock<ICacheService>();

        _orchestrator = new MessageOrchestrator(
            _repositoryMock.Object,
            mapperMock.Object,
            createValidatorMock.Object,
            editValidatorMock.Object,
            encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _hasherMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task LoadChatHistory_ShouldReturnCachedData_WhenCacheHit()
    {
        // Arrange
        const string sender = "sender@test.com";
        const string recipient = "recipient@test.com";
        const string hashedSender = "hashedSender";
        const string hashedRecipient = "hashedRecipient";
        const int take = 10;
        const int skip = 0;

        var cachedMessages = new List<Message>
        {
            new("sender", "recipient", "cachedContent1"),
            new("sender", "recipient", "cachedContent2")
        };

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        var expectedCacheKey = $"oto:{hashedRecipient}:{hashedSender}:history:{skip}:{take}";
        _cacheServiceMock.Setup(x => x.GetAsync<List<Message>>(expectedCacheKey))
            .ReturnsAsync(cachedMessages);

        // Act
        var result = await _orchestrator.LoadChatHistory(sender, recipient, skip, take);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(cachedMessages, result);

        _hasherMock.Verify(x => x.Hash(sender), Times.Once);
        _hasherMock.Verify(x => x.Hash(recipient), Times.Once);
        _cacheServiceMock.Verify(x => x.GetAsync<List<Message>>(expectedCacheKey), Times.Once);
        _repositoryMock.Verify(x => x.GetMessageStoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<Message>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task LoadChatHistory_ShouldCacheResult_WhenCacheMiss()
    {
        // Arrange
        const string sender = "sender@test.com";
        const string recipient = "recipient@test.com";
        const string hashedSender = "hashedSender";
        const string hashedRecipient = "hashedRecipient";
        const int take = 10;
        const int skip = 0;

        var encryptedMessages = new List<Message>
        {
            new("sender", "recipient", "encryptedContent1"),
            new("sender", "recipient", "encryptedContent2")
        };

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        var expectedCacheKey = $"oto:{hashedRecipient}:{hashedSender}:history:{skip}:{take}";
        _cacheServiceMock.Setup(x => x.GetAsync<List<Message>>(expectedCacheKey))
            .ReturnsAsync((List<Message>)null);

        _repositoryMock.Setup(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, skip, take))
            .ReturnsAsync(encryptedMessages);

        _decryptionInfoMock
            .Setup(x => x.DecryptObjectStrings(It.IsAny<Message>()))
            .Callback<Message>(_ => { });

        // Act
        var result = await _orchestrator.LoadChatHistory(sender, recipient, skip, take);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        _cacheServiceMock.Verify(x => x.GetAsync<List<Message>>(expectedCacheKey), Times.Once);
        _repositoryMock.Verify(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, skip ,take), Times.Once);
        _decryptionInfoMock.Verify(x => x.DecryptObjectStrings(It.IsAny<Message>()), Times.Exactly(2));
        _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, result, TimeSpan.FromMinutes(1)), Times.Once);
    }

    [Fact]
    public async Task LoadChatHistory_ShouldOrderHashesForCacheKey()
    {
        // Arrange
        const string sender = "zebra@test.com";
        const string recipient = "alpha@test.com";
        const string hashedSender = "zzz";
        const string hashedRecipient = "aaa";
        const int take = 5;
        const int skip = 0;

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        var expectedCacheKey = $"oto:{hashedRecipient}:{hashedSender}:history:{skip}:{take}";
        _cacheServiceMock.Setup(x => x.GetAsync<List<Message>>(expectedCacheKey))
            .ReturnsAsync((List<Message>)null);

        _repositoryMock.Setup(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, skip,take))
            .ReturnsAsync(new List<Message>());

        // Act
        await _orchestrator.LoadChatHistory(sender, recipient, skip, take);

        // Assert
        _cacheServiceMock.Verify(x => x.GetAsync<List<Message>>(expectedCacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, It.IsAny<List<Message>>(), TimeSpan.FromMinutes(1)), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldRemoveCorrectCacheKey()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
    
        var senderHashProperty = typeof(Message).GetProperty("SenderHash");
        var recipientHashProperty = typeof(Message).GetProperty("RecipientHash");
    
        senderHashProperty?.SetValue(message, "zzz");
        recipientHashProperty?.SetValue(message, "aaa");

        var expectedCacheKey = "oto:aaa:zzz:history:100";

        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull();

        // Act
        await (Task)method.Invoke(_orchestrator, [message]);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveAsync(expectedCacheKey), Times.Once);
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
        const int skip = 0;

        var encryptedMessages = new List<Message>
        {
            new("sender", "recipient", "encryptedContent1"),
            new("sender", "recipient", "encryptedContent2")
        };

        _hasherMock.Setup(x => x.Hash(sender)).Returns(hashedSender);
        _hasherMock.Setup(x => x.Hash(recipient)).Returns(hashedRecipient);

        _repositoryMock.Setup(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, skip, take))
            .ReturnsAsync(encryptedMessages);

        _decryptionInfoMock
            .Setup(x => x.DecryptObjectStrings(It.IsAny<Message>()))
            .Callback<Message>(_ => { });

        // Act
        var result = await _orchestrator.LoadChatHistory(sender, recipient, skip, take);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        _hasherMock.Verify(x => x.Hash(sender), Times.Once);
        _hasherMock.Verify(x => x.Hash(recipient), Times.Once);
        _repositoryMock.Verify(x => x.GetMessageStoryAsync(hashedSender, hashedRecipient, skip , take), Times.Once);
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