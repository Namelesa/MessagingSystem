using AutoMapper;
using Encryptor.Decryption;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Application.Oto.OtoChats.Dto;
using MessagingSystem.Services.Messaging.Core.Oto.OtoChats;
using MessagingSystem.Services.Messaging.Infrastructure.Cashing;
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
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly ChatOrchestrator _orchestrator;

    public ChatOrchestratorTests()
    {
        _chatRepositoryMock = new Mock<IChatRepository>();
        _hasherMock = new Mock<IHasher>();
        _mapperMock = new Mock<IMapper>();
        _decryptionInfoMock = new Mock<IDecryptionInfo>();
        _cacheServiceMock = new Mock<ICacheService>();
        
        _orchestrator = new ChatOrchestrator(
            _chatRepositoryMock.Object,
            _hasherMock.Object,
            _mapperMock.Object,
            _decryptionInfoMock.Object,
            _cacheServiceMock.Object
            );
    }

    [Fact]
    public async Task GetChatsAsync_WhenCacheHit_ShouldReturnCachedData()
    {
        // Arrange
        const string userName = "testUser";
        var expectedCacheKey = $"user_chats:{userName}";
    
        var cachedChats = new List<ChatDto>
        {
            new() { NickName = "cachedNick1", Image = "cachedImage1.jpg" },
            new() { NickName = "cachedNick2", Image = "cachedImage2.jpg" }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<List<ChatDto>>(expectedCacheKey))
            .ReturnsAsync(cachedChats);

        // Act
        var result = await _orchestrator.GetChatsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedChats.Count, result.Count);
        Assert.Equal(cachedChats[0].NickName, result[0].NickName);
        Assert.Equal(cachedChats[0].Image, result[0].Image);
        Assert.Equal(cachedChats[1].NickName, result[1].NickName);
        Assert.Equal(cachedChats[1].Image, result[1].Image);

        // Verify that cache was checked
        _cacheServiceMock.Verify(x => x.GetAsync<List<ChatDto>>(expectedCacheKey), Times.Once);
    
        // Verify that repository and other services were NOT called
        _hasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Never);
        _chatRepositoryMock.Verify(x => x.GetChatsAsync(It.IsAny<string>()), Times.Never);
        _decryptionInfoMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never);
        _mapperMock.Verify(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ChatDto>>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetChatsAsync_WhenCacheMiss_ShouldCacheResult()
    {
        // Arrange
        const string userName = "testUser";
        const string hashedUserName = "hashedUser";
        var expectedCacheKey = $"user_chats:{userName}";
    
        var encryptedChats = new List<Chat>
        {
            new() { NickName = "encrypted1", Image = "encryptedImage1.jpg" },
            new() { NickName = "encrypted2", Image = "encryptedImage2.jpg" }
        };

        var mappedResult = new List<ChatDto>
        {
            new() { NickName = "decrypted1", Image = "decryptedImage1.jpg" },
            new() { NickName = "decrypted2", Image = "decryptedImage2.jpg" }
        };

        // Setup cache miss
        _cacheServiceMock.Setup(x => x.GetAsync<List<ChatDto>>(expectedCacheKey))
            .ReturnsAsync((List<ChatDto>?)null);

        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync(encryptedChats);
    
        _decryptionInfoMock.Setup(x => x.Decrypt("encrypted1")).Returns("decrypted1");
        _decryptionInfoMock.Setup(x => x.Decrypt("encrypted2")).Returns("decrypted2");
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImage1.jpg")).Returns("decryptedImage1.jpg");
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImage2.jpg")).Returns("decryptedImage2.jpg");

        _mapperMock.Setup(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>())).Returns(mappedResult);

        // Act
        var result = await _orchestrator.GetChatsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mappedResult, result);

        // Verify cache operations
        _cacheServiceMock.Verify(x => x.GetAsync<List<ChatDto>>(expectedCacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, mappedResult, TimeSpan.FromMinutes(10)), Times.Once);

        // Verify other operations were called
        _hasherMock.Verify(x => x.Hash(userName), Times.Once);
        _chatRepositoryMock.Verify(x => x.GetChatsAsync(hashedUserName), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Exactly(4));
        _mapperMock.Verify(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>()), Times.Once);
    }

    [Fact]
    public async Task GetChatsAsync_ShouldDecryptBothNickNameAndImage()
    {
        // Arrange
        const string userName = "testUser";
        const string hashedUserName = "hashedUser";
        var expectedCacheKey = $"user_chats:{userName}";
    
        var encryptedChats = new List<Chat>
        {
            new() { NickName = "encryptedNick", Image = "encryptedImg" }
        };
    
        _cacheServiceMock.Setup(x => x.GetAsync<List<ChatDto>>(expectedCacheKey))
            .ReturnsAsync((List<ChatDto>?)null);

        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync(encryptedChats);
    
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedNick")).Returns("decryptedNick");
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImg")).Returns("decryptedImg");
    
        _mapperMock.Setup(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>())).Returns(new List<ChatDto>());

        // Act
        await _orchestrator.GetChatsAsync(userName);

        // Assert
        _decryptionInfoMock.Verify(x => x.Decrypt("encryptedNick"), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt("encryptedImg"), Times.Once);
    
        Assert.Equal("decryptedNick", encryptedChats[0].NickName);
        Assert.Equal("decryptedImg", encryptedChats[0].Image);
    }

    [Fact]
    public async Task GetChatsAsync_ShouldUseDifferentCacheKeysForDifferentUsers()
    {
        // Arrange
        const string userName1 = "user1";
        const string userName2 = "user2";
        var expectedCacheKey1 = $"user_chats:{userName1}";
        var expectedCacheKey2 = $"user_chats:{userName2}";

        var cachedResult1 = new List<ChatDto> { new() { NickName = "Chat1" } };
        var cachedResult2 = new List<ChatDto> { new() { NickName = "Chat2" } };

        _cacheServiceMock.Setup(x => x.GetAsync<List<ChatDto>>(expectedCacheKey1))
            .ReturnsAsync(cachedResult1);
        _cacheServiceMock.Setup(x => x.GetAsync<List<ChatDto>>(expectedCacheKey2))
            .ReturnsAsync(cachedResult2);

        // Act
        var result1 = await _orchestrator.GetChatsAsync(userName1);
        var result2 = await _orchestrator.GetChatsAsync(userName2);

        // Assert
        Assert.Equal(cachedResult1, result1);
        Assert.Equal(cachedResult2, result2);

        _cacheServiceMock.Verify(x => x.GetAsync<List<ChatDto>>(expectedCacheKey1), Times.Once);
        _cacheServiceMock.Verify(x => x.GetAsync<List<ChatDto>>(expectedCacheKey2), Times.Once);
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
        const string encryptedImage = "encryptedImage.jpg";
        const string decryptedImage = "decryptedImage.jpg";
    
        var encryptedChats = new List<Chat>
        {
            new() { NickName = encryptedNickName, Image = encryptedImage },
            new() { NickName = "encryptedNick2", Image = "encryptedImage2.jpg" }
        };
    

        var expectedDtos = new List<ChatDto>
        {
            new() { NickName = decryptedNickName, Image = decryptedImage },
            new() { NickName = "decryptedNick2", Image = "decryptedImage2.jpg" }
        };

        _hasherMock.Setup(x => x.Hash(userName)).Returns(hashedUserName);
        _chatRepositoryMock.Setup(x => x.GetChatsAsync(hashedUserName)).ReturnsAsync(encryptedChats);
    
        _decryptionInfoMock.Setup(x => x.Decrypt(encryptedNickName)).Returns(decryptedNickName);
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedNick2")).Returns("decryptedNick2");
    
        _decryptionInfoMock.Setup(x => x.Decrypt(encryptedImage)).Returns(decryptedImage);
        _decryptionInfoMock.Setup(x => x.Decrypt("encryptedImage2.jpg")).Returns("decryptedImage2.jpg");
    
        _mapperMock.Setup(x => x.Map<List<ChatDto>>(It.Is<List<Chat>>(chats => 
            chats.First().NickName == decryptedNickName && 
            chats.First().Image == decryptedImage)))
            .Returns(expectedDtos);

        // Act
        var result = await _orchestrator.GetChatsAsync(userName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDtos.Count, result.Count);
        Assert.Equal(expectedDtos[0].NickName, result[0].NickName);
        Assert.Equal(expectedDtos[0].Image, result[0].Image);
        Assert.Equal(expectedDtos[1].NickName, result[1].NickName);
        Assert.Equal(expectedDtos[1].Image, result[1].Image);
    
        // Verify calls
        _hasherMock.Verify(x => x.Hash(userName), Times.Once);
        _chatRepositoryMock.Verify(x => x.GetChatsAsync(hashedUserName), Times.Once);
        _decryptionInfoMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Exactly(4));
        _mapperMock.Verify(x => x.Map<List<ChatDto>>(It.IsAny<List<Chat>>()), Times.Once);
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
    
    [Fact]
    public async Task InvalidateUserChatsCacheAsync_ShouldCallRemoveAsyncWithCorrectKey()
    {
        // Arrange
        const string nickName = "testUser";
        var expectedCacheKey = $"user_chats:{nickName}";

        // Act
        await _orchestrator.InvalidateUserChatsCacheAsync(nickName);

        // Assert
        _cacheServiceMock.Verify(x => x.RemoveAsync(expectedCacheKey), Times.Once);
    }
}