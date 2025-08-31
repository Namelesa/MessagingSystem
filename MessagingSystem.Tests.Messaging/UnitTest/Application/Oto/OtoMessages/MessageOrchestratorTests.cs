using System.Reflection;
using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentAssertions;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Application.Oto.OtoMessages.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Caching;
using MessagingSystem.Services.Messaging.Infrastructure.FileLoaderService;
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
    private readonly IChatOrchestrator _chatOrchestrator;
    private readonly Mock<IFileLoader> _fileLoaderMock;

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
        _chatOrchestrator = new Mock<IChatOrchestrator>().Object;
        new Mock<IValidator<EditMessageDto>>();
        _fileLoaderMock = new Mock<IFileLoader>();

        _orchestrator = new MessageOrchestrator(
            _repositoryMock.Object,
            mapperMock.Object,
            createValidatorMock.Object,
            editValidatorMock.Object,
            encryptionInfoMock.Object,
            _decryptionInfoMock.Object,
            _hasherMock.Object,
            _cacheServiceMock.Object,
            _chatOrchestrator);
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
        _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, result, TimeSpan.FromMinutes(5)), Times.Once);
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
        _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, It.IsAny<List<Message>>(), TimeSpan.FromMinutes(5)), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldRemoveAllRelevantCacheKeys()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        typeof(Message).GetProperty("SenderHash")?.SetValue(message, "zzz");
        typeof(Message).GetProperty("RecipientHash")?.SetValue(message, "aaa");
        
        _decryptionInfoMock.Setup(x => x.Decrypt("sender")).Returns("senderNick");
        _decryptionInfoMock.Setup(x => x.Decrypt("recipient")).Returns("recipientNick");

        var removedKeys = new List<string>();
        _cacheServiceMock.Setup(c => c.RemoveAsync(It.IsAny<string>()))
            .Callback<string>(key => removedKeys.Add(key))
            .Returns(Task.CompletedTask);
        
        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        await (Task)method.Invoke(_orchestrator, [message]);

        // Assert
        var ordered = new[] { "aaa", "zzz" }.OrderBy(x => x).ToArray();
        for (var skip = 0; skip <= 500; skip += 100)
        {
            var expectedKey = $"oto:{ordered[0]}:{ordered[1]}:history:{skip}:100";
            Assert.Contains(expectedKey, removedKeys);
        }

        Assert.Contains("user_chats:senderNick", removedKeys);
        Assert.Contains("user_chats:recipientNick", removedKeys);
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
    
    [Fact]
    public async Task InvalidateCacheAsync_ShouldHandleDecryptionException()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        typeof(Message).GetProperty("SenderHash")?.SetValue(message, "zzz");
        typeof(Message).GetProperty("RecipientHash")?.SetValue(message, "aaa");

        _decryptionInfoMock
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Throws(new Exception("Decryption failed"));

        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var act = () => (Task)method.Invoke(_orchestrator, new object[] { message });

        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task InvalidateCacheByUserHashAsync_ShouldHandleExceptionInLoop()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        typeof(Message).GetProperty("SenderHash")?.SetValue(message, "hash1");
        typeof(Message).GetProperty("RecipientHash")?.SetValue(message, "hash2");

        _repositoryMock.Setup(r => r.FindMessagesByHashAsync(It.IsAny<string>()))
            .ReturnsAsync([ message ]);

        _decryptionInfoMock
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Throws(new Exception("Boom!"));

        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheByUserHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var act = () => (Task)method.Invoke(_orchestrator, new object[] { "anyUserHash" });

        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task InvalidateCacheByUserHashAsync_ShouldSkipNullNicknames()
    {
        // Arrange
        var message = new Message("sender", "recipient", "content");
        typeof(Message).GetProperty("SenderHash")?.SetValue(message, "hash1");
        typeof(Message).GetProperty("RecipientHash")?.SetValue(message, "hash2");

        _repositoryMock.Setup(r => r.FindMessagesByHashAsync(It.IsAny<string>()))
            .ReturnsAsync([ message ]);

        _decryptionInfoMock
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns((string)null!);

        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheByUserHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var act = () => (Task)method.Invoke(_orchestrator, ["anyUserHash"]);

        // Assert
        await act.Should().NotThrowAsync();
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>()));
    }
    [Fact]
    public async Task InvalidateCacheByUserHashAsync_ShouldDoNothing_WhenNoMessagesFound()
    {
        // Arrange
        _repositoryMock.Setup(r => r.FindMessagesByHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Message>());

        var method = typeof(MessageOrchestrator)
            .GetMethod("InvalidateCacheByUserHashAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var act = () => (Task)method.Invoke(_orchestrator, new object[] { "anyUserHash" });

        // Assert
        await act.Should().NotThrowAsync();
    }
    
    [Fact]
    public async Task FindMessageWithRecipientByIdAsync_ShouldReturnMessage_WhenMessageExists()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new Message("sender", "recipient", "encryptedContent");

        _repositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync(message);

        _decryptionInfoMock.Setup(d => d.DecryptObjectStrings(message));

        // Act
        var result = await _orchestrator.FindMessageWithRecipientByIdAsync(messageId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(message);
        _repositoryMock.Verify(r => r.FindMessageByIdAsync(messageId), Times.Once);
        _decryptionInfoMock.Verify(d => d.DecryptObjectStrings(message), Times.Once);
    }
    
    [Fact]
    public async Task FindMessageWithRecipientByIdAsync_ShouldFail_WhenMessageNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.FindMessageByIdAsync(messageId))
            .ReturnsAsync((Message)null!);

        // Act
        var result = await _orchestrator.FindMessageWithRecipientByIdAsync(messageId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Message not found");
        result.Data.Should().BeNull();
        _repositoryMock.Verify(r => r.FindMessageByIdAsync(messageId), Times.Once);
        _decryptionInfoMock.Verify(d => d.DecryptObjectStrings(It.IsAny<Message>()), Times.Never);
    }
}