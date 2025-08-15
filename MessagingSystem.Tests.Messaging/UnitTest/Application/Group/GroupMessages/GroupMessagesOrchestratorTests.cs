using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
using MessagingSystem.Services.Messaging.Infrastructure.Caching;
using MessagingSystem.Services.Messaging.Infrastructure.Hasher;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MessagingSystem.Tests.Messaging.UnitTest.Application.Group.GroupMessages
{
    public class GroupMessagesOrchestratorTests
    {
        private readonly Mock<IHasher> _hasherMock;
        private readonly Mock<IGroupMessagesRepository> _messageRepositoryMock;
        private readonly GroupMessagesOrchestrator _orchestrator;
        private readonly Mock<ICacheService> _cacheServiceMock;

        public GroupMessagesOrchestratorTests()
        {
            Mock<IMapper> mapperMock = new();
            _hasherMock = new Mock<IHasher>();
            Mock<IDecryptionInfo> decryptionInfoMock = new();
            Mock<IEncryptionInfo> encryptionInfoMock = new();
            Mock<IValidator<GroupMessageDto>> createValidatorMock = new();
            Mock<IValidator<EditMessageDto>> editValidatorMock = new();
            _messageRepositoryMock = new Mock<IGroupMessagesRepository>();
            _cacheServiceMock = new Mock<ICacheService>();

            _orchestrator = new GroupMessagesOrchestrator(
                mapperMock.Object,
                _hasherMock.Object,
                decryptionInfoMock.Object,
                encryptionInfoMock.Object,
                createValidatorMock.Object,
                editValidatorMock.Object,
                _messageRepositoryMock.Object,
                _cacheServiceMock.Object
            );
        }

        [Fact]
        public async Task InvalidateCacheAsync_ShouldRemoveCorrectCacheKey()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var message = new GroupMessage("sender", "content")
            {
                GroupId = groupId
            };
            var expectedCacheKey = $"group:{groupId}:history:100";

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("InvalidateCacheAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method?.Invoke(_orchestrator, [message]);

            // Assert
            _cacheServiceMock.Verify(x => x.RemoveAsync(expectedCacheKey), Times.Once);
        }

        [Fact]
        public async Task InvalidateCacheAsync_WithDifferentGroupIds_ShouldRemoveDifferentCacheKeys()
        {
            // Arrange
            var groupId1 = Guid.NewGuid();
            var groupId2 = Guid.NewGuid();
    
            var message1 = new GroupMessage("sender1", "content1")
            {
                GroupId = groupId1
            };
            var message2 = new GroupMessage("sender2", "content2")
            {
                GroupId = groupId2
            };

            var expectedCacheKey1 = $"group:{groupId1}:history:100";
            var expectedCacheKey2 = $"group:{groupId2}:history:100";

            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("InvalidateCacheAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            await (Task)method?.Invoke(_orchestrator, [message1]);
            await (Task)method?.Invoke(_orchestrator, [message2]);

            // Assert
            _cacheServiceMock.Verify(x => x.RemoveAsync(expectedCacheKey1), Times.Once);
            _cacheServiceMock.Verify(x => x.RemoveAsync(expectedCacheKey2), Times.Once);
        }

        [Fact]
        public async Task LoadChatHistory_WhenCacheHit_ShouldReturnCachedData()
{
    // Arrange
    var groupId = Guid.NewGuid();
    const int take = 10;
    const int skip = 0;
    var expectedCacheKey = $"group:{groupId}:history:{take}:skip:{skip}";
    
    var cachedMessages = new List<GroupMessage>
    {
        new("sender1", "cached content 1") { Id = Guid.NewGuid() },
        new("sender2", "cached content 2") { Id = Guid.NewGuid() }
    };

    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey))
        .ReturnsAsync(cachedMessages);

    // Act
    var result = await _orchestrator.LoadChatHistory(groupId, skip, take);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(cachedMessages.Count, result.Count);
    Assert.Equal(cachedMessages[0].Content, result[0].Content);
    Assert.Equal(cachedMessages[1].Content, result[1].Content);

    // Verify cache was checked
    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey), Times.Once);
    
    // Verify repository was NOT called when cache hit
    _messageRepositoryMock.Verify(x => x.GetMessageStoryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    _cacheServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<GroupMessage>>(), It.IsAny<TimeSpan>()), Times.Never);
}

        [Fact]
        public async Task LoadChatHistory_WhenCacheMiss_ShouldFetchDataAndCache()
{
    // Arrange
    var groupId = Guid.NewGuid();
    const int take = 10;
    const int skip = 0;
    var expectedCacheKey = $"group:{groupId}:history:{take}:skip:{skip}";
    
    var repositoryMessages = new List<GroupMessage>
    {
        new("sender1", "content1") { Id = Guid.NewGuid() },
        new("sender2", "content2") { Id = Guid.NewGuid() }
    };

    // Setup cache miss
    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey))
        .ReturnsAsync((List<GroupMessage>?)null);

    _messageRepositoryMock.Setup(x => x.GetMessageStoryAsync(groupId, skip, take))
        .ReturnsAsync(repositoryMessages);

    // Act
    var result = await _orchestrator.LoadChatHistory(groupId, skip ,take);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(repositoryMessages.Count, result.Count);

    // Verify cache miss was handled
    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey), Times.Once);
    _messageRepositoryMock.Verify(x => x.GetMessageStoryAsync(groupId, skip, take), Times.Once);
    _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, result, TimeSpan.FromMinutes(1)), Times.Once);
}

        [Fact]
        public async Task LoadChatHistory_WithDifferentGroupIds_ShouldUseDifferentCacheKeys()
{
    // Arrange
    var groupId1 = Guid.NewGuid();
    var groupId2 = Guid.NewGuid();
    const int take = 10;
    const int skip = 0;
    
    var expectedCacheKey1 = $"group:{groupId1}:history:{take}:skip:{skip}";
    var expectedCacheKey2 = $"group:{groupId2}:history:{take}:skip:{skip}";

    var cachedMessages1 = new List<GroupMessage> { new("sender1", "content1") };
    var cachedMessages2 = new List<GroupMessage> { new("sender2", "content2") };

    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey1))
        .ReturnsAsync(cachedMessages1);
    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey2))
        .ReturnsAsync(cachedMessages2);

    // Act
    var result1 = await _orchestrator.LoadChatHistory(groupId1, skip, take);
    var result2 = await _orchestrator.LoadChatHistory(groupId2, skip, take);

    // Assert
    Assert.Equal(cachedMessages1, result1);
    Assert.Equal(cachedMessages2, result2);

    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey1), Times.Once);
    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey2), Times.Once);
}

        [Fact]
        public async Task LoadChatHistory_WithDifferentTakeValues_ShouldUseDifferentCacheKeys()
{
    // Arrange
    var groupId = Guid.NewGuid();
    const int take1 = 10;
    const int take2 = 20;
    const int skip = 0;
    
    var expectedCacheKey1 = $"group:{groupId}:history:{take1}:skip:{skip}";
    var expectedCacheKey2 = $"group:{groupId}:history:{take2}:skip:{skip}";

    var cachedMessages1 = new List<GroupMessage> { new("sender1", "content1") };
    var cachedMessages2 = new List<GroupMessage> { new("sender2", "content2") };

    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey1))
        .ReturnsAsync(cachedMessages1);
    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey2))
        .ReturnsAsync(cachedMessages2);

    // Act
    var result1 = await _orchestrator.LoadChatHistory(groupId, skip, take1);
    var result2 = await _orchestrator.LoadChatHistory(groupId, skip, take2);

    // Assert
    Assert.Equal(cachedMessages1, result1);
    Assert.Equal(cachedMessages2, result2);

    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey1), Times.Once);
    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey2), Times.Once);
}
        
        [Fact]
        public async Task LoadChatHistory_WhenCacheMissWithEmptyRepository_ShouldCacheEmptyResult()
{
    // Arrange
    var groupId = Guid.NewGuid();
    const int take = 10;
    const int skip = 0;
    var expectedCacheKey = $"group:{groupId}:history:{take}:skip:{skip}";
    var emptyResult = new List<GroupMessage>();

    // Setup cache miss
    _cacheServiceMock.Setup(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey))
        .ReturnsAsync((List<GroupMessage>?)null);

    _messageRepositoryMock.Setup(x => x.GetMessageStoryAsync(groupId, skip, take))
        .ReturnsAsync(emptyResult);

    // Act
    var result = await _orchestrator.LoadChatHistory(groupId, skip, take);

    // Assert
    Assert.NotNull(result);
    Assert.Empty(result);

    // Verify cache operations
    _cacheServiceMock.Verify(x => x.GetAsync<List<GroupMessage>>(expectedCacheKey), Times.Once);
    _messageRepositoryMock.Verify(x => x.GetMessageStoryAsync(groupId, skip, take), Times.Once);
    _cacheServiceMock.Verify(x => x.SetAsync(expectedCacheKey, result, TimeSpan.FromMinutes(1)), Times.Once);
}
        
        [Fact]
        public async Task LoadChatHistory_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 10;
            var skip = 0;
            var emptyMessages = new List<GroupMessage>();

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, skip, take))
                .ReturnsAsync(emptyMessages);

            // Act
            var result = await _orchestrator.LoadChatHistory(groupId, skip, take);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task LoadChatHistory_WithDifferentTakeValues_ShouldPassCorrectParameters(int take, int skip = 0)
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var messages = new List<GroupMessage>();

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, skip, take))
                .ReturnsAsync(messages);

            // Act
            await _orchestrator.LoadChatHistory(groupId, skip, take);

            // Assert
            _messageRepositoryMock.Verify(x => x.GetMessageStoryAsync(groupId, skip, take), Times.Once);
        }

        [Fact]
        public void ApplyHashAndSet_ShouldHashSenderAndSetOnMessage()
        {
            // Arrange
            var dto = new GroupMessageDto("testSender", "content", Guid.NewGuid());
            var message = new GroupMessage("testSender", "content");
            var hashedSender = "hashedSender123";

            _hasherMock.Setup(x => x.Hash("testSender")).Returns(hashedSender);

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("ApplyHashAndSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [dto, message]);

            // Assert
            _hasherMock.Verify(x => x.Hash("testSender"), Times.Once);
            Assert.Equal(hashedSender, message.SenderHash);
        }
        
        [Xunit.Theory]
        [InlineData("user1", "hello")]
        [InlineData("user2", "world")]
        [InlineData("admin", "test message")]
        public void ApplyHashAndSet_WithDifferentSenders_ShouldHashEachCorrectly(string sender, string content)
        {
            // Arrange
            var dto = new GroupMessageDto(sender, content, Guid.NewGuid());
            var message = new GroupMessage(sender, content);
            var expectedHash = $"hash_{sender}";

            _hasherMock.Setup(x => x.Hash(sender)).Returns(expectedHash);

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("ApplyHashAndSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [dto, message]);

            // Assert
            _hasherMock.Verify(x => x.Hash(sender), Times.Once);
            Assert.Equal(expectedHash, message.SenderHash);
        }

        [Fact]
        public async Task LoadChatHistory_WhenRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 10;
            var skip = 0;
            var expectedException = new InvalidOperationException("Database error");

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, skip, take))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orchestrator.LoadChatHistory(groupId, skip, take));
            
            Assert.Equal("Database error", exception.Message);
        }

        [Fact]
        public void ApplyHashAndSet_WithNullSender_ShouldStillCallHasher()
        {
            // Arrange
            var dto = new GroupMessageDto(null, "content", Guid.NewGuid());
            var message = new GroupMessage("originalSender", "content");
            var hashedValue = "hashedNull";

            _hasherMock.Setup(x => x.Hash(null)).Returns(hashedValue);

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("ApplyHashAndSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [dto, message]);

            // Assert
            _hasherMock.Verify(x => x.Hash(null), Times.Once);
            Assert.Equal(hashedValue, message.SenderHash);
        }

        [Fact]
        public void EditMessage_WithEmptyContent_ShouldUpdateMessage()
        {
            // Arrange
            var message = new GroupMessage("sender", "originalContent");
            var editDto = new EditMessageDto("");

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("EditMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [message, editDto]);

            // Assert
            Assert.Null(message.Content);
            Assert.True(message.IsEdited);
            Assert.NotNull(message.EditTime);
        }

        [Fact]
        public void EditMessage_WithNullContent_ShouldUpdateMessage()
        {
            // Arrange
            var message = new GroupMessage("sender", "originalContent");
            var editDto = new EditMessageDto(null);

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("EditMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [message, editDto]);

            // Assert
            Assert.Null(message.Content);
            Assert.True(message.IsEdited);
            Assert.NotNull(message.EditTime);
        }

        [Fact]
        public void ApplyHashAndSet_ShouldNotChangeOtherMessageProperties()
        {
            // Arrange
            var dto = new GroupMessageDto("testSender", "content", Guid.NewGuid());
            var message = new GroupMessage("testSender", "originalContent");
            var originalContent = message.Content;
            var originalSender = message.Sender;
            var originalIsEdited = message.IsEdited;
            var originalIsDeleted = message.IsDeleted;

            _hasherMock.Setup(x => x.Hash("testSender")).Returns("hash123");

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("ApplyHashAndSet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [dto, message]);

            // Assert
            Assert.Equal(originalContent, message.Content);
            Assert.Equal(originalSender, message.Sender);
            Assert.Equal(originalIsEdited, message.IsEdited);
            Assert.Equal(originalIsDeleted, message.IsDeleted);
            Assert.Equal("hash123", message.SenderHash);
        }
        
        [Fact]
        public async Task InvalidateCacheByUserHashAsync_ShouldCompleteWithoutException()
        {
            // Arrange
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("InvalidateCacheByUserHashAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task)method?.Invoke(_orchestrator, ["someUserHash"]);

            // Assert
            await task;
            Assert.True(task.IsCompletedSuccessfully);
        }
    }
}