using AutoMapper;
using Encryptor.Decryption;
using Encryptor.Encryption;
using FluentValidation;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages;
using MessagingSystem.Services.Messaging.Application.Group.GroupMessages.Dto;
using MessagingSystem.Services.Messaging.Application.MessageDto;
using MessagingSystem.Services.Messaging.Core.Groups.GroupMessages;
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

        public GroupMessagesOrchestratorTests()
        {
            Mock<IMapper> mapperMock = new();
            _hasherMock = new Mock<IHasher>();
            Mock<IDecryptionInfo> decryptionInfoMock = new();
            Mock<IEncryptionInfo> encryptionInfoMock = new();
            Mock<IValidator<GroupMessageDto>> createValidatorMock = new();
            Mock<IValidator<EditMessageDto>> editValidatorMock = new();
            _messageRepositoryMock = new Mock<IGroupMessagesRepository>();

            _orchestrator = new GroupMessagesOrchestrator(
                mapperMock.Object,
                _hasherMock.Object,
                decryptionInfoMock.Object,
                encryptionInfoMock.Object,
                createValidatorMock.Object,
                editValidatorMock.Object,
                _messageRepositoryMock.Object
            );
        }

        [Fact]
        public async Task LoadChatHistory_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var take = 10;
            var emptyMessages = new List<GroupMessage>();

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, take))
                .ReturnsAsync(emptyMessages);

            // Act
            var result = await _orchestrator.LoadChatHistory(groupId, take);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Xunit.Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task LoadChatHistory_WithDifferentTakeValues_ShouldPassCorrectParameters(int take)
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var messages = new List<GroupMessage>();

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, take))
                .ReturnsAsync(messages);

            // Act
            await _orchestrator.LoadChatHistory(groupId, take);

            // Assert
            _messageRepositoryMock.Verify(x => x.GetMessageStoryAsync(groupId, take), Times.Once);
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

        [Fact]
        public void EditMessage_ShouldCallEditInfoOnMessage()
        {
            // Arrange
            var message = new GroupMessage("sender", "originalContent");
            var editDto = new EditMessageDto("newContent");
            var originalEditTime = message.EditTime;

            // Act
            var method = typeof(GroupMessagesOrchestrator)
                .GetMethod("EditMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(_orchestrator, [message, editDto]);

            // Assert
            Assert.Equal("newContent", message.Content);
            Assert.True(message.IsEdited);
            Assert.NotEqual(originalEditTime, message.EditTime);
            Assert.NotNull(message.EditTime);
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
            var expectedException = new InvalidOperationException("Database error");

            _messageRepositoryMock
                .Setup(x => x.GetMessageStoryAsync(groupId, take))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _orchestrator.LoadChatHistory(groupId, take));
            
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
            Assert.Equal("", message.Content);
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
    }
}