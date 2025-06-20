using AutoMapper;
using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Oto.OtoChats;

public class ChatOrchestratorTests
{
    private readonly Mock<IChatRepository> _chatRepositoryMock;
    private readonly Mock<IHasher> _hasherMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IDecryptionInfo> _decryptionInfoMock;
    private readonly ChatOrchestrator _orchestrator;

    public ChatOrchestratorTests()
    {
        _chatRepositoryMock = new Mock<IChatRepository>();
        _hasherMock = new Mock<IHasher>();
        _mapperMock = new Mock<IMapper>();
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        
        _orchestrator = new ChatOrchestrator(
            _chatRepositoryMock.Object,
            _hasherMock.Object,
            _mapperMock.Object,
            _decryptionInfoMock.Object);
    }

    [Fact]
    public async Task GetChatsAsync_WhenRepositoryReturnsNull_ShouldReturnEmptyList()
    {
        // Arrange
        const string userName = "testUser";
        const string hashedUserName = "hashedUser";
        
        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync((List<Chat>?)null);

        // Act
        var result = await _orchestrator.GetChatsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _hasherMock.Verify(x => x.Hash(userName), Times.Once);
        _chatRepositoryMock.Verify(x => x.GetChatsAsync(hashedUserName), Times.Once);
    }

    [Fact]
    public async Task GetChatsAsync_WhenRepositoryReturnsChats_ShouldDecryptAndMapChats()
    {
        // Arrange
        const string userName = "testUser";
        const string hashedUserName = "hashedUser";
        const string encryptedNickName = "encryptedNick";
        const string decryptedNickName = "decryptedNick";
        
        var encryptedChats = new List<Chat>
        {
            new() { NickName = encryptedNickName, Image = "image1.jpg" },
            new() { NickName = "encryptedNick2", Image = "image2.jpg" }
        };

        var expectedDtos = new List<ChatDto>
        {
            new() { NickName = decryptedNickName, Image = "image1.jpg" },
            new() { NickName = "decryptedNick2", Image = "image2.jpg" }
        };

        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync(encryptedChats);
        _decryptionInfoMock.Setup(x => x.Decrypt(encryptedNickName)).Returns(decryptedNickName);
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedNick2")).Returns("decryptedNick2");
        _mapperMock.Setup(x => x.Map<List<ChatDto>>(encryptedChats)).Returns(expectedDtos);

        // Act
        var result = await _orchestrator.GetChatsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDtos.Count, result.Count);
        Assert.Equal(expectedDtos, result);
        
        _hasherMock.Verify(x => x.Hash(userName), Times.Once);
        _chatRepositoryMock.Verify(x => x.GetChatsAsync(hashedUserName), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Exactly(2));
        _mapperMock.Verify(x => x.Map<List<ChatDto>>(encryptedChats), Times.Once);
    }

    [Fact]
    public async Task GetChatsAsync_ShouldDecryptEachChatNickName()
    {
        // Arrange
        const string userName = "testUser";
        const string hashedUserName = "hashedUser";
        
        var encryptedChats = new List<Chat>
        {
            new() { NickName = "encrypted1", Image = "image1.jpg" },
            new() { NickName = "encrypted2", Image = "image2.jpg" },
            new() { NickName = "encrypted3", Image = "image3.jpg" }
        };

        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync(encryptedChats);
        _decryptionInfoMock.Setup(x => x.Decrypt("encrypted1")).Returns("decrypted1");
        _decryptionInfoMock.Setup(x => x.Decrypt("encrypted2")).Returns("decrypted2");
        _decryptionInfoMock.Setup(x => x.Decrypt("encrypted3")).Returns("decrypted3");
        _mapperMock.Setup(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>())).Returns(new List<ChatDto>());

        // Act
        await _orchestrator.GetChatsAsync(userName);

        // Assert
        _decryptionInfoMock.Verify(x => x.Decrypt("encrypted1"), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt("encrypted2"), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt("encrypted3"), Times.Once);
        
        Assert.Equal("decrypted1", encryptedChats[0].NickName);
        Assert.Equal("decrypted2", encryptedChats[1].NickName);
        Assert.Equal("decrypted3", encryptedChats[2].NickName);
    }
}